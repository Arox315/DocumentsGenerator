using DocumentFormat.OpenXml.Spreadsheet;
using DocumentsGenerator.Config;
using DocumentsGenerator.Core;
using DocumentsGenerator.MVVM.Model;
using DocumentsGenerator.MVVM.View;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace DocumentsGenerator.MVVM.ViewModel
{
    class DocumentViewModel : ObservableObject
    {
        private readonly Constants constants = new Constants();

        public SolidColorBrush MainColor { get; set; }
        public SolidColorBrush MainColorDark { get; set; }
        public SolidColorBrush LoadedFilesHeader { get; set; }
        public SolidColorBrush LoadedFilesItems { get; set; }

        public ObservableCollection<LoadedFileNameModel>? LoadedFileNames { get; set; }
        public ObservableCollection<string>? FileKeyFilters { get; } = new()
        {
            "Zawiera",
            "Zaczyna się",
            "Kończy się"
        };
        
        public string DefaultDisplayFilterKeyName { get; set; } = ConfigManager.GetSetting("DocumentDefaultFileKeyName");
        private string _defaultFilterKeyName { get; } = ConfigManager.GetSetting("DocumentDefaultFileKeyName");

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

        private string _dataSheetSelectHelperText = "Wczytaj arkusz danych";
        public string DataSheetSelectHelperText
        {
            get => _dataSheetSelectHelperText;
            set
            {
                if (_dataSheetSelectHelperText != value)
                {
                    _dataSheetSelectHelperText = value;
                    OnPropertyChanged(nameof(DataSheetSelectHelperText));
                }
            }
        }

        private string _customFilterKeyName = String.Empty;
        public string CustomFilterKeyName
        {
            get => _customFilterKeyName;
            set
            {
                if (_customFilterKeyName != value)
                {
                    _customFilterKeyName = value;
                    OnPropertyChanged(nameof(CustomFilterKeyName));
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
                    _loadFilesWithDefaultKey = value;
                    OnPropertyChanged(nameof(LoadFilesWithDefaultKey));
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
                }
            }
        }

        public RelayCommand<LoadedFileNameModel> DeleteItemCommand { get; }
        public RelayCommand<object> LoadFileNameCommand { get; }
        public RelayCommand<object> LoadFolderNameCommand { get; }
        public RelayCommand<object> LoadFilesFromFolderCommand { get; }
        public RelayCommand<object> SetSaveFolderCommand { get; }
        public RelayCommand<object> LoadDataSheetCommand { get; }
        public RelayCommand<object> GenerateDocumentsCommand { get; }
        public RelayCommand<object> ClearLoadedFilesCommand { get; }

        protected DocumentModel documentModel;

        private string? _selectedReadFolder;
        private string? _selectedWriteFolder;
        private int _defaultKeyFilterMode;
        private string? _currentDataSheet;

        public DocumentViewModel() 
        {
            if (DefaultDisplayFilterKeyName.StartsWith('_'))
            {
                DefaultDisplayFilterKeyName = "_" + DefaultDisplayFilterKeyName;
            }
            documentModel = new DocumentModel();
            MainColor = new SolidColorBrush(constants._documentColor);
            MainColorDark = new SolidColorBrush(constants._documentColorDark);
            LoadedFilesHeader = new SolidColorBrush(constants._mainColorDark);
            LoadedFilesItems = new SolidColorBrush(constants._mainColor);

            SelectedKeyFilter = FileKeyFilters.First();

            LoadedFileNames = new ObservableCollection<LoadedFileNameModel>();

            ClearLoadedFilesCommand = new RelayCommand<object>(_ => { LoadedFileNames.Clear(); });
            DeleteItemCommand = new RelayCommand<LoadedFileNameModel>(
              item => LoadedFileNames.Remove(item),
              item => item != null);
            LoadFileNameCommand = new RelayCommand<object>(_ =>
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Szablony | *.docx";
                ofd.InitialDirectory = ConfigManager.GetSetting("DocumentFileSelectorInitialDirectory");
                ofd.Title = "Wybierz szablon(y)...";
                ofd.Multiselect = true;
                bool? success = ofd.ShowDialog();
                if (success == true)
                {
                    string[] paths = ofd.FileNames;
                    string[] fileNames = ofd.SafeFileNames;
                    for (int i = 0; i < paths.Length; i++)
                    {
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
                ofd.InitialDirectory = ConfigManager.GetSetting("DocumentFolderSelectorInitialDirectory");
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

                if (_selectedReadFolder == null)
                {
                    DialogWindow.Show("Nie wybrano folderu wejściowego!", "Błąd", DialogType.Ok, DialogIcon.Error);
                }
                else
                {
                    LoadFilesFromFolder();
                }

            });
            SetSaveFolderCommand = new RelayCommand<object>(_ => {
                OpenFolderDialog ofd = new OpenFolderDialog();
                ofd.InitialDirectory = ConfigManager.GetSetting("DocumentSaveFolderSelectInitialDirectory");
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
            LoadDataSheetCommand = new RelayCommand<object>(_ => {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Arkusze danych | *.xml";
                ofd.InitialDirectory = ConfigManager.GetSetting("DocumentLoadDataSheetInitialDirectory");
                ofd.Title = "Wybierz arkusz danych...";
                bool? success = ofd.ShowDialog();
                if (success == true)
                {
                    _currentDataSheet = ofd.FileName;
                    DataSheetSelectHelperText = ofd.SafeFileName;
                }
            });
            GenerateDocumentsCommand = new RelayCommand<object>(_ => {

                bool isError = false;
                if (LoadedFileNames.Count == 0)
                {
                    DialogWindow.ShowError("Nie wybrano żadnych szablonów!", "Błąd!");
                    return;
                }

                if (_currentDataSheet == null)
                {
                    DialogWindow.ShowError("Nie wczytano żadnego arkusza danych!", "Błąd!");
                    return;
                }

                if (_selectedWriteFolder == null && ConfigManager.GetSetting("DocumentSaveFolderDirectory") == "")
                {
                    DialogWindow.ShowError("Nie wybrano folderu docelowego!", "Błąd!");
                    return;
                }

                if (_selectedWriteFolder == null)
                {
                    string defaultSaveFolder = ConfigManager.GetSetting("DocumentSaveFolderDirectory");
                    DialogResultEx? proceed = DialogWindow.Show($"Nie wybrano folderu docelowego. Pliki zostaną zapisane w domyślnym folderze: {defaultSaveFolder}. Czy kontynuować?", "Zapis pliku", DialogType.YesNo, DialogIcon.Question);

                    if (proceed != DialogResultEx.Yes)
                    {
                        return;
                    }
                }

                string _outFolder = _selectedWriteFolder ?? ConfigManager.GetSetting("DocumentSaveFolderDirectory");
                documentModel.LoadedFileNames = LoadedFileNames;
                documentModel.GenerateDocuments(_outFolder, _currentDataSheet!, ref isError);

                if (isError) 
                {
                    DialogWindow.Show($"Generowanie zakończone z błędami. Dokumenty zostały wygenerowane w:\n{_outFolder}", "Generowanie zakończone", DialogType.Ok, DialogIcon.Warning);
                }
                else
                {
                    DialogWindow.ShowInfo($"Generowanie zakończone pomyślnie. Dokumenty zostały wygenerowane w:\n{_outFolder}", "Generowanie zakończone");
                }
   
            });
        }

        private void LoadFilesFromFolder()
        {
            LoadedFileNames!.Clear();
            _defaultKeyFilterMode = Int32.Parse(ConfigManager.GetSetting("DocumentDefaultFileKeyFilter"));
            string[]? files = Directory.GetFiles(_selectedReadFolder!, "*.docx");
            int loadedFilesCount = 0;

            foreach (string file in files)
            {
                if (_loadFilesWithDefaultKey)
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

                if (_selectedKeyFilter != null && _selectedKeyFilter != _defaultFilterKeyName)
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

            if (loadedFilesCount == 0)
            {
                DialogWindow.Show("Nie wczytano plików - Brak plików spełniających kryteria", "Brak plików", DialogType.Ok, DialogIcon.Warning);
            }

            FolderSelectHelperText = "Wczytaj pliki z folderu";
            _selectedReadFolder = null;
        }
    }
}
