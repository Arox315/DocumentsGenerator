using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DocumentsGenerator.Controls
{
    /// <summary>
    /// Logika interakcji dla klasy SettingsComboBoxField.xaml
    /// </summary>
    public partial class SettingsComboBoxField : UserControl
    {
        public ObservableCollection<string>? FileKeyFilters { get; } = new()
        {
            "Zawiera",
            "Zaczyna się",
            "Kończy się"
        };

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
                    _selectedKeyFilterMode = FileKeyFilters!.IndexOf(SelectedKeyFilter);
                }
            }
        }

        public SettingsComboBoxField()
        {
            InitializeComponent();
        }

        // Setting display name
        public static readonly DependencyProperty DisplayKeyProperty =
           DependencyProperty.Register(
               nameof(DisplayKey),
               typeof(string),
               typeof(SettingsComboBoxField));

        public string DisplayKey
        {
            get => (string)GetValue(DisplayKeyProperty);
            set => SetValue(DisplayKeyProperty, value);
        }

        // Main BG color
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register(
                nameof(BackgroundColor),
                typeof(string),
                typeof(SettingsComboBoxField));

        public string BackgroundColor
        {
            get => (string)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        // Current value
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(string),
                typeof(SettingsComboBoxField),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }
}
