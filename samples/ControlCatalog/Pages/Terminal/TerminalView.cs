using System;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Threading;
using ControlCatalog.Pages.Terminal.Pty;

namespace ControlCatalog.Pages.Terminal
{
    /// <summary>
    /// Shows a <see cref="TerminalBuffer"/> and sends key presses to a <see cref="IPty"/>.
    /// </summary>
    public sealed class TerminalView : Control
    {
        private static readonly Typeface s_typeface =
            new("Cascadia Mono, Consolas, Menlo, DejaVu Sans Mono, Liberation Mono, monospace");
        private const double FontSize = 14;

        private readonly TerminalBuffer _buffer = new(80, 24);
        private readonly AnsiMiniParser _parser;
        private readonly TerminalInputMethodClient _inputMethodClient;
        private readonly Size _cellSize;
        private IPty? _pty;
        private string? _preeditText;

        public TerminalView()
        {
            Focusable = true;
            ClipToBounds = true;
            _parser = new AnsiMiniParser(_buffer);
            _inputMethodClient = new TerminalInputMethodClient(this);
            var probe = new FormattedText("MMMMMMMMMM", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                s_typeface, FontSize, null);
            _cellSize = new Size(Math.Ceiling(probe.WidthIncludingTrailingWhitespace) / 10, Math.Ceiling(probe.Height));
            TextInputMethodClientRequested += (_, e) => e.Client = _inputMethodClient;
        }

        /// <summary>
        /// Raised for every key down, with the bytes that were sent for it (null when the key is left to TextInput).
        /// </summary>
        public event Action<KeyEventArgs, byte[]?>? KeyEncoded;

        /// <summary>
        /// Raised with the bytes that TextInput or paste sent.
        /// </summary>
        public event Action<string, byte[]>? TextSent;

        public event Action<int?>? Exited;

        /// <summary>
        /// On macOS, makes Option chords send ESC and the key's text, instead of the character that Option types.
        /// </summary>
        public bool OptionAsMeta { get; set; }

        public int Columns => _buffer.Columns;
        public int Rows => _buffer.Rows;

        public Rect CursorRectangle => new(
            _buffer.CursorX * _cellSize.Width, _buffer.CursorY * _cellSize.Height, _cellSize.Width, _cellSize.Height);

        internal void Attach(IPty pty)
        {
            _pty = pty;
            _pty.Resize(_buffer.Columns, _buffer.Rows);
            _pty.Start(
                data => Dispatcher.UIThread.Post(() =>
                {
                    _parser.Feed(data);
                    _inputMethodClient.UpdateCursorRectangle();
                    InvalidateVisual();
                }),
                code => Dispatcher.UIThread.Post(() =>
                {
                    _parser.Feed(Encoding.UTF8.GetBytes($"\r\n[process exited, code {code?.ToString() ?? "unknown"}]\r\n"));
                    InvalidateVisual();
                    Exited?.Invoke(code);
                }));
        }

        public void Detach()
        {
            _pty?.Dispose();
            _pty = null;
        }

        public void SetPreeditText(string? text)
        {
            _preeditText = text;
            InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();
            e.Handled = true;
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            var columns = Math.Max(2, (int)(e.NewSize.Width / _cellSize.Width));
            var rows = Math.Max(2, (int)(e.NewSize.Height / _cellSize.Height));
            if (columns == _buffer.Columns && rows == _buffer.Rows)
                return;
            _buffer.Resize(columns, rows);
            _pty?.Resize(columns, rows);
            InvalidateVisual();
        }

        protected override async void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled)
                return;

            if (IsClipboardGesture(e, Key.V))
            {
                e.Handled = true;
                if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard
                    && await clipboard.TryGetTextAsync() is { } pasted)
                    Send(KeyEncoder.EncodePaste(pasted), pasted);
                return;
            }

            if (IsClipboardGesture(e, Key.C))
            {
                // No selection: copy the whole screen
                e.Handled = true;
                KeyEncoded?.Invoke(e, Array.Empty<byte>());
                if (TopLevel.GetTopLevel(this)?.Clipboard is { } clipboard)
                    await clipboard.SetTextAsync(_buffer.GetText());
                return;
            }

            var bytes = KeyEncoder.Encode(e, OptionAsMeta);
            KeyEncoded?.Invoke(e, bytes);
            if (bytes is null)
                return;
            e.Handled = true;
            if (bytes.Length > 0)
                _pty?.Write(bytes);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            if (e.Handled || string.IsNullOrEmpty(e.Text))
                return;
            e.Handled = true;
            Send(KeyEncoder.EncodeText(e.Text), e.Text);
        }

        private void Send(byte[] bytes, string text)
        {
            TextSent?.Invoke(text, bytes);
            if (bytes.Length > 0)
                _pty?.Write(bytes);
        }

        /// <summary>
        /// Ctrl+Shift+C/V on Windows and Linux, where Ctrl+C/V belong to the program. Cmd+C/V on macOS.
        /// </summary>
        private static bool IsClipboardGesture(KeyEventArgs e, Key key) =>
            (e.Key == key || e.PhysicalKey == (key == Key.C ? PhysicalKey.C : PhysicalKey.V))
            && e.KeyModifiers == (OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control | KeyModifiers.Shift);

        public override void Render(DrawingContext context)
        {
            var background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
            var foreground = new SolidColorBrush(Color.FromRgb(0xD4, 0xD4, 0xD4));
            context.FillRectangle(background, new Rect(Bounds.Size));

            for (var y = 0; y < _buffer.Rows; y++)
            {
                var line = _buffer.GetLine(y).TrimEnd();
                if (line.Length == 0)
                    continue;
                var text = new FormattedText(line, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, s_typeface,
                    FontSize, foreground);
                context.DrawText(text, new Point(0, y * _cellSize.Height));
            }

            var cursor = CursorRectangle;
            if (!string.IsNullOrEmpty(_preeditText))
            {
                var preedit = new FormattedText(_preeditText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    s_typeface, FontSize, foreground);
                context.FillRectangle(background, new Rect(cursor.Position, new Size(preedit.Width, cursor.Height)));
                context.DrawText(preedit, cursor.Position);
                context.DrawLine(new Pen(foreground), cursor.BottomLeft, cursor.BottomLeft + new Vector(preedit.Width, 0));
            }
            else
            {
                var cursorBrush = new SolidColorBrush(Colors.LightGreen, IsFocused ? 0.7 : 0.3);
                context.FillRectangle(cursorBrush, cursor);
            }
        }

        protected override void OnGotFocus(FocusChangedEventArgs e)
        {
            base.OnGotFocus(e);
            InvalidateVisual();
        }

        protected override void OnLostFocus(FocusChangedEventArgs e)
        {
            base.OnLostFocus(e);
            InvalidateVisual();
        }
    }
}
