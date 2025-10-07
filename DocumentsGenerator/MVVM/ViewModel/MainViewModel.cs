using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentsGenerator.Core;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace DocumentsGenerator.MVVM.ViewModel
{
	class MainViewModel : ObservableObject
    {
        private Color _mainColor = Color.FromRgb(31, 29, 56);
        private Color _mainColorDark = Color.FromRgb(19, 18, 35);
        private Color _templateColor = Color.FromRgb(64, 167, 174);
        private Color _templateColorDark = Color.FromRgb(32, 84, 88);
        private Color _sheetColor = Color.FromRgb(136, 172, 76);
        private Color _sheetColorDark = Color.FromRgb(69, 87, 38);
        private Color _documentColor = Color.FromRgb(124, 67, 142);
        private Color _documentColorDark = Color.FromRgb(62, 34, 71);

        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand TemplateViewCommand {  get; set; }
        public RelayCommand SheetViewCommand { get; set; }
        public RelayCommand DocumentViewCommand { get; set; }


        public HomeViewModel HomeVm { get; set; }
        public TemplateViewModel TemplateVm { get; set; }
        public SheetViewModel SheetVm { get; set; }
        public DocumentViewModel DocumentVm { get; set; }

        public SolidColorBrush NavbarColor { get; set; }
        public SolidColorBrush MainMenuColor { get; set; }

        private object? _currentView;

        public object CurrentView
        {
            get { return _currentView!; }
            set {
                _currentView = value;
                OnPropertyChanged();
            }
           
        }
        public MainViewModel() 
        {
            HomeVm = new HomeViewModel();
            TemplateVm = new TemplateViewModel();
            SheetVm = new SheetViewModel();
            DocumentVm = new DocumentViewModel();

            NavbarColor = new SolidColorBrush(_mainColor);
            MainMenuColor = new SolidColorBrush(_mainColorDark);

            CurrentView = HomeVm;

            HomeViewCommand = new RelayCommand(_ => {
                CurrentView = HomeVm;
                NavbarColor.Color = _mainColor;
                MainMenuColor.Color = _mainColorDark;
            });

            TemplateViewCommand = new RelayCommand(_ => { 
                CurrentView = TemplateVm;
                NavbarColor.Color = _templateColor;
                MainMenuColor.Color = _templateColorDark;
            });

            SheetViewCommand = new RelayCommand(_ =>
            {
                CurrentView = SheetVm;
                NavbarColor.Color = _sheetColor;
                MainMenuColor.Color = _sheetColorDark;
            });

            DocumentViewCommand = new RelayCommand(_ => {
                CurrentView = DocumentVm;
                NavbarColor.Color = _documentColor;
                MainMenuColor.Color = _documentColorDark;
            });

                
        }
    }
}
