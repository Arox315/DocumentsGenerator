using DocumentsGenerator.Config;
using DocumentsGenerator.Core;
using DocumentsGenerator.MVVM.Model;
using DocumentsGenerator.MVVM.View;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Media;
using Microsoft.Win32;


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


        public ObservableCollection<AllKeysModel> AllKeys { get; } = new();
        public RelayCommand<AllKeysModel> ClearValueCommand { get; }
        public RelayCommand<AllKeysModel> DeleteValueCommand { get; }
        public RelayCommand<object> SaveAllKeysCommand { get; }
        public RelayCommand<object> AddNewKeyCommand { get; }
        public RelayCommand<object> ImportKeysCommand { get; }


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
            if (!DependencyManager.TryBuildDependenciesValidated(Keys, out var dependencies, out var errors))
            {
                DialogWindow.Show("Zapis nie powiódł się z powodu następujących problemów:\n\n" + string.Join("\n", errors),
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
                string filePath = DependencyManager.GetDefaultDependenciesPath();
                var json = JsonSerializer.Serialize(root, options);
                File.WriteAllText(filePath, json);
                DialogWindow.Show("Pomyślnie zapisano zmiany.", "Zapis zależności", DialogType.Ok, DialogIcon.Info);
            }
            catch (Exception ex)
            {
                DialogWindow.ShowError($"Błąd podczas zapisu pliku.\nBłąd:{ex.Message}", "Błąd zapisu!");
            }
        }
        private void ImportFromJson()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Importuj zależności...",
                Filter = "Pliki JSON(*.json)|*.json",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() != true) return;
            
            if (!DependencyManager.TryValidateAndNormalizeJsonNoDuplicates(ofd.FileName, out var normalizedRoot, out var errors))
            {
                DialogWindow.ShowError("Wybrany plik nie jest prawidłowym plikiem JSON zależności.\n\n" + string.Join("\n", errors), "Błąd importu!");
                return;
            }

            var targetPath = DependencyManager.GetDefaultDependenciesPath();

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(normalizedRoot, options);
                File.WriteAllText(targetPath, json);

                if (!LoadFromJson())
                {
                    DialogWindow.Show("Plik zapisano, ale nie udało się go ponownie wczytać.", "Importowanie pliku", DialogType.Ok, DialogIcon.Warning);
                }
                else
                {
                    DialogWindow.Show("Pomyślnie zaimportowano i wczytano dane.", "Importowanie pliku", DialogType.Ok, DialogIcon.Info);
                }
            }
            catch (Exception ex)
            {
                DialogWindow.ShowError($"Błąd podczas zapisu pliku.\nBłąd: {ex.Message}", "Błąd importu!");
            }
        }
        public bool LoadFromJson()
        {
            string filePath = DependencyManager.GetDefaultDependenciesPath();
            if (!File.Exists(filePath)) return false;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            try
            {
                var root = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string,
                    Dictionary<string, string>>>>>(File.ReadAllText(filePath),
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

        
        public ObservableCollection<string> KeySuggestions { get; } = new();
        private void LoadSuggestions()
        {
            var keySuggestions = Suggestions.LoadKeySuggestions(DependencyKeysManager.allKeysPath);
            KeySuggestions.Clear();
            AllKeys.Clear();
            foreach (string suggestion in keySuggestions) 
            { 
                KeySuggestions.Add(suggestion);
                AllKeys.Add(new AllKeysModel() { Value = suggestion });
            }
        }
        private void ClearValue(AllKeysModel key)
        {
            if (key != null)
            {
                key.Value = string.Empty;
            }
        }
        private void ImportKeys()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Importuj klucze...",
                Filter = "Pliki JSON(*.json)|*.json",
                CheckFileExists = true
            };

            if (ofd.ShowDialog() != true) return;

            bool success = DependencyKeysManager.ImportAllKeysFile(ofd.FileName, out var errors);

            if (success) {
                DialogWindow.ShowInfo("Pomyślnie zaimportowano dane.", "Importowanie danych");
                LoadSuggestions();
                LoadFromJson();
            }
            else
            {
                DialogWindow.ShowError("Importowanie danych nie powiodło się z powodu następujących problemów:\n\n" + string.Join("\n", errors), "Błąd importu!");
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
                {
                    var newValue = new ValueItem { Name = "Nowa Wartość" };
                    DependencyManager.PrepopulateSubPairsFromLatest(k, newValue, "Nowa Podwartość");
                    k.Values.Add(newValue);
                }
            }, keyObj => keyObj is KeyItem);

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


            ClearValueCommand = new RelayCommand<AllKeysModel>(key =>
            {
                ClearValue(key);
            });

            DeleteValueCommand = new RelayCommand<AllKeysModel>(
                key => AllKeys.Remove(key),
                key => key != null);

            AddNewKeyCommand = new RelayCommand<object>(_ => {
                AllKeys.Insert(0, new AllKeysModel() { Value = "" });
            });

            SaveAllKeysCommand = new RelayCommand<object>(_=> {
                try
                {
                    bool success = DependencyKeysManager.UpdateAllKeysFile(AllKeys);
                    if (success)
                    {
                        DialogWindow.ShowInfo("Pomyślnie zapisano zmiany.", "Zapis pliku");
                    }
                    else
                    {
                        DialogWindow.ShowInfo("Zapisywanie zakończone. Brak nowych zmian – brak nowych kluczy.", "Zapis pliku");
                    }

                    LoadSuggestions();
                }
                catch (JsonSanitizationException ex)
                {
                    DialogWindow.ShowError($"Błąd podczas zapisu! - Wykryto pustą wartość po sanityzacji.\n Błąd: {ex.Message}", "Błąd zapisu!");
                }
                catch (JsonValueIsEmptyException ex)
                {
                    DialogWindow.ShowError($"Błąd podczas zapisu! - Wykryto pustą wartość.\n Błąd: {ex.Message}", "Błąd zapisu!");
                }
                catch (JsonValueIsDuplicateException ex)
                {
                    DialogWindow.ShowError($"Błąd podczas zapisu! - Wykryto zduplikowaną wartość: {ex.Value}.\n Błąd: {ex.Message}", "Błąd zapisu!");
                }
                catch (Exception ex) {
                    DialogWindow.ShowError($"Błąd podczas zapisu!\n Błąd: {ex.Message}", "Błąd zapisu!");
                }

            });

            ImportKeysCommand = new RelayCommand<object>(_ => ImportKeys());

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
                DisplayName = "Miejsce klucza w nazwie pliku podczas filtracji",
                Value = (ConfigManager.GetSetting(nameof(TemplateDefaultFileKeyFilter)) != "Not Found") 
                ? FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(TemplateDefaultFileKeyFilter)))] : FileKeyFilters[0],
                Type = "DropDown"
            };
            Settings.Add(TemplateDefaultFileKeyFilter);

            //< add key = "TemplateFileSelectorInitialDirectory" value = "C:\"/>
            TemplateFileSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(TemplateFileSelectorInitialDirectory),
                DisplayName = "Początkowy katalog ręcznego wczytywania plików",
                Value = ConfigManager.GetSetting(nameof(TemplateFileSelectorInitialDirectory))
            };
            Settings.Add(TemplateFileSelectorInitialDirectory);

            //< add key = "TemplateFolderSelectorInitialDirectory" value = "C:\"/>
            TemplateFolderSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(TemplateFolderSelectorInitialDirectory),
                DisplayName = "Początkowy katalog wczytywania plików",
                Value = ConfigManager.GetSetting(nameof(TemplateFolderSelectorInitialDirectory))
            };
            Settings.Add(TemplateFolderSelectorInitialDirectory);

            //< add key = "TemplateSaveFolderSelectInitialDirectory" value = "C:\"/>
            TemplateSaveFolderSelectInitialDirectory = new SettingsModel()
            {
                Name = nameof(TemplateSaveFolderSelectInitialDirectory),
                DisplayName = "Początkowy katalog wyboru folderu zapisu",
                Value = ConfigManager.GetSetting(nameof(TemplateSaveFolderSelectInitialDirectory))
            };
            Settings.Add(TemplateSaveFolderSelectInitialDirectory);

            //< add key = "TemplateSaveFolderDirectory" value = "C:\"/>
            TemplateSaveFolderDirectory = new SettingsModel()
            {
                Name = nameof(TemplateSaveFolderDirectory),
                DisplayName = "Domyślny folder zapisu",
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
                DisplayName = "Miejsce klucza w nazwie pliku podczas filtracji",
                Value = (ConfigManager.GetSetting(nameof(DataSheetDefaultFileKeyFilter)) != "Not Found")
                ? FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(DataSheetDefaultFileKeyFilter)))] : FileKeyFilters[0],
                Type = "DropDown"
            };
            Settings.Add(DataSheetDefaultFileKeyFilter);

            //< add key = "DataSheetFileSelectorInitialDirectory" value = "C:\"/>
            DataSheetFileSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetFileSelectorInitialDirectory),
                DisplayName = "Początkowy katalog ręcznego wczytywania plików",
                Value = ConfigManager.GetSetting(nameof(DataSheetFileSelectorInitialDirectory))
            };
            Settings.Add(DataSheetFileSelectorInitialDirectory);

            //< add key = "DataSheetFolderSelectorInitialDirectory" value = "C:\"/>
            DataSheetFolderSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetFolderSelectorInitialDirectory),
                DisplayName = "Początkowy katalog wczytywania plików",
                Value = ConfigManager.GetSetting(nameof(DataSheetFolderSelectorInitialDirectory))
            };
            Settings.Add(DataSheetFolderSelectorInitialDirectory);

            //< add key = "DataSheetSaveFolderSelectInitialDirectory" value = "C:\"/>
            DataSheetSaveFolderSelectInitialDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetSaveFolderSelectInitialDirectory),
                DisplayName = "Początkowy katalog wyboru folderu zapisu",
                Value = ConfigManager.GetSetting(nameof(DataSheetSaveFolderSelectInitialDirectory))
            };
            Settings.Add(DataSheetSaveFolderSelectInitialDirectory);

            //< add key = "DataSheetSaveFolderDirectory" value = "C:\"/>
            DataSheetSaveFolderDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetSaveFolderDirectory),
                DisplayName = "Domyślny folder do zapisu",
                Value = ConfigManager.GetSetting(nameof(DataSheetSaveFolderDirectory))
            };
            Settings.Add(DataSheetSaveFolderDirectory);

            //< add key = "DataSheetEditInitialDirectory" value = "C:\"/>
            DataSheetEditInitialDirectory = new SettingsModel()
            {
                Name = nameof(DataSheetEditInitialDirectory),
                DisplayName = "Początkowy katalog wyboru arkusza danych do edycji",
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
                DisplayName = "Miejsce klucza w nazwie pliku podczas filtracji",
                Value = (ConfigManager.GetSetting(nameof(DocumentDefaultFileKeyFilter)) != "Not Found")
                ? FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(DocumentDefaultFileKeyFilter)))] : FileKeyFilters[0],
                Type = "DropDown"
            };
            Settings.Add(DocumentDefaultFileKeyFilter);

            //< add key = "DocumentFileSelectorInitialDirectory" value = "C:\"/>
            DocumentFileSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(DocumentFileSelectorInitialDirectory),
                DisplayName = "Początkowy katalog ręcznego wczytywania plików",
                Value = ConfigManager.GetSetting(nameof(DocumentFileSelectorInitialDirectory))
            };
            Settings.Add(DocumentFileSelectorInitialDirectory);

            //< add key = "DocumentFolderSelectorInitialDirectory" value = "C:\"/>
            DocumentFolderSelectorInitialDirectory = new SettingsModel()
            {
                Name = nameof(DocumentFolderSelectorInitialDirectory),
                DisplayName = "Początkowy katalog wczytywania plików",
                Value = ConfigManager.GetSetting(nameof(DocumentFolderSelectorInitialDirectory))
            };
            Settings.Add(DocumentFolderSelectorInitialDirectory);

            //< add key = "DocumentSaveFolderSelectInitialDirectory" value = "C:\"/>
            DocumentSaveFolderSelectInitialDirectory = new SettingsModel()
            {
                Name = nameof(DocumentSaveFolderSelectInitialDirectory),
                DisplayName = "Początkowy katalog wyboru folderu zapisu",
                Value = ConfigManager.GetSetting(nameof(DocumentSaveFolderSelectInitialDirectory))
            };
            Settings.Add(DocumentSaveFolderSelectInitialDirectory);

            //< add key = "DocumentSaveFolderDirectory" value = "C:\"/>
            DocumentSaveFolderDirectory = new SettingsModel()
            {
                Name = nameof(DocumentSaveFolderDirectory),
                DisplayName = "Domyślny folder do zapisu",
                Value = ConfigManager.GetSetting(nameof(DocumentSaveFolderDirectory))
            };
            Settings.Add(DocumentSaveFolderDirectory);

            //< add key = "DocumentLoadDataSheetInitialDirectory" value = "C:\"/>
            DocumentLoadDataSheetInitialDirectory = new SettingsModel()
            {
                Name = nameof(DocumentLoadDataSheetInitialDirectory),
                DisplayName = "Początkowy katalog wyboru arkusza danych",
                Value = ConfigManager.GetSetting(nameof(DocumentLoadDataSheetInitialDirectory))
            };
            Settings.Add(DocumentLoadDataSheetInitialDirectory);

            LoadSuggestions();
        } 
    }
}
