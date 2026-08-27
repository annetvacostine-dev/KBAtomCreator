using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace KBAtomCreator
{
    public class AutoCompleteTextBox : TextBox
    {
        private Popup _popup;
        private ListBox _listBox;
        private List<string> _autoCompleteList;

        public static readonly DependencyProperty AutoCompleteItemsProperty =
            DependencyProperty.Register("AutoCompleteItems", typeof(List<string>), typeof(AutoCompleteTextBox),
                new PropertyMetadata(null, OnAutoCompleteItemsChanged));

        public List<string> AutoCompleteItems
        {
            get { return (List<string>)GetValue(AutoCompleteItemsProperty); }
            set { SetValue(AutoCompleteItemsProperty, value); }
        }

        private static void OnAutoCompleteItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var textBox = (AutoCompleteTextBox)d;
            textBox.InitializeAutoComplete();
        }

        public AutoCompleteTextBox()
        {
            Loaded += (s, e) => InitializeAutoComplete();
        }

        private void InitializeAutoComplete()
        {
            _autoCompleteList = AutoCompleteItems ?? new List<string>();

            _popup = new Popup
            {
                Placement = PlacementMode.Bottom,
                PlacementTarget = this,
                StaysOpen = false,
                Width = this.Width,
                MaxHeight = 150
            };

            _listBox = new ListBox
            {
                Background = SystemColors.WindowBrush,
                BorderThickness = new Thickness(1),
                BorderBrush = SystemColors.ActiveBorderBrush
            };

            _listBox.MouseDoubleClick += (s, e) => InsertSelectedSuggestion();
            _listBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    InsertSelectedSuggestion();
            };

            _popup.Child = _listBox;

            TextChanged += OnTextChanged;
            PreviewKeyDown += OnPreviewKeyDown;
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(Text) || _popup == null)
            {
                _popup.IsOpen = false;
                return;
            }

            var lastOpenBracket = Text.LastIndexOf('[');
            if (lastOpenBracket >= 0)
            {
                var currentText = Text.Substring(lastOpenBracket).ToLower();
                var suggestions = _autoCompleteList
                    .Where(item => item.ToLower().StartsWith(currentText))
                    .ToList();

                if (suggestions.Any())
                {
                    _listBox.ItemsSource = suggestions;
                    _popup.IsOpen = true;
                    return;
                }
            }

            _popup.IsOpen = false;
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_popup.IsOpen) return;

            switch (e.Key)
            {
                case Key.Down:
                    if (_listBox.Items.Count > 0)
                    {
                        _listBox.Focus();
                        _listBox.SelectedIndex = 0;
                        e.Handled = true;
                    }
                    break;
                case Key.Enter:
                case Key.Tab:
                    if (_listBox.SelectedItem != null)
                    {
                        InsertSelectedSuggestion();
                        e.Handled = true;
                    }
                    break;
                case Key.Escape:
                    _popup.IsOpen = false;
                    e.Handled = true;
                    break;
            }
        }

        private void InsertSelectedSuggestion()
        {
            if (_listBox.SelectedItem == null) return;

            var suggestion = _listBox.SelectedItem.ToString();
            var lastOpenBracket = Text.LastIndexOf('[');

            if (lastOpenBracket >= 0)
            {
                var before = Text.Substring(0, lastOpenBracket);
                var after = Text.Substring(SelectionStart + SelectionLength);

                Text = before + suggestion + after;
                CaretIndex = before.Length + suggestion.Length;
            }

            _popup.IsOpen = false;
            Focus();
        }
    }
    public static class TextBoxAutoCompleteBehavior
    {
        public static readonly DependencyProperty AutoCompleteItemsProperty =
            DependencyProperty.RegisterAttached(
                "AutoCompleteItems",
                typeof(List<string>),
                typeof(TextBoxAutoCompleteBehavior),
                new PropertyMetadata(null, OnAutoCompleteItemsChanged));

        public static List<string> GetAutoCompleteItems(TextBox textBox)
        {
            return (List<string>)textBox.GetValue(AutoCompleteItemsProperty);
        }

        public static void SetAutoCompleteItems(TextBox textBox, List<string> value)
        {
            textBox.SetValue(AutoCompleteItemsProperty, value);
        }

        private static void OnAutoCompleteItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.TextChanged -= TextBox_TextChanged;
                textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;

                if (e.NewValue != null)
                {
                    textBox.TextChanged += TextBox_TextChanged;
                    textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                }
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;
            var autoCompleteItems = GetAutoCompleteItems(textBox);

            if (string.IsNullOrEmpty(textBox.Text) || autoCompleteItems == null)
            {
                ClosePopup(textBox);
                return;
            }

            var lastOpenBracket = textBox.Text.LastIndexOf('[');
            if (lastOpenBracket >= 0)
            {
                var currentText = textBox.Text.Substring(lastOpenBracket).ToLower();
                var suggestions = autoCompleteItems
                    .Where(item => item.ToLower().StartsWith(currentText))
                    .ToList();

                if (suggestions.Any())
                {
                    ShowPopup(textBox, suggestions);
                    return;
                }
            }

            ClosePopup(textBox);
        }

        private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var textBox = (TextBox)sender;
            var popup = GetPopup(textBox);

            if (popup == null || !popup.IsOpen) return;

            var listBox = popup.Child as ListBox;
            if (listBox == null) return;

            switch (e.Key)
            {
                case Key.Down:
                    if (listBox.Items.Count > 0)
                    {
                        listBox.Focus();
                        listBox.SelectedIndex = 0;
                        e.Handled = true;
                    }
                    break;
                case Key.Enter:
                case Key.Tab:
                    if (listBox.SelectedItem != null)
                    {
                        InsertSelectedSuggestion(textBox, listBox.SelectedItem.ToString());
                        e.Handled = true;
                    }
                    break;
                case Key.Escape:
                    popup.IsOpen = false;
                    e.Handled = true;
                    break;
            }
        }

        private static void ShowPopup(TextBox textBox, List<string> suggestions)
        {
            var popup = GetPopup(textBox);
            if (popup == null)
            {
                popup = new Popup
                {
                    Placement = PlacementMode.Bottom,
                    PlacementTarget = textBox,
                    StaysOpen = false,
                    MinWidth = 200,
                    MaxHeight = 200
                };

                var listBox = new ListBox
                {
                    Background = SystemColors.WindowBrush,
                    BorderThickness = new Thickness(1),
                    BorderBrush = SystemColors.ActiveBorderBrush
                };

                listBox.MouseDoubleClick += (s, e) =>
                {
                    if (listBox.SelectedItem != null)
                    {
                        InsertSelectedSuggestion(textBox, listBox.SelectedItem.ToString());
                    }
                };

                popup.Child = listBox;
                SetPopup(textBox, popup);
            }

            var listBoxControl = popup.Child as ListBox;
            if (listBoxControl != null)
            {
                listBoxControl.ItemsSource = suggestions;
                popup.IsOpen = true;
            }
        }

        private static void ClosePopup(TextBox textBox)
        {
            var popup = GetPopup(textBox);
            if (popup != null)
            {
                popup.IsOpen = false;
            }
        }

        private static void InsertSelectedSuggestion(TextBox textBox, string suggestion)
        {
            var lastOpenBracket = textBox.Text.LastIndexOf('[');
            if (lastOpenBracket >= 0)
            {
                var before = textBox.Text.Substring(0, lastOpenBracket);
                var selectionEnd = textBox.SelectionStart + textBox.SelectionLength;
                var after = selectionEnd < textBox.Text.Length ? textBox.Text.Substring(selectionEnd) : "";

                textBox.Text = before + suggestion + after;
                textBox.CaretIndex = before.Length + suggestion.Length;
            }

            ClosePopup(textBox);
            textBox.Focus();
        }

        private static readonly DependencyProperty PopupProperty =
            DependencyProperty.RegisterAttached("Popup", typeof(Popup), typeof(TextBoxAutoCompleteBehavior), new PropertyMetadata(null));

        private static Popup GetPopup(TextBox textBox)
        {
            return (Popup)textBox.GetValue(PopupProperty);
        }

        private static void SetPopup(TextBox textBox, Popup value)
        {
            textBox.SetValue(PopupProperty, value);
        }
    }

}
