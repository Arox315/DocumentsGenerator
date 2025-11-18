
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentsGenerator.Config;
using DocumentsGenerator.MVVM.View;


namespace DocumentsGenerator.MVVM.Model
{
    class TemplateModel
    {
        public ObservableCollection<LoadedFileNameModel>? LoadedFileNames { get; set; }

        // Match {tag_name} where tag can contain letters, digits, underscore, dash, colon
        private static readonly Regex TagRegex = new(@"\{([^{}]+)\}", RegexOptions.Compiled);

        private const string DataNs = "template-data";

        public void GenerateTemplates(string inFolder, string outFolder, ref bool isError)
        {
            Debug.WriteLine("Generowanie dokumentów...");
            string generationDate = DateTime.Now.ToString("dd'-'MM'-'yyyy'T'HH'-'mm'-'ss");
            string templateSubfolderName = outFolder + $@"\szablony_{generationDate}\";
            string sheetSubfolderName = outFolder + $@"\arkusze_{generationDate}\";

            Directory.CreateDirectory(templateSubfolderName);
            Directory.CreateDirectory(sheetSubfolderName);

            foreach (LoadedFileNameModel file in LoadedFileNames!) {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(file.FilePath!);
                    fileName = fileName.Replace(file.FileKey!, "");

                    string templateFileName = GetTemplateFileNameWithKey(fileName);
                    string dataSheetFileName = GetDataSheetFileNameWithKey(fileName);

                    string outputPath = templateSubfolderName + templateFileName;
                    string xmlPath = sheetSubfolderName + dataSheetFileName;

                    
                    if (file.FilePath != outputPath)
                    {
                        File.Copy(file.FilePath!, outputPath, overwrite: true);
                    }
                    
                    using (var doc = WordprocessingDocument.Open(outputPath, true))
                    {
                        var orderedRaw = CollectTagsInDocumentOrder(doc);
                        var tagMap = BuildTagMap(orderedRaw);

                        var xml = BuildDataXmlInOrder(orderedRaw, tagMap);
                        xml.Save(xmlPath);

                        var (part, storeItemId) = AddOrReplaceCustomXmlPart(doc, xml);

                        ReplaceTagsWithBoundContentControls(doc, storeItemId, tagMap);

                        // create keys list for dependency suggestions
                        try
                        {
                            bool success = DependencyKeysManager.UpdateAllKeysFile(orderedRaw);
                            if (!success)
                            {
                                Debug.WriteLine("No new changes - no new keys");
                            }
                        }
                        catch (Exception e) {
                            throw new Exception(message: e.Message);
                        }
                    }
                }
                catch (Exception ex) {
                    isError = true;
                    DialogWindow.ShowError($"Błąd podczas generacji szablonu: {file.FileName}\n Błąd: {ex}", "Błąd!");
                }
                
            }

            // Generate merged Data Sheet, if more than 1 template generated
            if(LoadedFileNames.Count > 1)
            {
                DataSheetModel dataSheetModel = new DataSheetModel();
                dataSheetModel.LoadedFileNames = new ObservableCollection<LoadedFileNameModel>();

                string[] dataSheets = Directory.GetFiles(sheetSubfolderName);
                foreach (string dataSheet in dataSheets)
                {
                    dataSheetModel.LoadedFileNames!.Add(new LoadedFileNameModel
                    {
                        FilePath = dataSheet,
                        FileName = Path.GetFileName(dataSheet),
                        FileKey = ""
                    });
                }
                dataSheetModel.MergeDataSheets(outFolder, ref isError);
            }
        }

        public TemplateModel() {}

        private static string ToXmlName(string raw)
        {
            if (raw == null) raw = "";
            raw = raw.Trim();

            // Replace whitespace with underscore
            raw = Regex.Replace(raw, @"\s+", "_");

            // Colon is reserved for namespaces - replace with underscore
            raw = raw.Replace(':', '_');

            // Remove/replace invalid chars (keep letters/digits/underscore/hyphen/dot)
            raw = Regex.Replace(raw, @"[^\p{L}\p{Nd}_\-\.\u00B7]", "_");

            // XML names cannot start with digit, hyphen or dot; must start with letter or underscore
            if (raw.Length == 0) raw = "_";
            if (!Regex.IsMatch(raw.Substring(0, 1), @"[\p{L}_]"))
                raw = "_" + raw;

            // "xml" start is reserved (case-insensitive)
            if (raw.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
                raw = "_" + raw;

            return raw;
        }

        private static Dictionary<string, string> BuildTagMap(IEnumerable<string> orderedRawTags)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            var used = new HashSet<string>(StringComparer.Ordinal);

            foreach (var raw in orderedRawTags)
            {
                var baseName = ToXmlName(raw);
                var name = baseName;
                int i = 2;
                while (!used.Add(name)) name = $"{baseName}_{i++}";
                map[raw] = name;
            }
            return map;
        }

        private static XDocument BuildDataXmlInOrder(
        IList<string> orderedRawTags,
        Dictionary<string, string> tagMap)
        {
            XNamespace ns = DataNs;
            var root = new XElement(ns + "root",
                orderedRawTags.Select(raw =>
                    new XElement(ns + tagMap[raw], "{" + raw + "}")
                )
            );
            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);
        }

        private static List<string> CollectTagsInDocumentOrder(WordprocessingDocument doc)
        {
            var ordered = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var paragraph in doc.MainDocumentPart!.Document.Body!.Descendants<Paragraph>())
            {
                var text = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
                foreach (Match match in TagRegex.Matches(text))
                {
                    var raw = match.Groups[1].Value.Trim();
                    if (raw.Length == 0) continue;
                    if (seen.Add(raw)) ordered.Add(raw);
                }
            }

            return ordered;
        }

        private static (CustomXmlPart part, string storeItemId) AddOrReplaceCustomXmlPart(WordprocessingDocument doc, XDocument xml)
        {
            foreach (var existing in doc.MainDocumentPart!.CustomXmlParts.ToList())
            {
                try
                {
                    var xDoc = XDocument.Load(existing.GetStream(FileMode.Open, FileAccess.Read));
                    if (xDoc.Root?.Name.NamespaceName == DataNs)
                        if (existing.CustomXmlPropertiesPart != null)
                            existing.DeletePart(existing.CustomXmlPropertiesPart);
                }
                catch { } // ignore unreadable parts
            }

            var part = doc.MainDocumentPart!.AddCustomXmlPart(CustomXmlPartType.CustomXml);
            using (var stream = part.GetStream(FileMode.Create, FileAccess.Write))
                xml.Save(stream);

            var props = part.AddNewPart<CustomXmlPropertiesPart>();
            var guid = "{" + Guid.NewGuid().ToString().ToUpper() + "}";
            var dataStoreItem = new DocumentFormat.OpenXml.CustomXmlDataProperties.DataStoreItem
            {
                ItemId = guid
            };
            props.DataStoreItem = dataStoreItem;

            return (part, guid);
        }

        private static bool IsInsideContentControl(OpenXmlElement element)
        {
            for (var currentElement = element.Parent; currentElement != null; currentElement = currentElement.Parent)
                if (currentElement is SdtRun || currentElement is SdtBlock || currentElement is SdtCell)
                    return true;
            return false;
        }


        private static void ReplaceTagsWithBoundContentControls(WordprocessingDocument doc, string storeItemId, Dictionary<string, string> tagMap)
        {
            var body = doc.MainDocumentPart!.Document.Body!;
            var paragraphs = body.Descendants<Paragraph>().ToList();

            foreach (var paragraph in paragraphs)
            {
                var runs = paragraph.Elements<Run>().ToList();
                if (!runs.Any()) continue;

                var map = BuildRunMap(runs, out string paraText);

                var matches = TagRegex.Matches(paraText).Cast<Match>().ToList();
                if (matches.Count == 0) continue;

                // Replace from end to start to keep indices valid
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    var match = matches[i];
                    var tagName = match.Groups[1].Value;
                    int start = match.Index;
                    int length = match.Length;

                    // Find the exact run span that covers [start, start+length)
                    var (firstIdx, lastIdx, startOffsetInFirst, endOffsetInLast) = LocateRunSpan(map, start, length);

                    // Split LAST run first so indices remain valid
                    if (endOffsetInLast > 0 && endOffsetInLast < map[lastIdx].Text.Length)
                    {
                        var split = SplitRunExact(map[lastIdx].Run, 0, endOffsetInLast);
                        if (split == null) continue;
                        
                        runs = paragraph.Elements<Run>().ToList();
                        map = BuildRunMap(runs, out paraText);
                    }

                    // Split FIRST run so that the tag begins at a run boundary
                    if (startOffsetInFirst > 0 && startOffsetInFirst < map[firstIdx].Text.Length)
                    {
                        var split = SplitRunExact(map[firstIdx].Run, startOffsetInFirst, map[firstIdx].Text.Length - startOffsetInFirst);
                        if (split == null) continue;
                       
                        runs = paragraph.Elements<Run>().ToList();
                        map = BuildRunMap(runs, out paraText);
                    }

                    // Recompute the exact runs covering the match span and isolate the exact match run(s)
                    var exactRuns = FindExactRunsForSpan(map, start, length).Select(s => s.Run).ToList();
                    if (exactRuns.Count == 0) continue;

                    if (exactRuns.Count == 1)
                    {
                        var single = exactRuns[0];
                        if (IsInsideContentControl(single)) continue;

                        var textNodes = single.Elements<Text>().ToList();
                        var flat = string.Concat(textNodes.Select(t => t.Text));
                        var idx = TagRegex.Match(flat).Index;
                        var tagText = TagRegex.Match(flat).Value;
                        if (string.IsNullOrEmpty(tagText)) continue;

                        var split2 = SplitRunExact(single, idx, tagText.Length);
                        if (split2 == null) continue;

                        exactRuns = new List<Run> { split2.Value.mid };
                    }
                    else
                    {
                        // Check none are inside SDT already
                        if (exactRuns.Any(IsInsideContentControl)) continue;
                    }

                    // Create SDTRun bound to XML
                    var firstRunProps = exactRuns.First().RunProperties?.CloneNode(true) as RunProperties;

                    if (tagMap != null)
                    {
                        if (!tagMap.TryGetValue(tagName, out var safeName)) continue;

                        var sdt = CreateBoundSdtRun(originalTag: tagName, safeXmlName: safeName, storeItemId);

                        var displayRun = new Run();
                        if (firstRunProps != null)
                            displayRun.RunProperties = (RunProperties)firstRunProps.CloneNode(true);

                        displayRun.Append(new Text($"{{{tagName}}}") { Space = SpaceProcessingModeValues.Preserve });
                        sdt.SdtContentRun!.Append(displayRun);

                        var anchor = exactRuns.First();
                        var parent = anchor.Parent;
                        if (parent == null) continue;
                        parent.InsertBefore(sdt, anchor);
                    }

                    foreach (var r in exactRuns) r.Remove();

                    runs = paragraph.Elements<Run>().ToList();
                    map = BuildRunMap(runs, out paraText);
                }
            }
        }

        private static SdtRun CreateBoundSdtRun(string originalTag, string safeXmlName, string storeItemId)
        {
            var sdt = new SdtRun(
                new SdtProperties(
                    new SdtAlias { Val = originalTag },
                    new Tag { Val = originalTag },
                    new DataBinding
                    {
                        StoreItemId = storeItemId,
                        PrefixMappings = $"xmlns:ns0='{DataNs}'",
                        XPath = $"/ns0:root/ns0:{safeXmlName}"
                    },
                    new Lock { Val = LockingValues.SdtLocked }
                ),
                new SdtContentRun()
            );
            return sdt;
        }

        private sealed class RunSlice
        {
            public Run Run = null!;
            public string Text = "";
            public int Start;
            public int End;
        }

        private static List<RunSlice> BuildRunMap(List<Run> runs, out string paragraphText)
        {
            var map = new List<RunSlice>();
            int pos = 0;
            foreach (var run in runs)
            {
                var tNodes = run.Elements<Text>().ToList();
                if (!tNodes.Any())
                {
                    map.Add(new RunSlice { Run = run, Text = "", Start = pos, End = pos });
                    continue;
                }
                var text = string.Concat(tNodes.Select(tNode => tNode.Text));
                map.Add(new RunSlice { Run = run, Text = text, Start = pos, End = pos + text.Length });
                pos += text.Length;
            }
            paragraphText = string.Concat(map.Select(runSlice => runSlice.Text));
            return map;
        }

        private static (int firstIdx, int lastIdx, int startOffsetInFirst, int endOffsetInLast)
            LocateRunSpan(List<RunSlice> map, int start, int length)
        {
            int end = start + length;
            int firstIdx = -1, lastIdx = -1, startOff = 0, endOff = 0;

            for (int i = 0; i < map.Count; i++)
            {
                var runSlice = map[i];
                if (firstIdx == -1 && start >= runSlice.Start && start <= runSlice.End)
                {
                    firstIdx = i;
                    startOff = start - runSlice.Start;
                }
                if (end >= runSlice.Start && end <= runSlice.End)
                {
                    lastIdx = i;
                    endOff = end - runSlice.Start;
                    break;
                }
            }

            if (firstIdx == -1) firstIdx = 0;
            if (lastIdx == -1) lastIdx = map.Count - 1;
            return (firstIdx, lastIdx, startOff, endOff);
        }

        private static (Run? left, Run mid, Run? right)? SplitRunExact(Run originalRun, int startOffset, int length)
        {
            var parent = originalRun.Parent;
            if (parent == null) return null;

            var texts = originalRun.Elements<Text>().ToList();
            if (texts.Count == 0) return null;

            var flat = string.Concat(texts.Select(t => t.Text));
            if (startOffset < 0 || length <= 0 || startOffset + length > flat.Length) return null;

            var leftStr = flat.Substring(0, startOffset);
            var midStr = flat.Substring(startOffset, length);
            var rightStr = flat.Substring(startOffset + length);

            Run CloneStyledRun(string s)
            {
                var run = (Run)originalRun.CloneNode(true);
                run.RemoveAllChildren<Text>();
                run.Append(new Text(s) { Space = SpaceProcessingModeValues.Preserve });
                return run;
            }

            Run? leftRun = string.IsNullOrEmpty(leftStr) ? null : CloneStyledRun(leftStr);
            var midRun = CloneStyledRun(midStr);
            Run? rightRun = string.IsNullOrEmpty(rightStr) ? null : CloneStyledRun(rightStr);

            OpenXmlElement anchor = originalRun;

            if (leftRun != null) parent.InsertBefore(leftRun, anchor);
            parent.InsertBefore(midRun, anchor);
            if (rightRun != null) parent.InsertBefore(rightRun, anchor);

            originalRun.Remove();

            return (leftRun, midRun, rightRun);
        }

        private static List<RunSlice> FindExactRunsForSpan(List<RunSlice> map, int start, int length)
        {
            int end = start + length;
            return map.Where(runSlice => !(runSlice.End <= start || runSlice.Start >= end))
                      .OrderBy(runSlice => runSlice.Start)
                      .ToList();
        }

        private string GetTemplateFileNameWithKey(string initialFileName)
        {
            string keyFilterType = ConfigManager.GetSetting("DocumentDefaultFileKeyFilter");
            string keyFilterName = ConfigManager.GetSetting("DocumentDefaultFileKeyName");

            if (keyFilterType == "0") return initialFileName + keyFilterName + ".docx";
            if (keyFilterType == "1") return keyFilterName + initialFileName + ".docx";
            if (keyFilterType == "2") return initialFileName + keyFilterName + ".docx";

            return initialFileName + ".docx";
        }

        private string GetDataSheetFileNameWithKey(string initialFileName)
        {
            string keyFilterType = ConfigManager.GetSetting("DataSheetDefaultFileKeyFilter");
            string keyFilterName = ConfigManager.GetSetting("DataSheetDefaultFileKeyName");

            if (keyFilterType == "0") return initialFileName + keyFilterName + ".xml";
            if (keyFilterType == "1") return keyFilterName + initialFileName + ".xml";
            if (keyFilterType == "2") return initialFileName + keyFilterName + ".xml";

            return initialFileName + ".xml";
        }
    }
}
