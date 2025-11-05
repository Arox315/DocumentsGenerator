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
    static class DependencyManager
    {
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

                // We only need up to: Dependencies -> Key -> Values
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

    }
}
