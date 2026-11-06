using System;
using System.Text;
using Avalonia.Input;

namespace ControlCatalog.Pages
{
    /// <summary>
    /// Turns key presses into the bytes that a terminal emulator sends to the program, the way xterm-like terminals
    /// do. It shows how <see cref="KeyEventArgs.KeyText"/>, <see cref="KeyEventArgs.KeyTextIgnoringModifiers"/> and
    /// <see cref="KeyEventArgs.ConsumedModifiers"/> are used.
    /// </summary>
    /// <remarks>
    /// Special keys use the common ANSI/xterm sequences.
    /// Keys that type text are left to the TextInput event, so that dead keys and input methods work.
    /// </remarks>
    internal static class TerminalKeyEncoder
    {
        private const string Esc = "\x1b";

        /// <summary>
        /// Encodes a key down.
        /// </summary>
        /// <returns>
        /// The bytes to send, or null if the key is left to the TextInput event.
        /// An empty array means that the key is consumed without output.
        /// </returns>
        /// <param name="e">The key event.</param>
        /// <param name="optionAsMeta">On macOS, send ESC and the key's text for Option chords, instead of the
        /// character that Option types.</param>
        public static byte[]? Encode(KeyEventArgs e, bool optionAsMeta)
        {
            var altIsMeta = !OperatingSystem.IsMacOS() || optionAsMeta;
            var mods = e.KeyModifiers;

            if (EncodeSpecialKey(e.Key, mods.HasFlag(KeyModifiers.Control), mods.HasFlag(KeyModifiers.Shift)) is { } special)
                return Bytes((mods.HasFlag(KeyModifiers.Alt) && altIsMeta ? Esc : "") + special);

            // Modifiers that the layout used to type the character, such as AltGr (Ctrl+Alt on Windows) or Option on
            // macOS, are not terminal modifiers. Option as Meta makes Option one, even when it changes the character.
            var consumed = e.ConsumedModifiers ?? KeyModifiers.None;
            if (OperatingSystem.IsMacOS() && optionAsMeta)
                consumed &= ~KeyModifiers.Alt;
            mods &= ~consumed;
            var ctrl = mods.HasFlag(KeyModifiers.Control);
            var alt = mods.HasFlag(KeyModifiers.Alt);

            if (ctrl)
            {
                var prefix = alt && altIsMeta ? Esc : "";
                return GetControlChar(e) is { } c ? Bytes(prefix + c) : Array.Empty<byte>();
            }

            // Don't type text for Super/Cmd chords
            if (mods.HasFlag(KeyModifiers.Meta))
                return Array.Empty<byte>();

            // On macOS Option types characters, unless it is used as Meta
            if (alt && altIsMeta)
                return IsPrintable(e.KeyTextIgnoringModifiers) ? Bytes(Esc + e.KeyTextIgnoringModifiers) : Array.Empty<byte>();

            return null;
        }

        /// <summary>
        /// Encodes text from the TextInput event. Control chars are dropped: keys that produce them are handled in
        /// <see cref="Encode"/>.
        /// </summary>
        public static byte[] EncodeText(string text)
        {
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (c >= 0x20 && c != 0x7F)
                    sb.Append(c);
            }
            return Bytes(sb.ToString());
        }

        private static string? EncodeSpecialKey(Key key, bool ctrl, bool shift) => key switch
        {
            Key.Up => Esc + "[A",
            Key.Down => Esc + "[B",
            Key.Right => Esc + "[C",
            Key.Left => Esc + "[D",
            Key.Home => Esc + "[H",
            Key.End => Esc + "[F",
            Key.Insert => Esc + "[2~",
            Key.Delete => Esc + "[3~",
            Key.PageUp => Esc + "[5~",
            Key.PageDown => Esc + "[6~",
            Key.F1 => Esc + "OP",
            Key.F2 => Esc + "OQ",
            Key.F3 => Esc + "OR",
            Key.F4 => Esc + "OS",
            Key.F5 => Esc + "[15~",
            Key.F6 => Esc + "[17~",
            Key.F7 => Esc + "[18~",
            Key.F8 => Esc + "[19~",
            Key.F9 => Esc + "[20~",
            Key.F10 => Esc + "[21~",
            Key.F11 => Esc + "[23~",
            Key.F12 => Esc + "[24~",
            Key.Enter => "\r",
            Key.Tab => shift ? Esc + "[Z" : "\t",
            Key.Escape => Esc,
            Key.Back => ctrl ? "\b" : "\x7f",
            _ => null
        };

        /// <summary>
        /// Gets the C0 control char for a Ctrl chord, from the most to the least reliable source.
        /// </summary>
        private static string? GetControlChar(KeyEventArgs e)
        {
            // 1. The char that the platform layout produced (the layout's Ctrl mapping)
            if (e.KeyText is { Length: 1 } text && IsControl(text[0]))
                return text;

            // 2. The caret-notation transform (XkbToControl) of the char without Ctrl
            if (e.KeyTextIgnoringModifiers is { Length: 1 } symbol && ToControl(symbol[0]) is { } c)
                return c.ToString();

            // 3. The Latin fallback of Key, for layouts without Latin letters
            if (e.Key is >= Key.A and <= Key.Z)
                return ((char)(e.Key - Key.A + 1)).ToString();

            return null;
        }

        /// <summary>
        /// The libX11/libxkbcommon <c>XkbToControl</c> transform.
        /// </summary>
        private static char? ToControl(char c)
        {
            if (c is >= '@' and < '\x7f' or ' ')
                return (char)(c & 0x1F);
            return c switch
            {
                '2' => '\0',
                >= '3' and <= '7' => (char)(c - '3' + 0x1B),
                '8' => '\x7f',
                '/' => '\x1f',
                _ => null
            };
        }

        private static bool IsControl(char c) => c < 0x20 || c == 0x7F;

        private static bool IsPrintable(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            foreach (var c in text)
            {
                if (IsControl(c))
                    return false;
            }
            return true;
        }

        private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);
    }
}
