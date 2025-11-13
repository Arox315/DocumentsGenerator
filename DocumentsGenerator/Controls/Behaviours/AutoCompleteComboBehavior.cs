using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DocumentsGenerator.Controls.Behaviours
{
    public static class AutoCompleteComboBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled", 
                typeof(bool), 
                typeof(AutoCompleteComboBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);
        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

        public static readonly DependencyProperty MinCharsProperty = 
            DependencyProperty.RegisterAttached(
                "MinChars", 
                typeof(int), 
                typeof(AutoCompleteComboBehavior),
                new PropertyMetadata(1));
        public static void SetMinChars(DependencyObject obj, int value) => obj.SetValue(MinCharsProperty, value);
        public static int GetMinChars(DependencyObject obj) => (int)obj.GetValue(MinCharsProperty);

        public static readonly DependencyProperty MatchModeProperty =
            DependencyProperty.RegisterAttached(
                "MatchMode", 
                typeof(MatchMode), 
                typeof(AutoCompleteComboBehavior),
                new PropertyMetadata(MatchMode.StartsWith));
        public static void SetMatchMode(DependencyObject obj, MatchMode value) => obj.SetValue(MatchModeProperty, value);
        public static MatchMode GetMatchMode(DependencyObject obj) => (MatchMode)obj.GetValue(MatchModeProperty);

        public enum MatchMode { StartsWith, Contains }

        public static readonly DependencyProperty MaxSuggestionsProperty =
            DependencyProperty.RegisterAttached(
                "MaxSuggestions",
                typeof(int),
                typeof(AutoCompleteComboBehavior),
                new PropertyMetadata(35));

        public static void SetMaxSuggestions(DependencyObject obj, int value) => obj.SetValue(MaxSuggestionsProperty, value);
        public static int GetMaxSuggestions(DependencyObject obj) => (int)obj.GetValue(MaxSuggestionsProperty);


        private class State
        {
            public ComboBox Cb = null!;
            public TextBox? Tb;
            public IEnumerable? MasterSource;
            public ObservableCollection<string> Local = new();
            public ICollectionView? View;
            public INotifyCollectionChanged? Notifier;
            public bool IsCommittingSelection;
        }

        private static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached(
                "State", typeof(State), typeof(AutoCompleteComboBehavior),
                new PropertyMetadata(null));

        private static void SetState(DependencyObject d, State? s) => d.SetValue(StateProperty, s);
        private static State? GetState(DependencyObject d) => (State?)d.GetValue(StateProperty);

        private static void OnItemMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ComboBox cb) return;
            var st = GetState(cb);
            if (st?.Tb == null) return;

            var cbi = FindAncestor<ComboBoxItem>(e.OriginalSource as DependencyObject);
            if (cbi == null) return;

            var chosen = cbi.DataContext?.ToString() ?? string.Empty;

            st.IsCommittingSelection = true;

            cb.SelectedItem = cbi.DataContext;
            st.Tb.Text = chosen;
            st.Tb.CaretIndex = chosen.Length;
            st.Tb.SelectionLength = 0;

            cb.IsDropDownOpen = false;
            st.Tb.Focus();

            e.Handled = true;
        }

        private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
        {
            while (start != null && start is not T)
                start = System.Windows.Media.VisualTreeHelper.GetParent(start);
            return start as T;
        }

        private static void ShowAllOnOpen(State st, ComboBox cb)
        {
            if (st.View == null) return;

            int n = Math.Max(1, GetMaxSuggestions(cb));
            if (n <= 0)
            {
                st.View.Filter = null;
            }
            else
            {
                var show = st.Local.Take(n).ToHashSet(StringComparer.OrdinalIgnoreCase);
                st.View.Filter = o => o != null && show.Contains(o.ToString() ?? "");
            }

            st.View.Refresh();
            cb.IsDropDownOpen = st.View.Cast<object>().Any();
        }


        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ComboBox cb) return;

            if ((bool)e.NewValue)
            {
                var st = new State { Cb = cb };
                SetState(cb, st);

                cb.IsEditable = true;
                cb.IsTextSearchEnabled = false;
                cb.StaysOpenOnEdit = true;

                cb.Loaded += Cb_Loaded;
                cb.Unloaded += Cb_Unloaded;
                cb.DropDownOpened += Cb_DropDownOpened;
                cb.SelectionChanged += Cb_SelectionChanged;
                cb.TargetUpdated += Cb_TargetUpdated;
            }
            else
            {
                TearDown(cb);
            }
        }

        private static void Cb_TargetUpdated(object? sender, DataTransferEventArgs e)
        {
            if (sender is ComboBox cb && e.Property == ItemsControl.ItemsSourceProperty)
                Initialize(cb);
        }

        private static void Cb_Loaded(object sender, RoutedEventArgs e) => Initialize((ComboBox)sender);

        private static void Cb_Unloaded(object sender, RoutedEventArgs e) => TearDown((ComboBox)sender);

        private static void TearDown(ComboBox cb)
        {
            var st = GetState(cb);
            if (st?.Tb != null)
            {
                st.Tb.TextChanged -= Tb_TextChanged;
                st.Tb.PreviewKeyDown -= Tb_PreviewKeyDown;
            }
            cb.Loaded -= Cb_Loaded;
            cb.Unloaded -= Cb_Unloaded;
            cb.DropDownOpened -= Cb_DropDownOpened;
            cb.SelectionChanged -= Cb_SelectionChanged;
            cb.TargetUpdated -= Cb_TargetUpdated;

            cb.RemoveHandler(ComboBoxItem.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnItemMouseDown));

            if (st?.Notifier != null)
                st.Notifier.CollectionChanged -= Master_CollectionChanged;

            SetState(cb, null);
        }

        private static void Initialize(ComboBox cb)
        {
            var st = GetState(cb);
            if (st == null) return;

            if (st.MasterSource != cb.ItemsSource)
            {
                if (st.Notifier != null)
                    st.Notifier.CollectionChanged -= Master_CollectionChanged;

                st.MasterSource = cb.ItemsSource;

                RebuildLocalFromMaster(st);

                st.View = CollectionViewSource.GetDefaultView(st.Local);
                cb.ItemsSource = st.View;

                st.Notifier = st.MasterSource as INotifyCollectionChanged;
                if (st.Notifier != null)
                    st.Notifier.CollectionChanged += Master_CollectionChanged;
            }

            st.Tb = cb.Template.FindName("PART_EditableTextBox", cb) as TextBox;
            if (st.Tb != null)
            {
                st.Tb.TextChanged -= Tb_TextChanged;
                st.Tb.TextChanged += Tb_TextChanged;

                st.Tb.PreviewKeyDown -= Tb_PreviewKeyDown;
                st.Tb.PreviewKeyDown += Tb_PreviewKeyDown;
            }

            cb.AddHandler(ComboBoxItem.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(OnItemMouseDown), true);
        }

        private static void Master_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {}

        private static void RebuildLocalFromMaster(State st)
        {
            st.Local.Clear();
            if (st.MasterSource == null) return;

            foreach (var item in st.MasterSource)
            {
                var s = item?.ToString();
                if (!string.IsNullOrWhiteSpace(s))
                    st.Local.Add(s);
            }
        }

        private static void Tb_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            if (tb.TemplatedParent is not ComboBox cb) return;
            var st = GetState(cb);
            if (st?.View == null) return;

            int caret = tb.CaretIndex;

            if (!st.IsCommittingSelection)
            {
                cb.SelectedIndex = -1;
                cb.SelectedItem = null;
            }

            string text = tb.Text ?? string.Empty;
            int min = GetMinChars(cb);
            var mode = GetMatchMode(cb);

            if (text.Length < min)
            {
                ShowAllOnOpen(st, cb);
            }
            else
            {
                st.View.Filter = o =>
                {
                    var s = o?.ToString() ?? string.Empty;
                    return mode == MatchMode.StartsWith
                        ? s.StartsWith(text, StringComparison.OrdinalIgnoreCase)
                        : s.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
                };
                st.View.Refresh();

                bool hasMatches = st.View.Cast<object>().Any();
                cb.IsDropDownOpen = hasMatches;
            }

            if (caret > tb.Text!.Length) caret = tb.Text.Length;
            tb.SelectionStart = caret;
            tb.SelectionLength = 0;

            st.IsCommittingSelection = false;
        }

        private static void Tb_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var tb = (TextBox)sender;
            if (tb.TemplatedParent is not ComboBox cb) return;

            if (e.Key == Key.Down && cb.IsDropDownOpen)
            {
                cb.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (cb.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement fe)
                        fe.Focus();
                }));
                e.Handled = true;
            }
        }

        private static void Cb_DropDownOpened(object? sender, EventArgs e)
        {
            var cb = (ComboBox)sender!;
            var st = GetState(cb);
            st?.Tb?.Focus();

            if (st?.View == null) return;

            ShowAllOnOpen(st, cb);
        }

        private static void Cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var cb = (ComboBox)sender;
            var st = GetState(cb);
            if (st?.Tb == null) return;

            if (cb.IsDropDownOpen && cb.SelectedItem != null)
            {
                st.IsCommittingSelection = true;

                var chosen = cb.SelectedItem.ToString() ?? string.Empty;
                st.Tb.Text = chosen;
                st.Tb.CaretIndex = chosen.Length;
                st.Tb.SelectionLength = 0;

                cb.IsDropDownOpen = false;
                st.Tb.Focus();

                e.Handled = true;
            }
        }
    }
}
