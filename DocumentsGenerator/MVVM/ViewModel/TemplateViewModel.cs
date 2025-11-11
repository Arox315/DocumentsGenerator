using DocumentsGenerator.Config;
using DocumentsGenerator.Core;
using DocumentsGenerator.MVVM.Model;
using DocumentsGenerator.MVVM.View;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;


namespace DocumentsGenerator.MVVM.ViewModel
{
    class TemplateViewModel : ObservableObject
    {
        private Constants constants = new Constants();

        public SolidColorBrush LoadedFilesHeader { get; set; }
        public SolidColorBrush LoadedFilesItems {  get; set; }

        public SolidColorBrush MainColor { get; set; }
        public SolidColorBrush MainColorDark { get; set; }

        public ObservableCollection<LoadedFileNameModel>? LoadedFileNames { get; set; }
        public ObservableCollection<string>? FileKeyFilters { get; } = new()
        {
            "Zawiera",
            "Zaczyna się",
            "Kończy się"
        };

        public string DefaultDisplayFilterKeyName { get; set; } = ConfigManager.GetSetting("TemplateDefaultFileKeyName");
        private string _defaultFilterKeyName { get; } = ConfigManager.GetSetting("TemplateDefaultFileKeyName");

        private string _folderSelectHelperText = "Wczytaj pliki z folderu";
        public string FolderSelectHelperText 
        { 
            get => _folderSelectHelperText;
            set
            {
                if (_folderSelectHelperText != value)
                {
                    _folderSelectHelperText = value;
                    OnPropertyChanged(nameof(FolderSelectHelperText));
                }
            }
        }

        private string _saveFolderSelectHelperText = "Wybierz folder docelowy";
        public string SaveFolderSelectHelperText
        {
            get => _saveFolderSelectHelperText;
            set
            {
                if (_saveFolderSelectHelperText != value)
                {
                    _saveFolderSelectHelperText = value;
                    OnPropertyChanged(nameof(SaveFolderSelectHelperText));
                }
            }
        }

        private int _selectedKeyFilterMode;
        private string? _selectedKeyFilter;
        public string SelectedKeyFilter 
        { 
            get => _selectedKeyFilter!;
            set
            {
                if (_selectedKeyFilter != value)
                {
                    _selectedKeyFilter = value;
                    OnPropertyChanged(nameof(SelectedKeyFilter));
                    _selectedKeyFilterMode = FileKeyFilters!.IndexOf(SelectedKeyFilter);
                    //OnSelectedItemChanged();
                }
            }
        }

        private bool _loadFilesWithDefaultKey = true;
        public bool LoadFilesWithDefaultKey
        {
            get => _loadFilesWithDefaultKey;
            set
            {
                if (_loadFilesWithDefaultKey != value)
                {
                    if (_loadAllFiles && !_loadFilesWithDefaultKey) { LoadAllFiles = false; }

                    _loadFilesWithDefaultKey = value;
                    OnPropertyChanged(nameof(LoadFilesWithDefaultKey));
                }
            }
        }

        private bool _loadAllFiles = false;
        public bool LoadAllFiles
        {
            get => _loadAllFiles;
            set
            {
                if (_loadAllFiles != value)
                {
                    if (!_loadAllFiles && _loadFilesWithDefaultKey) { LoadFilesWithDefaultKey = false; }

                    _loadAllFiles = value;
                    OnPropertyChanged(nameof(LoadAllFiles));
                }
            }
        }

        private string _customFilterKeyName = String.Empty;
        public string CustomFilterKeyName
        {
            get => _customFilterKeyName;
            set
            {
                if(_customFilterKeyName != value)
                {
                    _customFilterKeyName = value;
                    OnPropertyChanged(nameof(CustomFilterKeyName));
                    //Debug.WriteLine(_customFilterKeyName);
                }
            }
        }

        private string? _selectedReadFolder;
        private string? _selectedWriteFolder;
        private int _defaultKeyFilterMode;

        public RelayCommand<LoadedFileNameModel> DeleteItemCommand { get; }
        public RelayCommand<object> LoadFileNameCommand { get; }
        public RelayCommand<object> LoadFolderNameCommand { get; }
        public RelayCommand<object> LoadFilesFromFolderCommand { get; }
        public RelayCommand<object> SetSaveFolderCommand {  get; }
        public RelayCommand<object> GenerateTemplatesCommand {  get; }

        public RelayCommand<object> ClearLoadedFilesCommand { get; }

        protected TemplateModel templateModel;

        public TemplateViewModel() 
        {
            if (DefaultDisplayFilterKeyName.StartsWith('_'))
            {
                DefaultDisplayFilterKeyName = "_" + DefaultDisplayFilterKeyName;
            }

            templateModel = new TemplateModel();

            LoadedFilesHeader = new SolidColorBrush(constants._mainColorDark);
            LoadedFilesItems = new SolidColorBrush(constants._mainColor);

            MainColor = new SolidColorBrush(constants._templateColor);
            MainColorDark = new SolidColorBrush(constants._templateColorDark);

            LoadedFileNames = new ObservableCollection<LoadedFileNameModel>();
           
            SelectedKeyFilter = FileKeyFilters.First();

            ClearLoadedFilesCommand = new RelayCommand<object>(_ => { LoadedFileNames.Clear(); });

            DeleteItemCommand = new RelayCommand<LoadedFileNameModel>(
                item => LoadedFileNames.Remove(item),
                item => item != null);

            LoadFileNameCommand = new RelayCommand<object>(_ =>
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Dokumenty | *.docx";
                ofd.InitialDirectory = ConfigManager.GetSetting("TemplateFileSelectorInitialDirectory");
                ofd.Title = "Wybierz dokument(y) wzorcow(y/e)...";
                ofd.Multiselect = true;
                bool? success = ofd.ShowDialog();
                if (success == true)
                {
                    string[] paths = ofd.FileNames;
                    string[] fileNames = ofd.SafeFileNames;
                    for (int i = 0; i < paths.Length; i++) {
                        LoadedFileNames.Add(new LoadedFileNameModel
                        {
                            FilePath = paths[i],
                            FileName = fileNames[i],
                            FileKey = _defaultFilterKeyName
                        });
                    }
                }
            });

            LoadFolderNameCommand = new RelayCommand<object>(_ => { 
                OpenFolderDialog ofd = new OpenFolderDialog();
                ofd.InitialDirectory = ConfigManager.GetSetting("TemplateFolderSelectorInitialDirectory");
                ofd.Title = "Wybierz folder...";

                bool? success = ofd.ShowDialog();
                if (success == true) 
                {
                    string path = ofd.FolderName;
                    string folderName = ofd.SafeFolderName;
                    FolderSelectHelperText = path;
                    _selectedReadFolder = path;
                }

            });

            LoadFilesFromFolderCommand = new RelayCommand<object>(_ => {

                if(_selectedReadFolder == null)
                {
                    DialogWindow.Show("Nie wybrano folderu wejściowego!", "Błąd", DialogType.Ok, DialogIcon.Error);
                }
                else
                {
                    _LoadFilesFromFolder();
                }

            });

            SetSaveFolderCommand = new RelayCommand<object>(_ => {
                OpenFolderDialog ofd = new OpenFolderDialog();
                ofd.InitialDirectory = ConfigManager.GetSetting("TemplateSaveFolderSelectInitialDirectory");
                ofd.Title = "Wybierz folder...";

                bool? success = ofd.ShowDialog();
                if (success == true) 
                {
                    string path = ofd.FolderName;
                    string folderName = ofd.SafeFolderName;
                    SaveFolderSelectHelperText = path;
                    _selectedWriteFolder = path;
                }
            });

            GenerateTemplatesCommand = new RelayCommand<object>(_ => {
                bool isError = false;

                if(LoadedFileNames.Count == 0)
                {
                    DialogWindow.ShowError("Nie wybrano żadnych dokumentów wzorcowych!", "Błąd!");
                    return;
                }

                if(_selectedWriteFolder == null && ConfigManager.GetSetting("TemplateSaveFolderDirectory") == "")
                {
                    DialogWindow.ShowError("Nie wybrano folderu docelowego!", "Błąd!");
                    return;
                }
                
                if(_selectedWriteFolder == null)
                {
                    string defaultSaveFolder = ConfigManager.GetSetting("TemplateSaveFolderDirectory");
                    DialogResultEx? proceed = DialogWindow.Show($"Nie wybrano folderu docelowego. Pliki zostaną zapisane w domyślnym folderze: {defaultSaveFolder}. Czy kontynuować?", "Zapis plików", DialogType.YesNo, DialogIcon.Question);

                    if (proceed != DialogResultEx.Yes) {
                        return;
                    }
                }

                string _outFolder = _selectedWriteFolder ?? ConfigManager.GetSetting("TemplateSaveFolderDirectory");
                templateModel.LoadedFileNames = LoadedFileNames;
                templateModel.GenerateTemplates(_selectedReadFolder!, _outFolder, ref isError);

                if (isError)
                {
                    DialogWindow.Show($"Generowanie zakończone z błędami. Szablony zostały wygenerowane w:\n{_outFolder}", "Generowanie zakończone", DialogType.Ok, DialogIcon.Warning);
                }
                else
                {
                    DialogWindow.ShowInfo($"Generowanie zakończone pomyślnie. Szablony zostały wygenerowane w:\n{_outFolder}", "Generowanie zakończone");
                }
            });

        }
   
        private void _LoadFilesFromFolder()
        {
            LoadedFileNames!.Clear();
            _defaultKeyFilterMode = Int32.Parse(ConfigManager.GetSetting("TemplateDefaultFileKeyFilter"));
            string[]? files = Directory.GetFiles(_selectedReadFolder!, "*.docx");
            int loadedFilesCount = 0;

            foreach (string file in files) 
            {
                if (_loadAllFiles)
                {
                    LoadedFileNames.Add(new LoadedFileNameModel
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        FileKey = ""
                    });
                    loadedFilesCount++;
                }

                if (!_loadAllFiles && _loadFilesWithDefaultKey)
                {
                    switch (_defaultKeyFilterMode)
                    {
                        case 0:
                            if (Path.GetFileNameWithoutExtension(file).Contains(_defaultFilterKeyName))
                            {
                                LoadedFileNames.Add(new LoadedFileNameModel
                                {
                                    FilePath = file,
                                    FileName = Path.GetFileName(file),
                                    FileKey = _defaultFilterKeyName
                                });
                                loadedFilesCount++;
                            }
                            break;
                        case 1:
                            if (Path.GetFileNameWithoutExtension(file).StartsWith(_defaultFilterKeyName))
                            {
                                LoadedFileNames.Add(new LoadedFileNameModel
                                {
                                    FilePath = file,
                                    FileName = Path.GetFileName(file),
                                    FileKey = _defaultFilterKeyName
                                });
                                loadedFilesCount++;
                            }
                            break;
                        case 2:
                            if (Path.GetFileNameWithoutExtension(file).EndsWith(_defaultFilterKeyName))
                            {
                                LoadedFileNames.Add(new LoadedFileNameModel
                                {
                                    FilePath = file,
                                    FileName = Path.GetFileName(file),
                                    FileKey = _defaultFilterKeyName
                                });
                                loadedFilesCount++;
                            }
                            break;
                    }
                }

                if (!_loadAllFiles && _selectedKeyFilter != null && _selectedKeyFilter != _defaultFilterKeyName)
                {
                    switch (_selectedKeyFilterMode)
                    {
                        case 0:
                            if (Path.GetFileNameWithoutExtension(file).Contains(_selectedKeyFilter))
                            {
                                LoadedFileNames.Add(new LoadedFileNameModel
                                {
                                    FilePath = file,
                                    FileName = Path.GetFileName(file),
                                    FileKey = _selectedKeyFilter
                                });
                                loadedFilesCount++;
                            }
                            break;
                        case 1:
                            if (Path.GetFileNameWithoutExtension(file).StartsWith(_selectedKeyFilter))
                            {
                                LoadedFileNames.Add(new LoadedFileNameModel
                                {
                                    FilePath = file,
                                    FileName = Path.GetFileName(file),
                                    FileKey = _selectedKeyFilter
                                });
                                loadedFilesCount++;
                            }
                            break;
                        case 2:
                            if (Path.GetFileNameWithoutExtension(file).EndsWith(_selectedKeyFilter))
                            {
                                LoadedFileNames.Add(new LoadedFileNameModel
                                {
                                    FilePath = file,
                                    FileName = Path.GetFileName(file),
                                    FileKey = _selectedKeyFilter
                                });
                                loadedFilesCount++;
                            }
                            break;
                    }
                }
            }

            if (loadedFilesCount == 0) {
                DialogWindow.Show("Nie wczytano plików - Brak plików spełniających kryteria", "Brak plików", DialogType.Ok, DialogIcon.Warning);
            }

            FolderSelectHelperText = "Wczytaj pliki z folderu";
            _selectedReadFolder = null;
        }
    }
}
