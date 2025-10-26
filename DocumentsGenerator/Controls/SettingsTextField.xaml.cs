using DocumentsGenerator.Config;
using DocumentsGenerator.Core;
using DocumentsGenerator.MVVM.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Logika interakcji dla klasy SettingsTextField.xaml
    /// </summary>
    public partial class SettingsTextField : UserControl
    {
        public SettingsTextField()
        {
            InitializeComponent();
            ClearButtonCommand = new RelayCommand<object>(_ => Value = "" );
            SelectButtonCommand = new RelayCommand<object>(_ => {
                OpenFolderDialog ofd = new OpenFolderDialog();
                ofd.Title = "Wybierz folder...";

                bool? success = ofd.ShowDialog();
                if (success == true)
                {
                   Value = ofd.FolderName;
                }
            });
        }

        // Setting display name
        public static readonly DependencyProperty DisplayKeyProperty =
           DependencyProperty.Register(
               nameof(DisplayKey), 
               typeof(string), 
               typeof(SettingsTextField));

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
                typeof(SettingsTextField));

        public string BackgroundColor
        {
            get => (string) GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value); 
        }

        // TextBox value
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value), 
                typeof(string), 
                typeof(SettingsTextField), 
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public string Value
        {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty LoadValueBtnVisibilityProperty =
            DependencyProperty.Register(
                nameof(LoadValueBtnVisibility), 
                typeof(string), 
                typeof(SettingsTextField),
                new PropertyMetadata("Visible"));

        public string LoadValueBtnVisibility
        {
            get => (string)GetValue(LoadValueBtnVisibilityProperty);
            set { 
                SetValue(LoadValueBtnVisibilityProperty, value);
            }
        }

        // Select directory button command
        public static readonly DependencyProperty SelectButtonCommandProperty =
            DependencyProperty.Register(
                nameof(SelectButtonCommand), 
                typeof(ICommand), 
                typeof(SettingsTextField));

        public ICommand SelectButtonCommand
        {
            get => (ICommand)GetValue(SelectButtonCommandProperty);
            set => SetValue(SelectButtonCommandProperty, value);
        }

        // Clear TextBox value button command
        public static readonly DependencyProperty ClearButtonCommandProperty =
            DependencyProperty.Register(
                nameof(ClearButtonCommand), 
                typeof(ICommand), 
                typeof(SettingsTextField));

        public ICommand ClearButtonCommand
        {
            get => (ICommand)GetValue(ClearButtonCommandProperty);
            set => SetValue(ClearButtonCommandProperty, value);
        }

    }

}
