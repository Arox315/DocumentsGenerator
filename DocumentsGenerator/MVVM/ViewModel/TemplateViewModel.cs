using DocumentsGenerator.Core;
using DocumentsGenerator.MVVM.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public ObservableCollection<string>? FileKeyFilters { get; set; }
        public string SelectedKeyFilter { get; set; }
        public TemplateViewModel() 
        {
            LoadedFilesHeader = new SolidColorBrush(constants._mainColorDark);
            LoadedFilesItems = new SolidColorBrush(constants._mainColor);

            MainColor = new SolidColorBrush(constants._templateColor);
            MainColorDark = new SolidColorBrush(constants._templateColorDark);

            LoadedFileNames = new ObservableCollection<LoadedFileNameModel>();
            LoadedFileNames.Add(new LoadedFileNameModel { FileName = "test01.docx" });
            LoadedFileNames.Add(new LoadedFileNameModel { FileName = "test02.docx" });

            FileKeyFilters = new ObservableCollection<string>();
            FileKeyFilters.Add("Zawiera");
            FileKeyFilters.Add("Zaczyna się");
            FileKeyFilters.Add("Kończy się");

            SelectedKeyFilter = FileKeyFilters.First();

        }
    }
}
