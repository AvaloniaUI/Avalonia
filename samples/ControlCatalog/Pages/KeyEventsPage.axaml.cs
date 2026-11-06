using System;
using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class KeyEventsPage : ContentPage
    {
        private const int MaxEvents = 500;

        public KeyEventsPage()
        {
            InitializeComponent();
            DataContext = this;
            OptionAsMeta.IsVisible = OperatingSystem.IsMacOS();
            InputArea.KeyDown += OnKeyDown;
            InputArea.TextInput += OnTextInput;
            InputArea.PointerPressed += (_, _) => InputArea.Focus();
        }

        public ObservableCollection<KeyEventRow> Events { get; } = new();

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var bytes = TerminalKeyEncoder.Encode(e, OptionAsMeta.IsChecked == true);
            if (e.PlatformKeyInfo is { } info)
                PlatformFormat.Text = $"PlatformKeyInfo format: {info.Format}";

            // A terminal handles the keys that it encodes, so no TextInput follows them
            e.Handled = bytes is not null;

            Add(new KeyEventRow
            {
                Event = "KeyDown",
                Key = e.Key.ToString(),
                PhysicalKey = e.PhysicalKey.ToString(),
                KeyModifiers = e.KeyModifiers.ToString(),
                KeySymbol = Escape(e.KeySymbol),
                KeyText = Escape(e.KeyText),
                KeyTextIgnoringModifiers = Escape(e.KeyTextIgnoringModifiers),
                ConsumedModifiers = e.ConsumedModifiers?.ToString() ?? "",
                ScanCode = Hex(e.PlatformKeyInfo?.ScanCode),
                VirtualKey = Hex(e.PlatformKeyInfo?.VirtualKey),
                Terminal = bytes is null ? "" : ToCaretNotation(bytes)
            });
        }

        private void OnTextInput(object? sender, TextInputEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text))
                return;

            Add(new KeyEventRow
            {
                Event = "TextInput",
                KeyText = Escape(e.Text),
                Terminal = ToCaretNotation(TerminalKeyEncoder.EncodeText(e.Text))
            });
        }

        private void OnClear(object? sender, RoutedEventArgs e)
        {
            Events.Clear();
            InputArea.Focus();
        }

        private void Add(KeyEventRow row)
        {
            Events.Insert(0, row);
            if (Events.Count > MaxEvents)
                Events.RemoveAt(Events.Count - 1);
        }

        private static string Hex(uint? value) => value is { } v ? $"0x{v:X}" : "";

        /// <summary>
        /// Shows control characters as <c>\xNN</c>, so that they are visible in the table.
        /// </summary>
        private static string Escape(string? text)
        {
            if (text is null)
                return "";
            var sb = new StringBuilder();
            foreach (var c in text)
            {
                if (c < 0x20 || c == 0x7F)
                    sb.Append($"\\x{(int)c:x2}");
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Writes control characters in caret notation: <c>^C</c> for 0x03, <c>^[</c> for ESC and <c>^?</c> for DEL.
        /// </summary>
        private static string ToCaretNotation(byte[] bytes)
        {
            if (bytes.Length == 0)
                return "(nothing)";
            var sb = new StringBuilder();
            foreach (var c in Encoding.UTF8.GetString(bytes))
            {
                if (c < 0x20)
                    sb.Append('^').Append((char)(c + 0x40));
                else if (c == 0x7F)
                    sb.Append("^?");
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }

    public sealed class KeyEventRow
    {
        public string Event { get; init; } = "";
        public string Key { get; init; } = "";
        public string PhysicalKey { get; init; } = "";
        public string KeyModifiers { get; init; } = "";
        public string KeySymbol { get; init; } = "";
        public string KeyText { get; init; } = "";
        public string KeyTextIgnoringModifiers { get; init; } = "";
        public string ConsumedModifiers { get; init; } = "";
        public string ScanCode { get; init; } = "";
        public string VirtualKey { get; init; } = "";
        public string Terminal { get; init; } = "";
    }
}
