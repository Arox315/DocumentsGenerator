using DocumentsGenerator.Core;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.ObjectModel;
using DocumentsGenerator.MVVM.Model;

namespace DocumentsGenerator.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public RelayCommand<object> HomeViewCommand { get; set; }
        public RelayCommand<object> TemplateViewCommand { get; set; }
        public RelayCommand<object> SheetViewCommand { get; set; }
        public RelayCommand<object> DocumentViewCommand { get; set; }
        public RelayCommand<object> SettingsViewCommand { get; set; }

        public HomeViewModel HomeVm { get; set; }
        public TemplateViewModel TemplateVm { get; set; }
        public SheetViewModel SheetVm { get; set; }
        public DocumentViewModel DocumentVm { get; set; }
        public SettingsViewModel SettingsVm { get; set; }

        public SolidColorBrush NavbarColor { get; set; }
        public SolidColorBrush MainMenuColor { get; set; }

        private object? _currentView;

        public object CurrentView
        {
            get { return _currentView!; }
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }

        }

        private readonly Constants constants = new();

        public MainViewModel()
        {
            HomeVm = new HomeViewModel();
            TemplateVm = new TemplateViewModel();
            SheetVm = new SheetViewModel();
            DocumentVm = new DocumentViewModel();
            SettingsVm = new SettingsViewModel();

            CurrentView = HomeVm;

            NavbarColor = new SolidColorBrush(constants._mainColor);
            MainMenuColor = new SolidColorBrush(constants._mainColorDark);

            HomeViewCommand = new RelayCommand<object>(_ => {
                CurrentView = HomeVm;
                NavbarColor.Color = constants._mainColor;
                MainMenuColor.Color = constants._mainColorDark;
            });

            TemplateViewCommand = new RelayCommand<object>(_ => {
                CurrentView = TemplateVm;
                NavbarColor.Color = constants._templateColor;
                MainMenuColor.Color = constants._templateColorDark;
            });

            SheetViewCommand = new RelayCommand<object>(_ =>
            {
                CurrentView = SheetVm;
                NavbarColor.Color = constants._sheetColor;
                MainMenuColor.Color = constants._sheetColorDark;
            });

            DocumentViewCommand = new RelayCommand<object>(_ => {
                CurrentView = DocumentVm;
                NavbarColor.Color = constants._documentColor;
                MainMenuColor.Color = constants._documentColorDark;
            });

            SettingsViewCommand = new RelayCommand<object>(_ => {
                CurrentView = SettingsVm;
                NavbarColor.Color = constants._settingsColor;
                MainMenuColor.Color = constants._settingsColorDark;
            });
        }
    }
}
