using DocumentFormat.OpenXml.Wordprocessing;
using DocumentsGenerator.Config;
using DocumentsGenerator.Core;
using DocumentsGenerator.MVVM.Model;
using DocumentsGenerator.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

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

        public void SaveSettings()
        {
            foreach(var setting in Settings)
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
                } catch {
                    DialogWindow.ShowError($"Błąd podczas zapisu ustawienia:\n{setting.DisplayName}", "Błąd!");
                }
                
            }

            DialogWindow.ShowInfo("Zapisywanie ustawień zakończone", "Zapis ustawień");
        }

        public SettingsViewModel() 
        {
            MainColor = new SolidColorBrush(constants._settingsColor);
            MainColorDark = new SolidColorBrush(constants._settingsColorDark);
            SaveSettingsCommand = new RelayCommand<object>(_ => { SaveSettings(); });

            // Template settings

            //< add key = "TemplateDefaultFileKeyName" value = "_wzór" />
            TemplateDefaultFileKeyName = new SettingsModel()
            {
                Name = nameof(TemplateDefaultFileKeyName),
                DisplayName = "Domyślny klucz do filtracji plików",
                Value = ConfigManager.GetSetting(nameof(TemplateDefaultFileKeyName))
                
            };
            Settings.Add(TemplateDefaultFileKeyName);

            //< add key = "TemplateDefaultFileKeyFilter" value = "2" />
            TemplateDefaultFileKeyFilter = new SettingsModel()
            {
                Name = nameof(TemplateDefaultFileKeyFilter),
                DisplayName = "Sposób wyszukiwania domyślnego klucza",
                Value = FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(TemplateDefaultFileKeyFilter)) ?? "0")],
                Type="DropDown"
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
                Value = FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(DataSheetDefaultFileKeyFilter)) ?? "0")],
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
                Value = FileKeyFilters[Int32.Parse(ConfigManager.GetSetting(nameof(DocumentDefaultFileKeyFilter)) ?? "0")],
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
