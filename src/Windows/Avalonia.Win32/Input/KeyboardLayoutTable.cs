using System;
using System.Collections.Concurrent;
using System.Threading;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using static Avalonia.Win32.Interop.UnmanagedMethods.VirtualKeyStates;

namespace Avalonia.Win32.Input
{
    /// <summary>
    /// The text that a keyboard layout produces for each virtual key with each combination of Shift, Ctrl, Alt and
    /// Caps Lock, read once per layout with ToUnicodeEx. Lookups don't touch the keyboard state.
    /// </summary>
    internal sealed class KeyboardLayoutTable
    {
        private const int Shift = 1, Control = 2, Alt = 4, CapsLock = 8, StateCount = 16;

        private static readonly ConcurrentDictionary<IntPtr, Lazy<KeyboardLayoutTable?>> s_tables = new();

        private readonly string?[] _entries = new string?[256 * StateCount];

        private KeyboardLayoutTable(IntPtr layout) => Build(layout);

        /// <summary>
        /// Starts building the tables of the user's layouts on a background thread.
        /// </summary>
        public static unsafe void Prepare()
        {
            var count = GetKeyboardLayoutList(0, null);
            if (count <= 0)
                return;
            var layouts = new IntPtr[count];
            fixed (IntPtr* p = layouts)
                count = GetKeyboardLayoutList(count, p);

            new Thread(() =>
            {
                for (var i = 0; i < count; i++)
                    _ = Get(layouts[i]);
            })
            {
                IsBackground = true,
                Name = "Keyboard layout tables"
            }.Start();
        }

        /// <summary>
        /// Gets the table of a layout, or null if it could not be built.
        /// </summary>
        public static KeyboardLayoutTable? Get(IntPtr layout)
            => s_tables.GetOrAdd(layout, l => new Lazy<KeyboardLayoutTable?>(() => Create(l),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;

        public static KeyboardLayoutTable? Current => Get(GetKeyboardLayout(0));

        private static KeyboardLayoutTable? Create(IntPtr layout)
        {
            try
            {
                return new KeyboardLayoutTable(layout);
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Win32Platform)
                    ?.Log(null, "Unable to read keyboard layout {Layout}: {Exception}", layout, e);
                return null;
            }
        }

        /// <summary>
        /// Gets the text of a key with Shift and Caps Lock applied, ignoring Ctrl and Alt. A dead key gives its dead
        /// character.
        /// </summary>
        public string? GetTextIgnoringControlAndAlt(int virtualKey, bool shift, bool capsLock)
            => GetEntry(virtualKey, (shift ? Shift : 0) | (capsLock ? CapsLock : 0));

        /// <summary>
        /// Gets the pressed modifiers that select the level of the key: a modifier is consumed if releasing it
        /// changes the character of the key. Control characters don't count as a level.
        /// </summary>
        public RawInputModifiers GetConsumedModifiers(int virtualKey, RawInputModifiers pressed, bool capsLock)
        {
            var state = (pressed.HasFlag(RawInputModifiers.Shift) ? Shift : 0)
                        | (pressed.HasFlag(RawInputModifiers.Control) ? Control : 0)
                        | (pressed.HasFlag(RawInputModifiers.Alt) ? Alt : 0)
                        | (capsLock ? CapsLock : 0);
            var level = GetLevel(virtualKey, state);
            var consumed = RawInputModifiers.None;

            if ((state & Shift) != 0 && GetLevel(virtualKey, state & ~Shift) != level)
                consumed |= RawInputModifiers.Shift;
            if ((state & Control) != 0 && GetLevel(virtualKey, state & ~Control) != level)
                consumed |= RawInputModifiers.Control;
            if ((state & Alt) != 0 && GetLevel(virtualKey, state & ~Alt) != level)
                consumed |= RawInputModifiers.Alt;
            return consumed;
        }

        private string? GetLevel(int virtualKey, int state)
        {
            var text = GetEntry(virtualKey, state);
            return text is not null && IsPrintable(text) ?
                text :
                GetEntry(virtualKey, state & ~(Control | Alt));
        }

        private string? GetEntry(int virtualKey, int state)
            => virtualKey is > 0 and < 256 ? _entries[virtualKey * StateCount + state] : null;

        private static bool IsPrintable(string text)
        {
            foreach (var c in text)
                if (c < 0x20 || c == 0x7F)
                    return false;
            return true;
        }

        private unsafe void Build(IntPtr layout)
        {
            const uint doNotChangeKeyboardState = 1U << 2;
            const int bufferSize = 8;

            // These versions ignore the flag above, so every call can change the dead-key state
            var changesState = Win32Platform.WindowsVersion < PlatformConstants.Windows10_1607;

            var keyStates = stackalloc byte[256];
            var buffer = stackalloc char[bufferSize];

            if (changesState)
            {
                // A pending dead key would combine with the first entries
                new Span<byte>(keyStates, 256).Clear();
                ToUnicodeEx((uint)VK_SPACE, MapVirtualKeyEx((uint)VK_SPACE, 0, layout), keyStates, buffer,
                    bufferSize, 0, layout);
            }

            for (var vk = 1; vk < 256; vk++)
            {
                var scanCode = MapVirtualKeyEx((uint)vk, 0, layout);
                if (scanCode == 0)
                    continue;

                for (var state = 0; state < StateCount; state++)
                {
                    new Span<byte>(keyStates, 256).Clear();
                    if ((state & Shift) != 0)
                        keyStates[(int)VK_SHIFT] = keyStates[(int)VK_LSHIFT] = 0x80;
                    if ((state & Control) != 0)
                        keyStates[(int)VK_CONTROL] = keyStates[(int)VK_LCONTROL] = 0x80;
                    if ((state & Alt) != 0)
                        keyStates[(int)VK_MENU] = keyStates[(int)VK_LMENU] = 0x80;
                    if ((state & CapsLock) != 0)
                        keyStates[(int)VK_CAPITAL] = 0x01;
                    keyStates[vk] = 0x80;

                    var length = ToUnicodeEx((uint)vk, scanCode, keyStates, buffer, bufferSize,
                        doNotChangeKeyboardState, layout);
                    if (length != 0)
                        _entries[vk * StateCount + state] = new string(buffer, 0, Math.Min(Math.Abs(length), bufferSize));

                    // A second press of a dead key clears the dead state
                    if (length < 0 && changesState)
                        ToUnicodeEx((uint)vk, scanCode, keyStates, buffer, bufferSize, 0, layout);
                }
            }
        }
    }
}
