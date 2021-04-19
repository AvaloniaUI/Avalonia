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

        string? UnicodeTextInput(KeyEvent keyEvent)
        {
            return keyEvent.Action == KeyEventActions.Multiple
                && keyEvent.RepeatCount == 0
                && !string.IsNullOrEmpty(keyEvent?.Characters)
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

            var rawKeyEvent = new RawKeyEventArgs(
                          AndroidKeyboardDevice.Instance,
                          Convert.ToUInt64(e.EventTime),
                          _view.InputRoot,
                          e.Action == KeyEventActions.Down ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
                          AndroidKeyboardDevice.ConvertKey(e.KeyCode), GetModifierKeys(e));

            _view.Input(rawKeyEvent);

            if ((e.Action == KeyEventActions.Down && e.UnicodeChar >= 32)
                || unicodeTextInput != null)
            {
                var rawTextEvent = new RawTextInputEventArgs(
                  AndroidKeyboardDevice.Instance,
                  Convert.ToUInt32(e.EventTime),
                  _view.InputRoot,
                  unicodeTextInput ?? Convert.ToChar(e.UnicodeChar).ToString()
                  );
                _view.Input(rawTextEvent);
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

        public void Dispose()
        {
            HandleEvents = false;
        }
    }
}
