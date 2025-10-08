using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DocumentsGenerator.Controls
{
    public class DoubleIconButton : Button
    {
        public static readonly DependencyProperty IconImage1Property =
            DependencyProperty.Register(nameof(IconImage1), typeof(ImageSource), typeof(DoubleIconButton), new PropertyMetadata(null));

        public static readonly DependencyProperty IconImage2Property =
            DependencyProperty.Register(nameof(IconImage2), typeof(ImageSource), typeof(DoubleIconButton), new PropertyMetadata(null));

        public ImageSource IconImage1
        {
            get => (ImageSource)GetValue(IconImage1Property);
            set => SetValue(IconImage1Property, value);
        }

        public ImageSource IconImage2
        {
            get => (ImageSource)GetValue(IconImage2Property);
            set => SetValue(IconImage2Property, value);
        }
    }
}
