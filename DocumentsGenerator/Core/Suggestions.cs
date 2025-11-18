using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocumentsGenerator.Core
{
    public static class Suggestions
    {
        public static ObservableCollection<string> LoadKeySuggestions(string filePath) => LoadList(filePath, "Keys");

        private static ObservableCollection<string> LoadList(string filePath, string arrayName)
        {
            var result = new ObservableCollection<string>();
            if (!File.Exists(filePath)) return result;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
                if (doc.RootElement.TryGetProperty(arrayName, out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var element in arr.EnumerateArray())
                    {
                        if (element.ValueKind == JsonValueKind.String)
                        {
                            var suggestion = element.GetString();
                            if (!string.IsNullOrWhiteSpace(suggestion))
                                result.Add(suggestion.Trim());
                        }
                    }
                }
            }
            catch (Exception ex){
                Debug.WriteLine(ex.Message);
            }

            return result;
        }
    }
}
