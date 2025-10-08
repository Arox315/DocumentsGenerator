using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DocumentsGenerator.Controls
{
    class IconButton : Button
    {
        // Icon
        public static readonly DependencyProperty IconImageProperty =
           DependencyProperty.Register(
               nameof(IconImage),
               typeof(ImageSource),
               typeof(IconButton),
               new PropertyMetadata(null));

        public ImageSource IconImage
        {
            get => (ImageSource)GetValue(IconImageProperty);
            set => SetValue(IconImageProperty, value);
        }

        // Icon width
        public static readonly DependencyProperty IconWidthProperty = 
            DependencyProperty.Register(
                nameof(IconWidth),
                typeof(double),
                typeof(IconButton),
                new PropertyMetadata(null));

        public double IconWidth
        {
            get => (double)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }
       
        
        // Icon height
        public static readonly DependencyProperty IconHeightProperty =
            DependencyProperty.Register(
                nameof(IconHeight),
                typeof(double),
                typeof(IconButton),
                new PropertyMetadata(null));

        public double IconHeight
        {
            get => (double)GetValue(IconWidthProperty);
            set => SetValue(IconWidthProperty, value);
        }

        static IconButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(IconButton),
                new FrameworkPropertyMetadata(typeof(IconButton)));
        }
    }
}
