#nullable enable

using System;
using Android.Views;
using Avalonia.Android.Platform.Input;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Android.Platform.Specific.Helpers
{
    internal class AndroidKeyboardEventsHelper<TView> : IDisposable where TView : TopLevelImpl, IAndroidView
    {
        private readonly TView _view;

        public bool HandleEvents { get; set; }

        public AndroidKeyboardEventsHelper(TView view)
        {
            _view = view;
            HandleEvents = true;
        }

        public bool? DispatchKeyEvent(KeyEvent e, out bool callBase)
        {
            if (!HandleEvents)
            {
                callBase = true;
                return null;
            }

            return DispatchKeyEventInternal(e, out callBase);
        }

        static string? UnicodeTextInput(KeyEvent keyEvent)
        {
            return keyEvent.Action == KeyEventActions.Multiple
                && keyEvent.RepeatCount == 0
                && !string.IsNullOrEmpty(keyEvent.Characters)
                ? keyEvent.Characters
                : null;
        }

        private bool? DispatchKeyEventInternal(KeyEvent e, out bool callBase)
        {
            var unicodeTextInput = UnicodeTextInput(e);

            if (e.Action == KeyEventActions.Multiple && unicodeTextInput == null)
            {
                callBase = true;
                return null;
            }

            var physicalKey = AndroidKeyInterop.PhysicalKeyFromScanCode(e.ScanCode);
            var keySymbol = GetKeySymbol(e.UnicodeChar, physicalKey);
            var keyDeviceType = GetKeyDeviceType(e);

            var rawKeyEvent = new RawKeyEventArgs(
                          AndroidKeyboardDevice.Instance!,
                          Convert.ToUInt64(e.EventTime),
                          _view.InputRoot,
                          e.Action == KeyEventActions.Down ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                          AndroidKeyboardDevice.ConvertKey(e.KeyCode),
                          GetModifierKeys(e),
                          physicalKey,
                          keyDeviceType,
                          keySymbol);

            _view.Input?.Invoke(rawKeyEvent);

            if ((e.Action == KeyEventActions.Down && e.UnicodeChar >= 32)
                || unicodeTextInput != null)
            {
                var rawTextEvent = new RawTextInputEventArgs(
                  AndroidKeyboardDevice.Instance!,
                  Convert.ToUInt64(e.EventTime),
                  _view.InputRoot,
                  unicodeTextInput ?? Convert.ToChar(e.UnicodeChar).ToString()
                  );
                _view.Input?.Invoke(rawTextEvent);
            }

            if (e.Action == KeyEventActions.Up)
            {
                //nothing to do here more call base no need of more events
                callBase = true;
                return null;
            }

            callBase = false;
            return false;
        }

        private static RawInputModifiers GetModifierKeys(KeyEvent e)
        {
            var rv = RawInputModifiers.None;

            if (e.IsCtrlPressed) rv |= RawInputModifiers.Control;
            if (e.IsShiftPressed) rv |= RawInputModifiers.Shift;

            return rv;
        }

        private static string? GetKeySymbol(int unicodeChar, PhysicalKey physicalKey)
        {
            // Handle a very limited set of control characters so that we're consistent with other platforms
            // (matches KeySymbolHelper.IsAllowedAsciiKeySymbol)
            switch (physicalKey)
            {
                case PhysicalKey.Backspace:
                    return "\b";
                case PhysicalKey.Tab:
                    return "\t";
                case PhysicalKey.Enter:
                case PhysicalKey.NumPadEnter:
                    return "\r";
                case PhysicalKey.Escape:
                    return "\u001B";
                default:
                    if (unicodeChar <= 0x7F)
                    {
                        var asciiChar = (char)unicodeChar;
                        return KeySymbolHelper.IsAllowedAsciiKeySymbol(asciiChar) ? asciiChar.ToString() : null;
                    }
                    return char.ConvertFromUtf32(unicodeChar);
            }
        }

        private KeyDeviceType GetKeyDeviceType(KeyEvent e)
        {
            var source = e.Device?.Sources ?? InputSourceType.Unknown;

            // Remote controller reports itself as "DPad | Keyboard", which is confusing,
            // so we need to double-check KeyboardType as well.

            if (source.HasAnyFlag(InputSourceType.Dpad)
                && e.Device?.KeyboardType == InputKeyboardType.NonAlphabetic)
                return KeyDeviceType.Remote;

            // ReSharper disable BitwiseOperatorOnEnumWithoutFlags - it IS flags enum under the hood.
            if (source.HasAnyFlag(InputSourceType.Joystick | InputSourceType.Gamepad))
                return KeyDeviceType.Gamepad;
            // ReSharper restore BitwiseOperatorOnEnumWithoutFlags

            return KeyDeviceType.Keyboard; // fallback to the keyboard, if unknown.
        }

        public void Dispose()
        {
            HandleEvents = false;
        }
    }
}
