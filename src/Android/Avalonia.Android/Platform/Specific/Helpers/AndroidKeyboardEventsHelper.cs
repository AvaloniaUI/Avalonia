using System;
using System.ComponentModel;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.Input;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;

namespace Avalonia.Android.Platform.Specific.Helpers
{
    public class AndroidKeyboardEventsHelper<TView> : IDisposable where TView :ITopLevelImpl, IAndroidView
    {
        private TView _view;
        private IInputElement _lastFocusedElement;

        public bool HandleEvents { get; set; }

        public AndroidKeyboardEventsHelper(TView view)
        {
            this._view = view;
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

        private bool? DispatchKeyEventInternal(KeyEvent e, out bool callBase)
        {
            if (e.Action == KeyEventActions.Multiple)
            {
                callBase = true;
                return null;
            }

            var rawKeyEvent = new RawKeyEventArgs(
                          AndroidKeyboardDevice.Instance,
                          Convert.ToUInt32(e.EventTime),
                          e.Action == KeyEventActions.Down ? RawKeyEventType.KeyDown : RawKeyEventType.KeyUp,
            AndroidKeyboardDevice.ConvertKey(e.KeyCode), GetModifierKeys(e));
            _view.Input(rawKeyEvent);

            if (e.Action == KeyEventActions.Down && e.UnicodeChar >= 32)
            {
                var rawTextEvent = new RawTextInputEventArgs(
                  AndroidKeyboardDevice.Instance,
                  Convert.ToUInt32(e.EventTime),
                  Convert.ToChar(e.UnicodeChar).ToString()
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

        private bool NeedsKeyboard(IInputElement element)
        {
            //may be some other elements
            return element is TextBox;
        }

        private void TryShowHideKeyboard(IInputElement element, bool value)
        {
            var input = _view.View.Context.GetSystemService(Context.InputMethodService).JavaCast<InputMethodManager>();

            if (value)
            {
                //show keyboard
                //may be in the future different keyboards support e.g. normal, only digits etc.
                //Android.Text.InputTypes
                input.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.ImplicitOnly);
            }
            else
            {
                //hide keyboard
                input.HideSoftInputFromWindow(_view.View.WindowToken, HideSoftInputFlags.None);
            }
        }

        public void UpdateKeyboardState(IInputElement element)
        {
            var focusedElement = element;
            bool oldValue = NeedsKeyboard(_lastFocusedElement);
            bool newValue = NeedsKeyboard(focusedElement);

            if (newValue != oldValue || newValue)
            {
                TryShowHideKeyboard(focusedElement, newValue);
            }

            _lastFocusedElement = element;
        }

        public void ActivateAutoShowKeyboard()
        {
            var kbDevice = (KeyboardDevice.Instance as INotifyPropertyChanged);

            //just in case we've called more than once the method
            kbDevice.PropertyChanged -= KeyboardDevice_PropertyChanged;
            kbDevice.PropertyChanged += KeyboardDevice_PropertyChanged;
        }

        private void KeyboardDevice_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyboardDevice.FocusedElement))
            {
                UpdateKeyboardState(KeyboardDevice.Instance.FocusedElement);
            }
        }

        public void Dispose()
        {
            HandleEvents = false;
        }
    }
}
