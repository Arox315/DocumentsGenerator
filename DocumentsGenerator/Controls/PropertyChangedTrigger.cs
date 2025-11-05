using System.Windows;
using System.Windows.Data;
using Microsoft.Xaml.Behaviors;

namespace DocumentsGenerator.Controls
{
    public class PropertyChangedTrigger : TriggerBase<FrameworkElement>
    {
        public static readonly DependencyProperty ObservedValueProperty =
            DependencyProperty.Register(
                nameof(ObservedValue),
                typeof(object),
                typeof(PropertyChangedTrigger),
                new PropertyMetadata(null, OnObservedValueChanged));

        public object ObservedValue
        {
            get => GetValue(ObservedValueProperty);
            set => SetValue(ObservedValueProperty, value);
        }

        public BindingBase? Binding { get; set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (Binding != null)
            {
                // Bind this trigger’s ObservedValue to whatever the user supplied
                BindingOperations.SetBinding(this, ObservedValueProperty, Binding);
            }
        }

        protected override void OnDetaching()
        {
            BindingOperations.ClearBinding(this, ObservedValueProperty);
            base.OnDetaching();
        }

        private static void OnObservedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var trigger = (PropertyChangedTrigger)d;
            // Run all actions each time the value changes
            trigger.InvokeActions(e.NewValue);
        }
    }
}
