using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class CommandBarKeyboardPage : UserControl
    {
        private readonly List<string> _log = new();

        public CommandBarKeyboardPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            DemoBar.Opened += OnOpened;
            DemoBar.Closed += OnClosed;

            BtnCopy.GotFocus += OnItemFocused;
            BtnPaste.GotFocus += OnItemFocused;
            BtnBold.GotFocus += OnItemFocused;
            BtnShare.GotFocus += OnItemFocused;
            BtnDelete.GotFocus += OnItemFocused;
            BtnExport.GotFocus += OnItemFocused;
        }

        private void OnUnloaded(object? sender, RoutedEventArgs e)
        {
            DemoBar.Opened -= OnOpened;
            DemoBar.Closed -= OnClosed;

            BtnCopy.GotFocus -= OnItemFocused;
            BtnPaste.GotFocus -= OnItemFocused;
            BtnBold.GotFocus -= OnItemFocused;
            BtnShare.GotFocus -= OnItemFocused;
            BtnDelete.GotFocus -= OnItemFocused;
            BtnExport.GotFocus -= OnItemFocused;
        }

        private void OnOpened(object? sender, RoutedEventArgs e)
            => AppendLog("Opened. Use arrow keys to navigate.");

        private void OnClosed(object? sender, RoutedEventArgs e)
            => AppendLog("Closed");

        private void OnItemFocused(object? sender, FocusChangedEventArgs e)
        {
            var label = sender switch
            {
                AppBarButton btn => btn.Label ?? "(unnamed)",
                AppBarToggleButton t => t.Label ?? "(unnamed)",
                _ => sender?.GetType().Name ?? "?"
            };

            var method = e.NavigationMethod switch
            {
                NavigationMethod.Directional => "arrow key",
                NavigationMethod.Tab => "Tab",
                NavigationMethod.Pointer => "pointer",
                _ => "unspecified"
            };

            AppendLog($"Focus: {label} ({method})");
        }

        private void OnOpenOverflow(object? sender, RoutedEventArgs e)
        {
            DemoBar.IsOpen = true;
        }

        private void OnClearLog(object? sender, RoutedEventArgs e)
        {
            _log.Clear();
            FocusLogText.Text = "Log cleared.";
        }

        private void AppendLog(string message)
        {
            _log.Add(message);

            if (_log.Count > 10)
                _log.RemoveAt(0);

            FocusLogText.Text = string.Join("\n", _log.Select((entry, i) => $"{i + 1,2}. {entry}"));
        }
    }
}
