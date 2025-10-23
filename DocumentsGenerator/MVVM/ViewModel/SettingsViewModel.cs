using DocumentsGenerator.Core;
using System;
using System.Collections.Generic;
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
 
        public SettingsViewModel() 
        {
            MainColor = new SolidColorBrush(constants._settingsColor);
            MainColorDark = new SolidColorBrush(constants._settingsColorDark);
        } 
    }
}
