using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DocumentsGenerator.Controls
{
    class IconRadioButton : RadioButton
    {
        // Icon
        public static readonly DependencyProperty IconImageProperty =
           DependencyProperty.Register(
               nameof(IconImage),
               typeof(ImageSource),
               typeof(IconRadioButton),
               new PropertyMetadata(null));

        public ImageSource IconImage
        {
            get => (ImageSource)GetValue(IconImageProperty);
            set => SetValue(IconImageProperty, value);
        }

        // Selected Color
        public static readonly DependencyProperty SelectedColorProperty = 
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(Brush),
                typeof(IconRadioButton),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(34, 32, 47))));
        
        public Brush SelectedColor
        {
            get => (Brush)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        static IconRadioButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(IconRadioButton),
                new FrameworkPropertyMetadata(typeof(IconRadioButton)));
        }

    }
}
