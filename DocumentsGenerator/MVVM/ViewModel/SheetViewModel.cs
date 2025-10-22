using DocumentFormat.OpenXml.Wordprocessing;
using DocumentsGenerator.Config;
using DocumentsGenerator.Core;
using DocumentsGenerator.MVVM.Model;
using DocumentsGenerator.MVVM.View;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace DocumentsGenerator.MVVM.ViewModel
{
    class SheetViewModel : ObservableObject
    {
        private readonly Constants constants = new Constants();

        public SolidColorBrush MainColor { get; set; }
        public SolidColorBrush MainColorDark { get; set; }
        public SolidColorBrush LoadedFilesHeader { get; set; }
        public SolidColorBrush LoadedFilesItems { get; set; }

        public ObservableCollection<LoadedFileNameModel>? LoadedFileNames { get; set; }
        public ObservableCollection<DataSheetItemModel>? Items { get; set; }
        public ObservableCollection<string>? FileKeyFilters { get; } = new()
        {
            "Zawiera",
            "Zaczyna się",
            "Kończy się"
        };

        public string DefaultDisplayFilterKeyName { get; } = "_" + ConfigManager.GetSetting("DataSheetDefaultFileKeyName");
        private string _defaultFilterKeyName { get; } = ConfigManager.GetSetting("DataSheetDefaultFileKeyName");

        private string _fileSelectHelperText = "Wybierz plik";
        public string FileSelectHelperText
        {
            get { return _fileSelectHelperText; }
            set {
                if (_fileSelectHelperText != value) {
                    _fileSelectHelperText = value;
                    OnPropertyChanged(nameof(FileSelectHelperText));
                }
            }
        }

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
                    //Debug.WriteLine(_customFilterKeyName);
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
                    //Debug.WriteLine($"Value changed to {_loadFilesWithDefaultKey}");
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

        public RelayCommand<DataSheetItemModel> ClearValueCommand { get; }
        public RelayCommand<LoadedFileNameModel> DeleteItemCommand { get; }
        public RelayCommand<object> LoadFileDataCommand { get; }
        public RelayCommand<object> SaveDataSheetCommand { get; }


        public RelayCommand<object> LoadFileNameCommand { get; }
        public RelayCommand<object> LoadFolderNameCommand { get; }
        public RelayCommand<object> LoadFilesFromFolderCommand { get; }
        public RelayCommand<object> SetSaveFolderCommand { get; }
        public RelayCommand<DataSheetItemModel> DeleteDataRowCommand { get; }
        public RelayCommand<object> MergeDataSheetsCommand { get; }

        private string? _selectedReadFolder;
        private string? _selectedWriteFolder;
        private int _defaultKeyFilterMode;

        private string _currentDataSheet = "";

        protected DataSheetModel dataSheetModel;
        public SheetViewModel() 
        {
            dataSheetModel = new DataSheetModel();
            MainColor = new SolidColorBrush(constants._sheetColor);
            MainColorDark = new SolidColorBrush(constants._sheetColorDark);
            LoadedFilesHeader = new SolidColorBrush(constants._mainColorDark);
            LoadedFilesItems = new SolidColorBrush(constants._mainColor);

            SelectedKeyFilter = FileKeyFilters.First();

            Items = new ObservableCollection<DataSheetItemModel>();
            LoadedFileNames = new ObservableCollection<LoadedFileNameModel>();

            ClearValueCommand = new RelayCommand<DataSheetItemModel>(item =>
            {
                ClearValue(item);
            });
            DeleteDataRowCommand = new RelayCommand<DataSheetItemModel>(
                item => Items.Remove(item),
                item => item != null);

            LoadFileDataCommand = new RelayCommand<object>(_ => {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Arkusze danych | *.xml";
                ofd.InitialDirectory = ConfigManager.GetSetting("DataSheetEditInitialDirectory");
                ofd.Title = "Wybierz arkusz danych...";
                bool? success = ofd.ShowDialog();
                if (success == true) 
                {
                    Items.Clear();
                    _currentDataSheet = ofd.FileName;
                    XDocument doc = XDocument.Load(ofd.FileName,LoadOptions.PreserveWhitespace);
                    foreach(var element in doc.Root!.Descendants())
                    {
                        Items.Add(new DataSheetItemModel
                        {
                            Key=element.Name.LocalName.Replace("_","__"),
                            Value=element.Value
                        });
                    }
                }

            });
            SaveDataSheetCommand = new RelayCommand<object>(_ => {

                if(_currentDataSheet == "")
                {
                    DialogWindow.ShowError("Nie wczytano żadnego arkusza danych!", "Błąd!");
                    return;
                }

                XDocument doc = XDocument.Load(_currentDataSheet);
                foreach (var item in Items)
                {
                    var element = doc.Root!.Element("{template-data}" + item.Key!);
                    if (element != null)
                    {
                        element.SetValue(item.Value!);
                    }
                }
                doc.Save(_currentDataSheet);
                DialogWindow.ShowInfo("Pomyślnie zapisano zmiany.","Zapis pliku");
            });


            DeleteItemCommand = new RelayCommand<LoadedFileNameModel>(
               item => LoadedFileNames.Remove(item),
               item => item != null);
            LoadFileNameCommand = new RelayCommand<object>(_ =>
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "Arkusze danych | *.xml";
                ofd.InitialDirectory = ConfigManager.GetSetting("DataSheetFileSelectorInitialDirectory");
                ofd.Title = "Wybierz arkusz(e)...";
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
                ofd.InitialDirectory = ConfigManager.GetSetting("DataSheetFolderSelectorInitialDirectory");
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
                ofd.InitialDirectory = ConfigManager.GetSetting("DataSheetSaveFolderSelectInitialDirectory");
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
            MergeDataSheetsCommand = new RelayCommand<object>(_ => {
                if (LoadedFileNames.Count == 0)
                {
                    DialogWindow.ShowError("Nie wybrano żadnych arkuszy!", "Błąd!");
                    return;
                }

                if (_selectedWriteFolder == null)
                {
                    string defaultSaveFolder = ConfigManager.GetSetting("DataSheetSaveFolderDirectory");
                    DialogResultEx? proceed = DialogWindow.Show($"Nie wybrano folderu docelowego. Plik zostanie zapisany w domyślnym folderze: {defaultSaveFolder}. Czy kontynuować?", "Zapis pliku", DialogType.YesNo, DialogIcon.Question);

                    if (proceed != DialogResultEx.Yes)
                    {
                        return;
                    }
                }

                string _outFolder = _selectedWriteFolder ?? ConfigManager.GetSetting("DataSheetSaveFolderDirectory");
                dataSheetModel.LoadedFileNames = LoadedFileNames;
                dataSheetModel.MergeDataSheets(_outFolder);

                DialogWindow.ShowInfo($"Arkusz danych został wygenerowany w:\n{_outFolder}", "Generowanie zakończone");
            });
        }

        private void ClearValue(DataSheetItemModel item)
        {
            if (item != null)
            {
                //Items![Items.IndexOf(item)].Value = string.Empty;
                item.Value = string.Empty;
            }
        }

        private void LoadFilesFromFolder()
        {
            LoadedFileNames!.Clear();
            _defaultKeyFilterMode = Int32.Parse(ConfigManager.GetSetting("DataSheetDefaultFileKeyFilter"));
            string[]? files = Directory.GetFiles(_selectedReadFolder!, "*.xml");
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
        }

    }
}
