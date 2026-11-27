using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TextBoxSelectionPage : UserControl
    {
        public TextBoxSelectionPage()
        {
            InitializeComponent();

            SelectionBox.PropertyChanged += OnSelectionBoxPropertyChanged;
            CaretBlinkCombo.SelectionChanged += OnCaretBlinkChanged;

            UpdateSelectionStatus();
        }

        private void OnSelectionBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.SelectionStartProperty ||
                e.Property == TextBox.SelectionEndProperty ||
                e.Property == TextBox.TextProperty)
            {
                UpdateSelectionStatus();
            }
        }

        private void OnSelectAll(object? sender, RoutedEventArgs e)
        {
            SelectionBox.SelectAll();
            SelectionBox.Focus();
        }

        private void OnSelectFirstWord(object? sender, RoutedEventArgs e)
        {
            var text = SelectionBox.Text ?? string.Empty;
            var end = text.IndexOf(' ');

            if (end < 0)
            {
                end = text.Length;
            }

            SelectionBox.SelectionStart = 0;
            SelectionBox.SelectionEnd = end;
            SelectionBox.Focus();
        }

        private void OnClearSelection(object? sender, RoutedEventArgs e)
        {
            SelectionBox.ClearSelection();
        }

        private void OnCaretToStart(object? sender, RoutedEventArgs e)
        {
            CaretBox.CaretIndex = 0;
            CaretBox.Focus();
        }

        private void OnCaretToEnd(object? sender, RoutedEventArgs e)
        {
            CaretBox.CaretIndex = CaretBox.Text?.Length ?? 0;
            CaretBox.Focus();
        }

        private void OnCaretBlinkChanged(object? sender, SelectionChangedEventArgs e)
        {
            CaretBox.CaretBlinkInterval = CaretBlinkCombo.SelectedIndex switch
            {
                1 => TimeSpan.FromMilliseconds(150),
                2 => TimeSpan.FromSeconds(1),
                3 => TimeSpan.Zero,
                _ => TimeSpan.FromMilliseconds(500)
            };
        }

        private void UpdateSelectionStatus()
        {
            var selected = SelectionBox.SelectedText;
            var shown = string.IsNullOrEmpty(selected) ? "(nothing selected)" : selected;

            SelectionStatus.Text =
                $"SelectionStart: {SelectionBox.SelectionStart}     SelectionEnd: {SelectionBox.SelectionEnd}     SelectedText: {shown}";
        }
    }
}
