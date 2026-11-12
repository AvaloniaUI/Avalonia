using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class TextBoxEditingPage : UserControl
    {
        private const int MaxLogLines = 5;

        private readonly List<string> _textLog = new();
        private readonly List<string> _clipboardLog = new();

        private readonly MenuItem _cutItem = new() { Header = "Cut", InputGesture = new KeyGesture(Key.X, KeyModifiers.Control) };
        private readonly MenuItem _copyItem = new() { Header = "Copy", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control) };
        private readonly MenuItem _pasteItem = new() { Header = "Paste", InputGesture = new KeyGesture(Key.V, KeyModifiers.Control) };
        private readonly MenuItem _selectAllItem = new() { Header = "Select all" };

        public TextBoxEditingPage()
        {
            InitializeComponent();

            TextEventsBox.TextChanging += OnTextChanging;
            TextEventsBox.TextChanged += OnTextChanged;

            ClipboardBox.CuttingToClipboard += OnCuttingToClipboard;
            ClipboardBox.CopyingToClipboard += OnCopyingToClipboard;
            ClipboardBox.PastingFromClipboard += OnPastingFromClipboard;

            BuildContextFlyout();

            UpdateTextLog();
            UpdateClipboardLog();
        }

        private void BuildContextFlyout()
        {
            _cutItem.Click += (_, _) => FlyoutBox.Cut();
            _copyItem.Click += (_, _) => FlyoutBox.Copy();
            _pasteItem.Click += (_, _) => FlyoutBox.Paste();
            _selectAllItem.Click += (_, _) => FlyoutBox.SelectAll();

            // CanCut, CanCopy and CanPaste raise change notifications, so the menu items can simply follow them.
            _cutItem.Bind(IsEnabledProperty, FlyoutBox.GetObservable(TextBox.CanCutProperty));
            _copyItem.Bind(IsEnabledProperty, FlyoutBox.GetObservable(TextBox.CanCopyProperty));
            _pasteItem.Bind(IsEnabledProperty, FlyoutBox.GetObservable(TextBox.CanPasteProperty));

            var flyout = new MenuFlyout();
            flyout.Items.Add(_cutItem);
            flyout.Items.Add(_copyItem);
            flyout.Items.Add(_pasteItem);
            flyout.Items.Add(new Separator());
            flyout.Items.Add(_selectAllItem);

            FlyoutBox.ContextFlyout = flyout;
        }

        private void OnTextChanging(object? sender, TextChangingEventArgs e)
        {
            Log(_textLog, $"TextChanging, Text is now \"{TextEventsBox.Text}\"");
            UpdateTextLog();
        }

        private void OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            Log(_textLog, $"TextChanged, {TextEventsBox.Text?.Length ?? 0} characters");
            UpdateTextLog();
        }

        private void OnCuttingToClipboard(object? sender, RoutedEventArgs e)
        {
            var cancelled = BlockCutCheck.IsChecked == true;
            e.Handled = cancelled;
            Log(_clipboardLog, cancelled ? "CuttingToClipboard, cancelled" : "CuttingToClipboard, allowed");
            UpdateClipboardLog();
        }

        private void OnCopyingToClipboard(object? sender, RoutedEventArgs e)
        {
            var cancelled = BlockCopyCheck.IsChecked == true;
            e.Handled = cancelled;
            Log(_clipboardLog, cancelled ? "CopyingToClipboard, cancelled" : "CopyingToClipboard, allowed");
            UpdateClipboardLog();
        }

        private void OnPastingFromClipboard(object? sender, RoutedEventArgs e)
        {
            var cancelled = BlockPasteCheck.IsChecked == true;
            e.Handled = cancelled;
            Log(_clipboardLog, cancelled ? "PastingFromClipboard, cancelled" : "PastingFromClipboard, allowed");
            UpdateClipboardLog();
        }

        private void OnUndo(object? sender, RoutedEventArgs e)
        {
            UndoBox.Undo();
            UndoBox.Focus();
        }

        private void OnRedo(object? sender, RoutedEventArgs e)
        {
            UndoBox.Redo();
            UndoBox.Focus();
        }

        private void OnClearTextLog(object? sender, RoutedEventArgs e)
        {
            _textLog.Clear();
            UpdateTextLog();
        }

        private void OnClearClipboardLog(object? sender, RoutedEventArgs e)
        {
            _clipboardLog.Clear();
            UpdateClipboardLog();
        }

        private static void Log(List<string> log, string entry)
        {
            log.Insert(0, entry);

            while (log.Count > MaxLogLines)
            {
                log.RemoveAt(log.Count - 1);
            }
        }

        private void UpdateTextLog()
        {
            TextEventLog.Text = _textLog.Count == 0
                ? "No text events yet."
                : string.Join("\n", _textLog);
        }

        private void UpdateClipboardLog()
        {
            ClipboardLog.Text = _clipboardLog.Count == 0
                ? "No clipboard events yet."
                : string.Join("\n", _clipboardLog);
        }
    }
}
