using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DocumentsGenerator.Controls
{
    public class CommitEditableComboBox : ComboBox
    {
        public bool AddCustomItemToItemsSource { get; set; } = true;
        public bool CommitOnLostFocus { get; set; } = true;

        private readonly MouseButtonEventHandler _itemMouseDownHandler;

        public CommitEditableComboBox()
        {
            IsEditable = true;
            IsTextSearchEnabled = false; // allow free typing without built-in search stepping in
            _itemMouseDownHandler = OnItemPreviewMouseDown;
        }

        protected override void OnDropDownOpened(EventArgs e)
        {
            base.OnDropDownOpened(e);
            // Capture clicks anywhere inside ComboBoxItem (even if already handled)
            AddHandler(ComboBoxItem.PreviewMouseLeftButtonDownEvent, _itemMouseDownHandler, handledEventsToo: true);
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            // Stop listening once closed
            RemoveHandler(ComboBoxItem.PreviewMouseLeftButtonDownEvent, _itemMouseDownHandler);
            base.OnDropDownClosed(e);

            // If user didn’t pick anything and just clicked away, commit the typed text
            if (CommitOnLostFocus)
            {
                CommitTypedTextIfNeeded();
            }
        }

        private void OnItemPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Find the ComboBoxItem under the click
            var src = e.OriginalSource as DependencyObject;
            var container = ItemsControl.ContainerFromElement(this, src) as ComboBoxItem;
            if (container == null) return;

            // Select the item immediately and close the dropdown
            var item = ItemContainerGenerator.ItemFromContainer(container);
            SelectedItem = item;
            IsDropDownOpen = false;
            e.Handled = true; // prevent the default focus churn that breaks selection in editable ComboBox
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Enter)
            {
                if (CommitTypedTextIfNeeded())
                    e.Handled = true;
            }
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnLostKeyboardFocus(e);
            // Don’t commit while popup is open — it interferes with mouse selection
            if (!IsDropDownOpen && CommitOnLostFocus)
            {
                CommitTypedTextIfNeeded();
            }
        }

        private bool CommitTypedTextIfNeeded()
        {
            var text = Text?.Trim();
            if (string.IsNullOrEmpty(text))
                return false;

            // If current selection already matches the visible text, nothing to do
            if (SelectedItem != null &&
                string.Equals(GetItemDisplayText(SelectedItem), text, StringComparison.CurrentCulture))
                return false;

            // Try to match an existing item by display text
            var existing = Items.Cast<object>()
                .FirstOrDefault(item => string.Equals(GetItemDisplayText(item), text, StringComparison.CurrentCulture));

            if (existing != null)
            {
                SelectedItem = existing;
                return true;
            }

            // Not in list — add if allowed
            if (AddCustomItemToItemsSource)
            {
                if (ItemsSource is IList list)
                    list.Add(text);
                else
                    Items.Add(text);
            }

            SelectedItem = text; // still select the free-typed value
            return true;
        }

        private string GetItemDisplayText(object item)
        {
            if (item == null) return string.Empty;

            if (!string.IsNullOrWhiteSpace(DisplayMemberPath))
            {
                var prop = item.GetType().GetProperty(DisplayMemberPath);
                if (prop != null)
                    return prop.GetValue(item)?.ToString() ?? string.Empty;
            }

            return item.ToString() ?? string.Empty;
        }
    }
}
