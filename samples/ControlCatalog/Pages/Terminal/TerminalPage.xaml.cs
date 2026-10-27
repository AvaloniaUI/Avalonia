using System;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using ControlCatalog.Pages.Terminal.Pty;

namespace ControlCatalog.Pages.Terminal
{
    public partial class TerminalPage : ContentPage
    {
        private string _description = "";

        public TerminalPage()
        {
            InitializeComponent();
            Terminal.KeyEncoded += OnKeyEncoded;
            Terminal.TextSent += (text, bytes) => Inspector.Text = $"TextInput \"{Escape(text)}\" → {Hex(bytes)}";
            Terminal.Exited += code => Status.Text = $"{_description}: exited, code {code?.ToString() ?? "unknown"}";
            OptionAsMeta.IsVisible = OperatingSystem.IsMacOS();
            OptionAsMeta.IsCheckedChanged += (_, _) =>
            {
                Terminal.OptionAsMeta = OptionAsMeta.IsChecked == true;
                Terminal.Focus();
            };
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (!PtyFactory.IsSupported)
            {
                Status.Text = "Pseudo terminals are not available on this platform.";
                return;
            }

            try
            {
                var pty = PtyFactory.Create(Terminal.Columns, Terminal.Rows);
                _description = pty.Description;
                Status.Text = _description;
                Terminal.Attach(pty);
                Terminal.Focus();
            }
            catch (Exception ex)
            {
                Status.Text = "Failed to start the shell: " + ex.Message;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            Terminal.Detach();
        }

        private void OnKeyEncoded(KeyEventArgs e, byte[]? bytes)
        {
            var sent = bytes is null ? "(left to TextInput)" : Hex(bytes);
            Inspector.Text =
                $"Key={e.Key} PhysicalKey={e.PhysicalKey} KeyModifiers={e.KeyModifiers} KeySymbol=\"{Escape(e.KeySymbol)}\"\n" +
                $"KeyText=\"{Escape(e.KeyText)}\" KeyTextIgnoringModifiers=\"{Escape(e.KeyTextIgnoringModifiers)}\" " +
                $"ConsumedModifiers={e.ConsumedModifiers?.ToString() ?? "null"}\n" +
                $"{FormatNative(e.NativeKeyInfo)}\n" +
                $"Sent: {sent}";
        }

        private static string FormatNative(NativeKeyInfo? info)
        {
            if (info is null)
                return "NativeKeyInfo=null";
            static string Hex(ulong? value) => value is { } v ? $"0x{v:X}" : "null";
            return $"Native({info.Format}): ScanCode={Hex(info.ScanCode)} VirtualKey={Hex(info.VirtualKey)} " +
                   $"Modifiers={Hex(info.Modifiers)}";
        }

        private static string Escape(string? text)
        {
            if (text is null)
                return "null";
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

        private static string Hex(byte[] bytes) =>
            bytes.Length == 0 ? "(nothing)" : BitConverter.ToString(bytes).Replace('-', ' ').ToLowerInvariant();
    }
}
