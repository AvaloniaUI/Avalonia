using System;
using System.ComponentModel;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Avalonia.Android.Platform.Input;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.Android.Platform.Specific.Helpers
{
    internal class AndroidKeyboardEventsHelper<TView> : IDisposable where TView : TopLevelImpl, IAndroidView, ITopLevelImplWithTextInputMethod
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

        private bool NeedsKeyboard(IInputElement element)
        {
            //may be some other elements
            return element is ISoftInputElement;
        }

        private void TryShowHideKeyboard(ISoftInputElement element, bool value)
        {
            _view.InitEditorInfo((outAttrs) =>
            {
                outAttrs.InputType = element.InputType switch
                {
                    InputType.Numeric => global::Android.Text.InputTypes.ClassNumber,
                    InputType.Phone => global::Android.Text.InputTypes.ClassPhone,
                    _ => global::Android.Text.InputTypes.Null
                };
            });

            var input = _view.View.Context.GetSystemService(Context.InputMethodService).JavaCast<InputMethodManager>();

            if (value && element != null && element.InputType != InputType.None)
            {
                _view.View.RequestFocus();

                if (!ReferenceEquals(_lastFocusedElement, element))
                {
                    input.RestartInput(_view.View);
                }

                input.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.NotAlways);
            }
            else
            {
                input.HideSoftInputFromWindow(_view.View.WindowToken, HideSoftInputFlags.None);
            }
        }

        public void UpdateKeyboardState(IInputElement element)
        {
            var focusedElement = element as ISoftInputElement;
            var lastElement = _lastFocusedElement as ISoftInputElement;
            
            bool oldValue = lastElement?.InputType > InputType.None;
            bool newValue = focusedElement?.InputType > InputType.None;

            if (newValue != oldValue || newValue)
            {
                if (_lastFocusedElement != null)
                    _lastFocusedElement.PointerReleased -= RestoreSoftKeyboard;

                TryShowHideKeyboard(focusedElement, newValue);

                if (newValue && focusedElement != null)
                    element.PointerReleased += RestoreSoftKeyboard;
            }

            _lastFocusedElement = element;
        }

        private void RestoreSoftKeyboard(object sender, PointerReleasedEventArgs e)
        {
            if (_lastFocusedElement is ISoftInputElement softInputElement && softInputElement.InputType != InputType.None)
                TryShowHideKeyboard(softInputElement, true);
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
