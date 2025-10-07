using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace DocumentsGenerator.Controls
{
    class NavbarButton : Button
    {
        // Icon
        public static readonly DependencyProperty IconImageProperty =
           DependencyProperty.Register(
               nameof(IconImage),
               typeof(ImageSource),
               typeof(NavbarButton),
               new PropertyMetadata(null));

        public ImageSource IconImage
        {
            get => (ImageSource)GetValue(IconImageProperty);
            set => SetValue(IconImageProperty, value);
        }

        static NavbarButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(NavbarButton),
                new FrameworkPropertyMetadata(typeof(NavbarButton)));
        }
    }
}
