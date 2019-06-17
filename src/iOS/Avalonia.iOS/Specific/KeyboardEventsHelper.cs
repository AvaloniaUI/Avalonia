using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using ObjCRuntime;
using UIKit;

namespace Avalonia.iOS.Specific
{
    /// <summary>
    /// In order to have properly handle of keyboard event in iOS View should already made some things in the View:
    /// 1. Adopt the UIKeyInput protocol - add [Adopts("UIKeyInput")] to your view class
    /// 2. Implement all the methods required by UIKeyInput:
    ///     2.1  Implement HasText
    ///             example:
    ///                 [Export("hasText")]
    ///                 bool HasText => _keyboardHelper.HasText()
    ///     2.2   Implement InsertText
    ///            example:
    ///                [Export("insertText:")]
    ///                void InsertText(string text) => _keyboardHelper.InsertText(text);
    ///     2.3   Implement InsertText
    ///            example:
    ///               [Export("deleteBackward")]
    ///               void DeleteBackward() => _keyboardHelper.DeleteBackward();
    /// 3.Let iOS know that this can become a first responder:
    ///            public override bool CanBecomeFirstResponder => _keyboardHelper.CanBecomeFirstResponder();
    ///            or
    ///            public override bool CanBecomeFirstResponder { get { return true; } }
    ///
    /// 4. To show keyboard:
    ///             view.BecomeFirstResponder();
    /// 5. To hide keyboard
    ///             view.ResignFirstResponder();
    /// </summary>
    /// <typeparam name="TView">View that needs keyboard events and show/hide keyboard</typeparam>
    internal class KeyboardEventsHelper<TView> where TView : UIView, ITopLevelImpl
    {
        private TView _view;
        private IInputElement _lastFocusedElement;
        private IKeyboardDevice _keyboard;

        public KeyboardEventsHelper(TView view, IKeyboardDevice keyboard)
        {
            _view = view;
            _keyboard = keyboard;

            var uiKeyInputAttribute = view.GetType().GetCustomAttributes(typeof(AdoptsAttribute), true).OfType<AdoptsAttribute>().Where(a => a.ProtocolType == "UIKeyInput").FirstOrDefault();

            if (uiKeyInputAttribute == null) throw new NotSupportedException($"View class {typeof(TView).Name} should have class attribute - [Adopts(\"UIKeyInput\")] in order to access keyboard events!");

            HandleEvents = true;
        }

        /// <summary>
        /// HandleEvents in order to suspend keyboard notifications or resume it
        /// </summary>
        public bool HandleEvents { get; set; }

        public bool HasText() => false;

        public bool CanBecomeFirstResponder() => true;

        public void DeleteBackward()
        {
            HandleKey(Key.Back, RawKeyEventType.KeyDown);
            HandleKey(Key.Back, RawKeyEventType.KeyUp);
        }

        public void InsertText(string text)
        {
            var rawTextEvent = new RawTextInputEventArgs(_keyboard, (uint)DateTime.Now.Ticks, text);
            _view.Input(rawTextEvent);
        }

        private void HandleKey(Key key, RawKeyEventType type)
        {
            var rawKeyEvent = new RawKeyEventArgs(_keyboard, (uint)DateTime.Now.Ticks, type, key, InputModifiers.None);
            _view.Input(rawKeyEvent);
        }

        //currently not found a way to get InputModifiers state
        //private static InputModifiers GetModifierKeys(object e)
        //{
        //    var im = InputModifiers.None;
        //    //if (IsCtrlPressed) rv |= InputModifiers.Control;
        //    //if (IsShiftPressed) rv |= InputModifiers.Shift;

        //    return im;
        //}

        private bool NeedsKeyboard(IInputElement element)
        {
            //may be some other elements
            return element is TextBox;
        }

        private void TryShowHideKeyboard(IInputElement element, bool value)
        {
            if (value)
            {
                _view.BecomeFirstResponder();
            }
            else
            {
                _view.ResignFirstResponder();
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

        private IFocusManager _focusManager;

        public IFocusManager FocusManager
        {
            get => _focusManager;
            set
            {
                if (_focusManager != null)
                    _focusManager.FocusedElementChanged -= OnFocusedElementChanged;
                _focusManager = value;
                if (_focusManager != null)
                    _focusManager.FocusedElementChanged += OnFocusedElementChanged;
                UpdateKeyboardState(_focusManager?.FocusedElement);
            }
        }

        private void OnFocusedElementChanged(object sender, EventArgs e)
        {
            UpdateKeyboardState(_focusManager?.FocusedElement);
        }

    }
}
