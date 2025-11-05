using DocumentFormat.OpenXml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentsGenerator.Config;
using DocumentsGenerator.Core;
using DocumentsGenerator.MVVM.Model;
using DocumentsGenerator.MVVM.View;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DocumentsGenerator.MVVM.ViewModel
{
    class SettingsViewModel : ObservableObject
    {
        private readonly Constants constants = new Constants();

        public SolidColorBrush MainColor { get; set; }
        public SolidColorBrush MainColorDark { get; set; }

        public ObservableCollection<string>? FileKeyFilters { get; } = new()
        {
            "Zawiera",
            "Zaczyna się",
            "Kończy się"
        };

        public List<SettingsModel> Settings { get; set; } = new();

        public SettingsModel TemplateDefaultFileKeyName { get; set; }
        public SettingsModel TemplateDefaultFileKeyFilter { get; set; }
        public SettingsModel TemplateFileSelectorInitialDirectory { get; set; }
        public SettingsModel TemplateFolderSelectorInitialDirectory { get; set; }
        public SettingsModel TemplateSaveFolderSelectInitialDirectory { get; set; }
        public SettingsModel TemplateSaveFolderDirectory { get; set; }


        public SettingsModel DataSheetDefaultFileKeyName { get; set; }
        public SettingsModel DataSheetDefaultFileKeyFilter { get; set; }
        public SettingsModel DataSheetFileSelectorInitialDirectory { get; set; }
        public SettingsModel DataSheetFolderSelectorInitialDirectory { get; set; }
        public SettingsModel DataSheetSaveFolderSelectInitialDirectory { get; set; }
        public SettingsModel DataSheetSaveFolderDirectory { get; set; }
        public SettingsModel DataSheetEditInitialDirectory { get; set; }

        public SettingsModel DocumentDefaultFileKeyName { get; set; }
        public SettingsModel DocumentDefaultFileKeyFilter { get; set; }
        public SettingsModel DocumentFileSelectorInitialDirectory { get; set; }
        public SettingsModel DocumentFolderSelectorInitialDirectory { get; set; }
        public SettingsModel DocumentSaveFolderSelectInitialDirectory { get; set; }
        public SettingsModel DocumentSaveFolderDirectory { get; set; }
        public SettingsModel DocumentLoadDataSheetInitialDirectory { get; set; }

        public RelayCommand<object> SaveSettingsCommand { get; set; }

        public ObservableCollection<KeyItem> Keys { get; } = new();

        public RelayCommand<object> AddKeyCommand { get; }
        public RelayCommand<object> RemoveKeyCommand { get; }
        public RelayCommand<object> AddValueCommand { get; }
        public RelayCommand<object> RemoveValueCommand { get; }
        public RelayCommand<object> AddSubPairCommand { get; }
        public RelayCommand<object> RemoveSubPairCommand { get; }
        public RelayCommand<object> SaveDependenciesCommand { get; }
        public RelayCommand<object> ImportCommand { get; }

        public void SaveSettings()
        {
            foreach (var setting in Settings)
            {
                try
                {
                    if (setting.Type == "DropDown")
                    {
                        string newValue = FileKeyFilters!.IndexOf(setting.Value!).ToString();
                        ConfigManager.UpdateSetting(setting.Name!, newValue);
                    }
                    else
                    {
                        ConfigManager.UpdateSetting(setting.Name!, setting.Value!);
                    }
                }
                catch
                {
                    DialogWindow.ShowError($"Błąd podczas zapisu ustawienia:\n{setting.DisplayName}", "Błąd!");
                }

            }

            DialogWindow.ShowInfo("Zapisywanie ustawień zakończone", "Zapis ustawień");
        }


        private void SaveToJson()
        {
            if (!TryBuildDependenciesValidated(out var dependencies, out var errors))
            {
                DialogWindow.Show("Zapis nie powiódł się z następujących problemów:\n\n" + string.Join("\n", errors),
                    "Błąd zapisu!", DialogType.Ok, DialogIcon.Error);
                return;
            }
            
            var root = new Dictionary<string, object>
            {
                ["Dependencies"] = dependencies
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            try
            {
                string filePath = GetDefaultDependenciesPath();
                var json = JsonSerializer.Serialize(root, options);
                File.WriteAllText(filePath, json);
                DialogWindow.Show("Pomyślnie zapisano zmiany.", "Zapis zależności", DialogType.Ok, DialogIcon.Info);
            }
            catch (Exception ex)
            {
                DialogWindow.ShowError($"Błąd podczas zapisu pliku.\nBłąd:{ex.Message}", "Błąd zapisu!");
            }
        }

        private bool TryBuildDependenciesValidated(out Dictionary<string, Dictionary<string, Dictionary<string, string>>> dependencies, out List<string> errors)
        {
            dependencies = new(StringComparer.OrdinalIgnoreCase);
            errors = new();

            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasAtLeastOneValidChain = false;

            foreach (var key in Keys)
            {
                if (string.IsNullOrWhiteSpace(key.Name))
                    continue;

                var keyName = key.Name.Trim();

                if (!seenKeys.Add(keyName))
                    errors.Add($"Wykryto zduplikowany klucz: \"{keyName}\"");

                var cleanedValues = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                var seenValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                int validValueCountForKey = 0;

                foreach (var val in key.Values)
                {
                    if (string.IsNullOrWhiteSpace(val.Name))
                        continue;

                    var valueName = val.Name.Trim();

                    if (!seenValues.Add(valueName))
                        errors.Add($"Wykryto zduplikowaną wartość: \"{valueName}\" dla klucza: \"{keyName}\"");

                    var cleanedSub = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    var seenSubKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    int validSubPairsForValue = 0;

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

                        cleanedSub[sk] = pair.SubValue;
                        validSubPairsForValue++;
                    }

                    if (validSubPairsForValue == 0)
                    {
                        errors.Add($"Wartość: \"{valueName}\" dla klucza: \"{keyName}\" nie posiada żadnej pary podklucz/podwartość.");
                    }
                    else
                    {
                        cleanedValues[valueName] = cleanedSub;
                        validValueCountForKey++;
                        hasAtLeastOneValidChain = true;
                    }
                }

                if (validValueCountForKey == 0)
                {
                    errors.Add($"Klucz: \"{keyName}\" nie posiada żadnej poprawnej wartości.");
                }
                else
                {
                    dependencies[keyName] = cleanedValues;
                }
            }

            if (!hasAtLeastOneValidChain || dependencies.Count == 0)
            {
                errors.Add("Dane są niekompletne: potrzebny jest co najmniej jeden zbiór: klucz -> wartość -> para podklucz/podwartość.");
            }

            return errors.Count == 0;
        }

        public bool LoadFromJson()
        {
            string filePath = GetDefaultDependenciesPath();
            if (!File.Exists(filePath)) return false;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                // Expected shape:
                // { "Dependencies": { "Key01": { "Value01": { "SubKey01": "SubVal", ... }, ... } } }
                var root = JsonSerializer.Deserialize<
                    Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>>(File.ReadAllText(filePath),
                    options);

                if (root == null || !root.TryGetValue("Dependencies", out var deps)) return false;

                Keys.Clear();

                foreach (var (keyName, valuesDict) in deps)
                {
                    var keyItem = new KeyItem { Name = keyName };

                    foreach (var (valueName, subDict) in valuesDict)
                    {
                        var valueItem = new ValueItem { Name = valueName };

                        foreach (var (subKey, subVal) in subDict)
                        {
                            valueItem.SubPairs.Add(new SubPair
                            {
                                SubKey = subKey,
                                SubValue = subVal
                            });
                        }

                        keyItem.Values.Add(valueItem);
                    }

                    Keys.Add(keyItem);
                }

                return true;
            }
            catch 
            {
                return false;
            }
        }

        private static string GetDefaultDependenciesPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dependencies.json");

        private void ImportFromJson()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Importuj zależności...",
                Filter = "Pliki JSON(*.json)|*.json",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() != true) return;

            if (!TryValidateJsonFile(ofd.FileName, out var normalizedRoot))
            {
                DialogWindow.Show("Wybrany plik nie jest prawidłowym plikiem JSON zależności.", "Niepoprawny plik", DialogType.Ok, DialogIcon.Warning);
                return;
            }

            var targetPath = GetDefaultDependenciesPath();
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(normalizedRoot, options);
                File.WriteAllText(targetPath, json);

                if (!LoadFromJson())
                {
                    DialogWindow.Show("Zaimportowany plik został zapisany, ale ponowne wczytanie nie powiodło się.", "Importowanie zależności", DialogType.Ok, DialogIcon.Warning);
                }
                else
                {
                    DialogWindow.ShowInfo("Zaimportowano i załadowano pomyślnie.", "Importowanie zależności");
                }
            }
            catch (Exception ex)
            {
                DialogWindow.ShowError($"Importowanie nie powiodło się.\nBłąd:{ex.Message}", "Błąd!");
            }
        }

        private bool TryValidateJsonFile(string filePath, out Dictionary<string, object> normalizedRoot)
        {
            normalizedRoot = new();

            try
            {
                var json = File.ReadAllText(filePath);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                var root = JsonSerializer.Deserialize<
                    Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string>>>>>(json, options);

                if (root == null || !root.TryGetValue("Dependencies", out var deps))
                    return false;

                var cleaned = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                bool hasAtLeastOnePair = false;

                foreach (var (keyName, valuesDict) in deps)
                {
                    if (string.IsNullOrWhiteSpace(keyName) || valuesDict == null) continue;

                    var cleanedValues = new Dictionary<string, Dictionary<string, string>>();

                    foreach (var (valueName, subDict) in valuesDict)
                    {
                        if (string.IsNullOrWhiteSpace(valueName) || subDict == null) continue;

                        var cleanedSub = new Dictionary<string, string>();
                        foreach (var (subKey, subVal) in subDict)
                        {
                            if (!string.IsNullOrWhiteSpace(subKey) &&
                                !string.IsNullOrWhiteSpace(subVal))
                            {
                                cleanedSub[subKey] = subVal;
                            }
                        }

                        if (cleanedSub.Count > 0)
                        {
                            cleanedValues[valueName] = cleanedSub;
                            hasAtLeastOnePair = true;
                        }
                    }

                    if (cleanedValues.Count > 0)
                    {
                        cleaned[keyName] = cleanedValues;
                    }
                }

                if (!hasAtLeastOnePair || cleaned.Count == 0)
                    return false;

                normalizedRoot["Dependencies"] = cleaned;
                return true;
            }
            catch
            {
                return false;
            }
        }


        public SettingsViewModel() 
        {
            MainColor = new SolidColorBrush(constants._settingsColor);
            MainColorDark = new SolidColorBrush(constants._settingsColorDark);
            SaveSettingsCommand = new RelayCommand<object>(_ => { SaveSettings(); });

            LoadFromJson();
            AddKeyCommand = new RelayCommand<object>(_ =>{
                Keys.Add(new KeyItem { Name = "Nowy Klucz" });
            });
            RemoveKeyCommand = new RelayCommand<object>(keyObj => {
                if (keyObj is KeyItem key) Keys.Remove(key);
                Debug.WriteLine("Removed key");
            }, keyObj => keyObj is KeyItem );
            AddValueCommand = new RelayCommand<object>(keyObj => {
                if (keyObj is KeyItem k)
                    k.Values.Add(new ValueItem { Name = "Nowa Wartość" });
            }, keyObj => keyObj is KeyItem );
            RemoveValueCommand = new RelayCommand<object>(valueObj => {
                if (valueObj is ValueItem value)
                {
                    foreach (var key in Keys)
                    {
                        if (key.Values.Contains(value))
                        {
                            key.Values.Remove(value);
                            break;
                        }
                    }
                }
            }, valueObj => valueObj is ValueItem );
            AddSubPairCommand = new RelayCommand<object>(valueObj => {
                if (valueObj is ValueItem v)
                    v.SubPairs.Add(new SubPair { SubKey = "Nowy Podklucz", SubValue = "Nowa Podwartość" });
            }, valueObj => valueObj is ValueItem );
            RemoveSubPairCommand = new RelayCommand<object>(pairObj => {
                if (pairObj is SubPair pair)
                {
                    
                    foreach (var key in Keys)
                    {
                        foreach (var value in key.Values)
                        {

                            if (value.SubPairs.Contains(pair))
                            {
                                value.SubPairs.Remove(pair);
                                return;
                            }
                        }
                    }
                }
            }, pairObj => pairObj is SubPair );
            SaveDependenciesCommand = new RelayCommand<object>(_ => SaveToJson());
            ImportCommand = new RelayCommand<object>(_ => ImportFromJson());

            // Template settings

            //< add key = "TemplateDefaultFileKeyName" value = "_wzór" />
            TemplateDefaultFileKeyName = new SettingsModel()
            {
                Name = nameof(TemplateDefaultFileKeyName),
                DisplayName = "Domyślny klucz do filtracji plików",
                Value = ConfigManager.GetSetting(nameof(TemplateDefaultFileKeyName))
                //Value = ConfigManager.GetSetting("TemplateDefaultFileKeyName")
                
            };
            Settings.Add(TemplateDefaultFileKeyName);

            //< add key = "TemplateDefaultFileKeyFilter" value = "2" />
            TemplateDefaultFileKeyFilter = new SettingsModel()
            {
                Name = nameof(TemplateDefaultFileKeyFilter),
                DisplayName = "Sposób wyszukiwania domyślnego klucza",
                Value = (ConfigManager.GetSetting(nameof(TemplateDefaultFileKeyFilter)) != "Not Found") 
                ? FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(TemplateDefaultFileKeyFilter)))] : FileKeyFilters[0],
                Type = "DropDown"
            };
            Settings.Add(TemplateDefaultFileKeyFilter);

            //< add key = "TemplateFileSelectorInitialDirectory" value = "C:\"/>
            TemplateFileSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(TemplateFileSelectorInitialDirectory),
                DisplayName = "Domyślny folder do ręcznego wczytania plików",
                Value = ConfigManager.GetSetting(nameof(TemplateFileSelectorInitialDirectory))
            };
            Settings.Add(TemplateFileSelectorInitialDirectory);

            //< add key = "TemplateFolderSelectorInitialDirectory" value = "C:\"/>
            TemplateFolderSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(TemplateFolderSelectorInitialDirectory),
                DisplayName = "Domyślny folder do wczytania plików",
                Value = ConfigManager.GetSetting(nameof(TemplateFolderSelectorInitialDirectory))
            };
            Settings.Add(TemplateFolderSelectorInitialDirectory);

            //< add key = "TemplateSaveFolderSelectInitialDirectory" value = "C:\"/>
            TemplateSaveFolderSelectInitialDirectory = new SettingsModel()
            {
                Name = nameof(TemplateSaveFolderSelectInitialDirectory),
                DisplayName = "Domyślny folder przy wyborze folderu docelowego",
                Value = ConfigManager.GetSetting(nameof(TemplateSaveFolderSelectInitialDirectory))
            };
            Settings.Add(TemplateSaveFolderSelectInitialDirectory);

            //< add key = "TemplateSaveFolderDirectory" value = "C:\"/>
            TemplateSaveFolderDirectory = new SettingsModel()
            {
                Name = nameof(TemplateSaveFolderDirectory),
                DisplayName = "Domyślny folder do zapisu szablonów",
                Value = ConfigManager.GetSetting(nameof(TemplateSaveFolderDirectory))
            };
            Settings.Add(TemplateSaveFolderDirectory);


            // Data sheet settings

            //< add key = "DataSheetDefaultFileKeyName" value = "_arkusz" />
            DataSheetDefaultFileKeyName = new SettingsModel()
            {
                Name = nameof(DataSheetDefaultFileKeyName),
                DisplayName = "Domyślny klucz do filtracji plików",
                Value = ConfigManager.GetSetting(nameof(DataSheetDefaultFileKeyName))
            };
            Settings.Add(DataSheetDefaultFileKeyName);

            //< add key = "DataSheetDefaultFileKeyFilter" value = "2" />
            DataSheetDefaultFileKeyFilter = new SettingsModel()
            {
                Name = nameof(DataSheetDefaultFileKeyFilter),
                DisplayName = "Sposób wyszukiwania domyślnego klucza",
                Value = (ConfigManager.GetSetting(nameof(DataSheetDefaultFileKeyFilter)) != "Not Found")
                ? FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(DataSheetDefaultFileKeyFilter)))] : FileKeyFilters[0],
                Type = "DropDown"
            };
            Settings.Add(DataSheetDefaultFileKeyFilter);

            //< add key = "DataSheetFileSelectorInitialDirectory" value = "C:\"/>
            DataSheetFileSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetFileSelectorInitialDirectory),
                DisplayName = "Domyślny folder do ręcznego wczytania plików",
                Value = ConfigManager.GetSetting(nameof(DataSheetFileSelectorInitialDirectory))
            };
            Settings.Add(DataSheetFileSelectorInitialDirectory);

            //< add key = "DataSheetFolderSelectorInitialDirectory" value = "C:\"/>
            DataSheetFolderSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetFolderSelectorInitialDirectory),
                DisplayName = "Domyślny folder do wczytania plików",
                Value = ConfigManager.GetSetting(nameof(DataSheetFolderSelectorInitialDirectory))
            };
            Settings.Add(DataSheetFolderSelectorInitialDirectory);

            //< add key = "DataSheetSaveFolderSelectInitialDirectory" value = "C:\"/>
            DataSheetSaveFolderSelectInitialDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetSaveFolderSelectInitialDirectory),
                DisplayName = "Domyślny folder przy wyborze folderu docelowego",
                Value = ConfigManager.GetSetting(nameof(DataSheetSaveFolderSelectInitialDirectory))
            };
            Settings.Add(DataSheetSaveFolderSelectInitialDirectory);

            //< add key = "DataSheetSaveFolderDirectory" value = "C:\"/>
            DataSheetSaveFolderDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetSaveFolderDirectory),
                DisplayName = "Domyślny folder do zapisu arkusza",
                Value = ConfigManager.GetSetting(nameof(DataSheetSaveFolderDirectory))
            };
            Settings.Add(DataSheetSaveFolderDirectory);

            //< add key = "DataSheetEditInitialDirectory" value = "C:\"/>
            DataSheetEditInitialDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetEditInitialDirectory),
                DisplayName = "Domyślny folder przy wyborze arkusza do edycji",
                Value = ConfigManager.GetSetting(nameof(DataSheetEditInitialDirectory))
            };
            Settings.Add(DataSheetEditInitialDirectory);


            // Document Settings

            //< add key = "DocumentDefaultFileKeyName" value = "_szablon" />
            DocumentDefaultFileKeyName = new SettingsModel()
            {
                Name = nameof(DocumentDefaultFileKeyName),
                DisplayName = "Domyślny klucz do filtracji plików",
                Value = ConfigManager.GetSetting(nameof(DocumentDefaultFileKeyName))
            };
            Settings.Add(DocumentDefaultFileKeyName);

            //< add key = "DocumentDefaultFileKeyFilter" value = "2" />
            DocumentDefaultFileKeyFilter = new SettingsModel()
            {
                Name = nameof(DocumentDefaultFileKeyFilter),
                DisplayName = "Sposób wyszukiwania domyślnego klucza",
                Value = (ConfigManager.GetSetting(nameof(DocumentDefaultFileKeyFilter)) != "Not Found")
                ? FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(DocumentDefaultFileKeyFilter)))] : FileKeyFilters[0],
                Type = "DropDown"
            };
            Settings.Add(DocumentDefaultFileKeyFilter);

            //< add key = "DocumentFileSelectorInitialDirectory" value = "C:\"/>
            DocumentFileSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(DocumentFileSelectorInitialDirectory),
                DisplayName = "Domyślny folder do ręcznego wczytania plików",
                Value = ConfigManager.GetSetting(nameof(DocumentFileSelectorInitialDirectory))
            };
            Settings.Add(DocumentFileSelectorInitialDirectory);

            //< add key = "DocumentFolderSelectorInitialDirectory" value = "C:\"/>
            DocumentFolderSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(DocumentFolderSelectorInitialDirectory),
                DisplayName = "Domyślny folder do wczytania plików",
                Value = ConfigManager.GetSetting(nameof(DocumentFolderSelectorInitialDirectory))
            };
            Settings.Add(DocumentFolderSelectorInitialDirectory);

            //< add key = "DocumentSaveFolderSelectInitialDirectory" value = "C:\"/>
            DocumentSaveFolderSelectInitialDirectory = new SettingsModel()
            {
                Name = nameof(DocumentSaveFolderSelectInitialDirectory),
                DisplayName = "Domyślny folder przy wyborze folderu docelowego",
                Value = ConfigManager.GetSetting(nameof(DocumentSaveFolderSelectInitialDirectory))
            };
            Settings.Add(DocumentSaveFolderSelectInitialDirectory);

            //< add key = "DocumentSaveFolderDirectory" value = "C:\"/>
            DocumentSaveFolderDirectory = new SettingsModel()
            {
                Name = nameof(DocumentSaveFolderDirectory),
                DisplayName = "Domyślny folder do zapisu dokumentów",
                Value = ConfigManager.GetSetting(nameof(DocumentSaveFolderDirectory))
            };
            Settings.Add(DocumentSaveFolderDirectory);

            //< add key = "DocumentLoadDataSheetInitialDirectory" value = "C:\"/>
            DocumentLoadDataSheetInitialDirectory = new SettingsModel()
            {
                Name = nameof(DocumentLoadDataSheetInitialDirectory),
                DisplayName = "Domyślny folder przy wyborze arkusza danych",
                Value = ConfigManager.GetSetting(nameof(DocumentLoadDataSheetInitialDirectory))
            };
            Settings.Add(DocumentLoadDataSheetInitialDirectory);
        } 
    }
}
