using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentsGenerator.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DocumentsGenerator.MVVM.Model
{
    internal class DocumentModel
    {
        public ObservableCollection<LoadedFileNameModel>? LoadedFileNames { get; set; }
        const string DataNs = "template-data";


        public void ReplaceCustomXml(string docPath, string newXmlPath)
        {
            using var wordDoc = WordprocessingDocument.Open(docPath, true);
            var main = wordDoc.MainDocumentPart!;

            var dataPart = main.CustomXmlParts.FirstOrDefault(part =>
            {
                try 
                { 
                    using var stream = part.GetStream(); 
                    return XDocument.Load(stream).Root?.Name.NamespaceName == DataNs; 
                }
                catch 
                { 
                    return false; 
                }
            });

            using (var src = File.OpenRead(newXmlPath))
            using (var dst = dataPart!.GetStream(FileMode.Create, FileAccess.Write))
                src.CopyTo(dst);

            XDocument dataDoc;
            using (var s = dataPart.GetStream(FileMode.Open, FileAccess.Read))
                dataDoc = XDocument.Load(s);

            void ProcessScope(OpenXmlElement scope)
            {
                foreach (var sdt in scope.Descendants<SdtRun>())
                {
                    var property = sdt.GetFirstChild<SdtProperties>();
                    var dataBinding = property?.GetFirstChild<DataBinding>();
                    if (dataBinding == null) continue;

                    var xpath = dataBinding.XPath?.Value ?? "";
                    var lastStep = xpath.Split('/').LastOrDefault() ?? "";
                    var noPred = lastStep.Split('[')[0];
                    var local = noPred.Contains(':') ? noPred[(noPred.IndexOf(':') + 1)..] : noPred;

                    var val = dataDoc.Root?.Element(XName.Get(local, DataNs))?.Value ?? "";
                    val ??= "";

                    var content = sdt.SdtContentRun ?? sdt.AppendChild(new SdtContentRun());
                    var run = content.Elements<Run>().FirstOrDefault();
                    if (run == null)
                    {
                        run = content.AppendChild(new Run());
                    }

                    if (run.RunProperties == null || !run.RunProperties.Any())
                    {
                        var endProperty = sdt.GetFirstChild<SdtEndCharProperties>();
                        var endRunProperty = endProperty?.GetFirstChild<RunProperties>();
                        if (endRunProperty != null)
                            run.RunProperties = (RunProperties)endRunProperty.CloneNode(true);
                    }

                    var text = run.Elements<Text>().FirstOrDefault();
                    if (text == null)
                    {
                        text = run.AppendChild(new Text() { Space = SpaceProcessingModeValues.Preserve });
                    }
                    text.Text = val;
                }
            }

            if (main.Document?.Body != null) ProcessScope(main.Document.Body);
            foreach (var headerPart in main.HeaderParts) if (headerPart.Header != null) ProcessScope(headerPart.Header);
            foreach (var footerPart in main.FooterParts) if (footerPart.Footer != null) ProcessScope(footerPart.Footer);

            main.Document?.Save();
        }

        public void GenerateDocuments(string saveDirectoryPath, string dataSheetPath, ref bool isError)
        {
            foreach(var file in LoadedFileNames!) {
                string outputDoc;
                if (file.FileKey == "")
                    outputDoc = $@"{saveDirectoryPath}\{file.FileName!}";
                else
                    outputDoc = $@"{saveDirectoryPath}\{file.FileName!.Replace(file.FileKey!, "")}";

                try
                {
                    if (file.FilePath != outputDoc)
                    {
                        File.Copy(file.FilePath!, outputDoc, true);
                        
                    }
                    ReplaceCustomXml(outputDoc, dataSheetPath);
                }
                catch (IOException ex)
                {
                    isError = true;
                    DialogWindow.ShowError($"Błąd podczas generowania dokumentu: {file.FileName}\n Błąd: {ex.Message}", "Błąd!");
                } 
            }
        }

        public DocumentModel() { }
    }
}
