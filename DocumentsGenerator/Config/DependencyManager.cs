using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocumentsGenerator.Config
{
    public static class DependencyManager
    {
        public static string GetDefaultDependenciesPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dependencies.json");

        public static string? GetSubValueFromJson(string key, string value, string subKey)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigManager.GetSetting("DependeciesFileName"));
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var root = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>>(json, options);

                if (root == null || !root.TryGetValue("Dependencies", out Dictionary<string, Dictionary<string, Dictionary<string, string>>>? dependencies))
                    return null;

                if (!dependencies.TryGetValue(key, out var valuesDict))
                    return null;

                if (!valuesDict.TryGetValue(value, out var subKeyDict))
                    return null;

                if (!subKeyDict.TryGetValue(subKey, out var result))
                    return null;

                return result;
            }
            catch
            {
                // Invalid JSON or unexpected structure
                return null;
            }
        }

        public static bool ContainsKeyInJson(string keyToFind)
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigManager.GetSetting("DependeciesFileName"));
            if (!File.Exists(filePath) || string.IsNullOrWhiteSpace(keyToFind))
                return false;

            try
            {
                var json = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Expected structure:
                // { "Dependencies": { "Key01": { ... }, "Key02": { ... } } }
                var root = JsonSerializer.Deserialize<
                    Dictionary<string, Dictionary<string, object>>>(json, options);

                if (root == null || !root.TryGetValue("Dependencies", out var dependencies))
                    return false;

                return dependencies.ContainsKey(keyToFind);
            }
            catch
            {
                // Invalid JSON or unexpected structure
                return false;
            }
        }

        public static ObservableCollection<string> GetValuesForKey(string key)
        {
            var result = new ObservableCollection<string>();

            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigManager.GetSetting("DependeciesFileName"));
            if (!File.Exists(filePath) || string.IsNullOrWhiteSpace(key))
                return result;

            try
            {
                var json = File.ReadAllText(filePath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var root = JsonSerializer.Deserialize<
                    Dictionary<string, Dictionary<string, Dictionary<string, object>>>>(json, options);

                if (root == null || !root.TryGetValue("Dependencies", out var dependencies))
                    return result;

                if (!dependencies.TryGetValue(key, out var valuesDict))
                    return result;

                foreach (var valueName in valuesDict.Keys)
                {
                    if (!string.IsNullOrWhiteSpace(valueName))
                        result.Add(valueName);
                }
            }
            catch
            {
                // ignore and return empty collection
            }

            return result;
        }

        public static bool TryValidateAndNormalizeJsonNoDuplicates(string filePath, out Dictionary<string, object> normalizedRoot, out List<string> errors)
        {
            normalizedRoot = new();
            errors = new();

            if (!File.Exists(filePath))
            {
                errors.Add("Plik nie istnieje.");
                return false;
            }

            try
            {
                using var stream = File.OpenRead(filePath);
                using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

                if (!doc.RootElement.TryGetProperty("Dependencies", out var depsEl) || depsEl.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("Brak sekcji \"Dependencies\" lub nieprawidłowy typ - oczekiwano obiektu.");
                    return false;
                }

                var cleaned = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);

                bool hasAtLeastOneKeyWithValue = false;

                var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var keyProp in depsEl.EnumerateObject())
                {
                    var keyNameRaw = keyProp.Name;
                    if (string.IsNullOrWhiteSpace(keyNameRaw)) continue;
                    var keyName = keyNameRaw.Trim();

                    if (!seenKeys.Add(keyName))
                    {
                        errors.Add($"Wykryto zduplikowany klucz: \"{keyName}\"");
                    }

                    if (keyProp.Value.ValueKind != JsonValueKind.Object)
                    {
                        errors.Add($"Klucz \"{keyName}\" ma nieprawidłowy typ wartości - oczekiwano obiektu.");
                        continue;
                    }

                    var cleanedValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                    var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var valueProp in keyProp.Value.EnumerateObject())
                    {
                        var valueNameRaw = valueProp.Name;
                        if (string.IsNullOrWhiteSpace(valueNameRaw)) continue;
                        var valueName = valueNameRaw.Trim();

                        if (!seenValues.Add(valueName))
                        {
                            errors.Add($"Wykryto zduplikowaną wartość: \"{valueName}\" dla klucza: \"{keyName}\"");
                        }

                        if (valueProp.Value.ValueKind != JsonValueKind.Object)
                        {
                            errors.Add($"Wartość: \"{valueName}\" dla klucza: \"{keyName}\" ma nieprawidłowy typ - oczekiwano obiektu.");
                            continue;
                        }

                        var cleanedSub = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        var seenSub = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        foreach (var subProp in valueProp.Value.EnumerateObject())
                        {
                            var subKeyRaw = subProp.Name;
                            if (string.IsNullOrWhiteSpace(subKeyRaw)) continue;
                            var subKey = subKeyRaw.Trim();

                            if (!seenSub.Add(subKey))
                            {
                                errors.Add($"Wykryto zduplikowany podklucz: \"{subKey}\" dla klucza: \"{keyName}\" i wartości: \"{valueName}\"");
                                continue;
                            }

                            if (subProp.Value.ValueKind != JsonValueKind.String)
                            {
                                errors.Add($"Podklucz: \"{subKey}\" dla klucza: \"{keyName}\" i wartości: \"{valueName}\" musi mieć tekstową wartość.");
                                continue;
                            }

                            cleanedSub[subKey] = subProp.Value.GetString() ?? string.Empty;
                        }

                        cleanedValues[valueName] = cleanedSub;
                    }

                    if (cleanedValues.Count > 0)
                    {
                        cleaned[keyName] = cleanedValues;
                        hasAtLeastOneKeyWithValue = true;
                    }
                }

                if (!hasAtLeastOneKeyWithValue || cleaned.Count == 0)
                {
                    errors.Add("Dane są niekompletne: wymagany jest co najmniej jeden klucz z co najmniej jedną wartością. Wartości mogą nie zawierać par podklucz/podwartość.");
                }

                if (errors.Count > 0)
                {
                    return false;
                }

                normalizedRoot["Dependencies"] = cleaned;
                return true;
            }
            catch (JsonException jex)
            {
                errors.Add($"Nieprawidłowy JSON: {jex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                errors.Add($"Błąd podczas walidacji: {ex.Message}");
                return false;
            }
        }

        public static bool TryBuildDependenciesValidated(ObservableCollection<KeyItem> Keys, out Dictionary<string, Dictionary<string, Dictionary<string, string>>> dependencies, out List<string> errors)
        {
            dependencies = new(StringComparer.OrdinalIgnoreCase);
            errors = new();

            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasAtLeastOneKeyWithoutValue = false;

            foreach (var key in Keys)
            {
                if (string.IsNullOrWhiteSpace(key.Name))
                    continue;

                var keyName = key.Name.Trim();

                if (!seenKeys.Add(keyName))
                    errors.Add($"Wykryto zduplikowany klucz: \"{keyName}\"");

                var cleanedValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var val in key.Values)
                {
                    if (string.IsNullOrWhiteSpace(val.Name))
                        continue;

                    var valueName = val.Name.Trim();

                    if (!seenValues.Add(valueName))
                        errors.Add($"Wykryto zduplikowaną wartość: \"{valueName}\" dla klucza: \"{keyName}\"");

                    var cleanedSub = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var seenSubKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var pair in val.SubPairs)
                    {
                        if (string.IsNullOrWhiteSpace(pair.SubKey) || string.IsNullOrWhiteSpace(pair.SubValue))
                            continue;

                        var sk = pair.SubKey.Trim();
                        if (!seenSubKeys.Add(sk))
                        {
                            errors.Add($"Wykryto zduplikowany podklucz: \"{pair.SubKey}\" dla klucza: \"{keyName}\" i wartości: \"{valueName}\"");
                            continue;
                        }

                        if (!cleanedSub.ContainsKey(sk))
                            cleanedSub[sk] = pair.SubValue;
                    }

                    cleanedValues[valueName] = cleanedSub;
                }

                if (cleanedValues.Count > 0)
                {
                    dependencies[keyName] = cleanedValues;
                }
                else
                {
                    hasAtLeastOneKeyWithoutValue = true;
                    errors.Add($"Klucz: \"{keyName}\" nie posiada żadnych wartości.");
                }
            }

            if (hasAtLeastOneKeyWithoutValue || dependencies.Count == 0)
            {
                errors.Add("Dane są niekompletne: wymagany jest co najmniej jeden klucz z co najmniej jedną wartością. Wartości mogą nie zawierać par podklucz/podwartość.");
            }

            return errors.Count == 0;
        }

        public static void PrepopulateSubPairsFromLatest(KeyItem key, ValueItem targetValue, string defaultSubValue = "Nowa Podwartość")
        {
            if (key == null || targetValue == null) return;

            for (int i = key.Values.Count - 1; i >= 0; i--)
            {
                var src = key.Values[i];
                if (src.SubPairs != null && src.SubPairs.Count > 0)
                {
                    foreach (var sp in src.SubPairs)
                    {
                        targetValue.SubPairs.Add(new SubPair
                        {
                            SubKey = sp.SubKey,
                            SubValue = defaultSubValue
                        });
                    }
                    break;
                }
            }
        }

    }
}
