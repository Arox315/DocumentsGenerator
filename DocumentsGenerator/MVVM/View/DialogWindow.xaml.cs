using DocumentsGenerator.Core;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace DocumentsGenerator.MVVM.View
{
    public enum DialogType { Ok, YesNo, OkCancel, YesNoCancel }
    public enum DialogResultEx { None, Ok, Cancel, Yes, No }
    public enum DialogIcon { None, Info, Warning, Error, Question }

    public partial class DialogWindow : Window
    {
        public string Message { get; }
        public DialogType Type { get; }
        public DialogIcon DIcon { get; }
        public DialogResultEx Result { get; private set; } = DialogResultEx.None;

        Constants constants = new();
        public SolidColorBrush NavbarColor { get; set; }
        public SolidColorBrush ButtonColor { get; set; }

        public DialogWindow(string message, string title, DialogType type, DialogIcon icon)
        {
            InitializeComponent();

            NavbarColor = new SolidColorBrush(constants._mainColor);
            ButtonColor = new SolidColorBrush(constants._mainColorDark);

            Message = message;
            Title = title;
            Type = type;
            DIcon = icon;

            DataContext = this;

            Loaded += (_, __) => ConfigureButtons();
            Loaded += (_, __) => ConfigureIcons();
        }

        private void ConfigureButtons()
        {
            // Show/hide and set defaults based on DialogType
            switch (Type)
            {
                case DialogType.Ok:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    break;

                case DialogType.YesNo:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    BtnYes.IsDefault = true;
                    break;

                case DialogType.OkCancel:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    BtnOk.IsDefault = true;
                    break;

                case DialogType.YesNoCancel:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    BtnYes.IsDefault = true;
                    break;
            }
        }

        private void ConfigureIcons()
        {
            switch (DIcon)
            {
                case DialogIcon.Info:
                    IconInfo.Visibility = Visibility.Visible;
                    break;
                case DialogIcon.Error: 
                    IconError.Visibility = Visibility.Visible; 
                    break;
                case DialogIcon.Warning:
                    IconWarning.Visibility = Visibility.Visible;
                    break;
                case DialogIcon.Question:
                    IconQuestion.Visibility = Visibility.Visible;
                    break;
            }
        }

        // Button handlers
        private void BtnOk_Click(object sender, RoutedEventArgs e) { Result = DialogResultEx.Ok; Close(); }
        private void BtnCancel_Click(object sender, RoutedEventArgs e) { Result = DialogResultEx.Cancel; Close(); }
        private void BtnYes_Click(object sender, RoutedEventArgs e) { Result = DialogResultEx.Yes; Close(); }
        private void BtnNo_Click(object sender, RoutedEventArgs e) { Result = DialogResultEx.No; Close(); }

        // Static helpers (nice API)
        public static void ShowInfo(string message, string title = "Info", Window? owner = null)
            => ShowInternal(message, title, DialogType.Ok, DialogIcon.Info, owner!);

        public static void ShowError(string message, string title = "Error", Window? owner = null)
            => ShowInternal(message, title, DialogType.Ok, DialogIcon.Error, owner!);

        public static bool ConfirmYesNo(string message, string title = "Confirm", Window? owner = null)
            => ShowInternal(message, title, DialogType.YesNo, DialogIcon.Question, owner!) == DialogResultEx.Yes;

        public static DialogResultEx Show(
            string message,
            string title,
            DialogType type,
            DialogIcon icon = DialogIcon.None,
            Window? owner = null)
            => ShowInternal(message, title, type, icon, owner!);

        private static DialogResultEx ShowInternal(
            string message, string title, DialogType type, DialogIcon icon, Window owner)
        {
            var dlg = new DialogWindow(message, title, type, icon);
            if (owner != null) dlg.Owner = owner;
            dlg.ShowDialog();
            return dlg.Result;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (Type)
            {
                case DialogType.Ok:
                    Result = DialogResultEx.Ok;
                    break;

                case DialogType.YesNo:
                    Result = DialogResultEx.No;
                    break;

                case DialogType.OkCancel:
                    Result = DialogResultEx.Cancel;
                    break;

                case DialogType.YesNoCancel:
                    Result = DialogResultEx.Cancel;
                    break;
            }
            
            Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
    }
}

