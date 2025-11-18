using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using DocumentsGenerator.MVVM.Model;
using DocumentsGenerator.Core;

namespace DocumentsGenerator.Config
{
    public static class DependencyKeysManager
    {
        public static readonly string allKeysPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "all_keys.json");

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        public static bool UpdateAllKeysFile(IEnumerable<string> newKeys)
        {
            try
            {
                if (newKeys == null)
                    return false;

                var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (File.Exists(allKeysPath))
                {
                    try
                    {
                        var json = File.ReadAllText(allKeysPath);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("Keys", out var arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in arr.EnumerateArray())
                            {
                                
                                if (element.ValueKind == JsonValueKind.String)
                                {
                                    var keyName = element.GetString();
                                    if (!string.IsNullOrWhiteSpace(keyName))
                                        existingKeys.Add(keyName.Trim().Replace(" ", "_"));
                                }
                            }
                        }
                    }
                    catch
                    {
                        existingKeys.Clear();
                    }
                }

                bool isAdded = false;
                foreach (var keyName in newKeys.Where(key => !string.IsNullOrWhiteSpace(key)))
                {
                    if (existingKeys.Add(keyName.Trim().Replace(" ", "_")))
                        isAdded = true;
                }

                if (!isAdded && File.Exists(allKeysPath))
                    return false;

                // alpahabetical sort
                var sortedKeys = existingKeys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();

                var jsonObj = new Dictionary<string, object>
                {
                    ["Keys"] = sortedKeys
                };

                var jsonOut = JsonSerializer.Serialize(jsonObj, _jsonOptions);
                File.WriteAllText(allKeysPath, jsonOut);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        public static bool UpdateAllKeysFile(IEnumerable<AllKeysModel> newKeys)
        {
            if (newKeys is null) return false;

            var empty = newKeys.FirstOrDefault(key => key == null || string.IsNullOrWhiteSpace(key.Value));
            if (empty != null)
            {
                throw new JsonValueIsEmptyException("all_keys.json update failed: one or more items have empty Value.");
            }

            var unique = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in newKeys)
            {
                var sanitized = SanitizeKey(item.Value!);
                if (sanitized.Length == 0)
                { 
                    throw new JsonSanitizationException("all_keys.json update failed: a Value normalized to empty after sanitization."); 
                }

                if (unique.Contains(sanitized))
                {
                    throw new JsonValueIsDuplicateException($"all_keys.json update failed: Detected a duplicate value: {sanitized}.",sanitized);
                }

                unique.Add(sanitized);
            }

            if (unique.Count == 0) return false;

            var sorted = unique.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();

            var payload = new Dictionary<string, object> { ["Keys"] = sorted };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            File.WriteAllText(allKeysPath, json);

            return true;
        }

        public static bool ImportAllKeysFile(string importFilePath, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(importFilePath) || !File.Exists(importFilePath))
            {
                errors.Add("Plik nie istnieje lub ścieżka jest pusta.");
                return false;
            }

            try
            {
                using var stream = File.OpenRead(importFilePath);
                using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
                {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                });

                if (!doc.RootElement.TryGetProperty("Keys", out var keysElement) || keysElement.ValueKind != JsonValueKind.Array)
                {
                    errors.Add("Nieprawidłowy format: oczekiwano obiektu z tablicą \"Keys\".");
                    return false;
                }

                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int index = 0;
                foreach (var element in keysElement.EnumerateArray())
                {
                    index++;
                    if (element.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"Element Keys[{index}] nie jest tekstem.");
                        continue;
                    }

                    var raw = element.GetString();
                    var sanitized = SanitizeKey(raw!);

                    if (string.IsNullOrEmpty(sanitized))
                    {
                        errors.Add($"Element Keys[{index}] jest pusty po normalizacji.");
                        continue;
                    }

                    set.Add(sanitized);
                }

                if (errors.Count > 0)
                    return false;

                if (set.Count == 0)
                {
                    errors.Add("Tablica \"Keys\" jest pusta po weryfikacji.");
                    return false;
                }

                var sorted = set.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList();
                var payload = new Dictionary<string, object> { ["Keys"] = sorted };

                var jsonOut = JsonSerializer.Serialize(payload, _jsonOptions);
                File.WriteAllText(allKeysPath, jsonOut);

                return true;
            }
            catch (JsonException jx)
            {
                errors.Add($"Nieprawidłowy JSON: {jx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                errors.Add($"Błąd podczas importu: {ex.Message}");
                return false;
            }
        }

        private static string SanitizeKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

            var keyName = raw.Normalize(NormalizationForm.FormKC).Trim();

            var stringBuilder = new StringBuilder(keyName.Length);
            bool lastUnderscore = false;

            foreach (var chr in keyName)
            {
                if (char.IsWhiteSpace(chr))
                {
                    if (!lastUnderscore)
                    {
                        stringBuilder.Append('_');
                        lastUnderscore = true;
                    }
                }
                else
                {
                    stringBuilder.Append(chr);
                    lastUnderscore = (chr == '_');
                }
            }

            return stringBuilder.ToString().Trim('_');
        }
    }
}
