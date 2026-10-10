using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Input
{
    [PrivateApi]
    public class KeyboardDevice : IKeyboardDevice, INotifyPropertyChanged
    {
        private IInputElement? _focusedElement;
        private IInputRoot? _focusedRoot;

        public event PropertyChangedEventHandler? PropertyChanged;

        internal static KeyboardDevice? Instance => AvaloniaLocator.Current.GetService<IKeyboardDevice>() as KeyboardDevice;

        public IInputManager? InputManager => AvaloniaLocator.Current.GetService<IInputManager>();

        public IFocusManager? FocusManager => AvaloniaLocator.Current.GetService<IFocusManager>();
        
        // This should live in the FocusManager, but with the current outdated architecture
        // the source of truth about the input focus is in KeyboardDevice
        private readonly TextInputMethodManager _textInputManager = new TextInputMethodManager();

        public IInputElement? FocusedElement => _focusedElement;

        private static void ClearFocusWithinAncestors(IInputElement? element)
        {
            var el = element;
            
            while (el != null)
            {
                if (el is InputElement ie)
                {
                    ie.IsKeyboardFocusWithin = false;
                }

                el = (IInputElement?)(el as Visual)?.VisualParent;
            }
        }
        
        private void ClearFocusWithin(IInputElement element, bool clearRoot)
        {
            if (element is Visual v)
            {
                foreach (var visual in v.TypedVisualChildren)
                {
                    if (visual is IInputElement el && el.IsKeyboardFocusWithin)
                    {
                        ClearFocusWithin(el, true);
                        break;
                    }
                }
            }
            
            if (clearRoot)
            {
                if (element is InputElement ie)
                {
                    ie.IsKeyboardFocusWithin = false;
                }
            }
        }

        private void SetIsFocusWithin(IInputElement? oldElement, IInputElement? newElement)
        {
            if (newElement == null && oldElement != null)
            {
                ClearFocusWithinAncestors(oldElement);
                return;
            }
            
            IInputElement? branch = null;

            var el = newElement;

            while (el != null)
            {
                if (el.IsKeyboardFocusWithin)
                {
                    branch = el;
                    break;
                }

                el = (el as Visual)?.VisualParent as IInputElement;
            }

            el = oldElement;

            if (el != null && branch != null)
            {
                ClearFocusWithin(branch, false);
            }

            el = newElement;
            
            while (el != null && el != branch)
            {
                if (el is InputElement ie)
                {
                    ie.IsKeyboardFocusWithin = true;
                }

                el = (el as Visual)?.VisualParent as IInputElement;
            }
        }
        
        private void ClearChildrenFocusWithin(IInputElement element, bool clearRoot)
        {
            if (element is Visual v)
            {
                foreach (var visual in v.TypedVisualChildren)
                {
                    if (visual is IInputElement el && el.IsKeyboardFocusWithin)
                    {
                        ClearChildrenFocusWithin(el, true);
                        break;
                    }
                }
            }
            
            if (clearRoot && element is InputElement ie)
            {
                ie.IsKeyboardFocusWithin = false;
            }
        }

        public void SetFocusedElement(
            IInputElement? element,
            NavigationMethod method,
            KeyModifiers keyModifiers)
        {
            SetFocusedElement(element, method, keyModifiers, true);
        }


        public void SetFocusedElement(
            IInputElement? element,
            NavigationMethod method,
            KeyModifiers keyModifiers,
            bool isFocusChangeCancellable)
        {
            if (element != FocusedElement)
            {
                var interactive = FocusedElement as Interactive;

                bool changeFocus = true;

                var losingFocus = new FocusChangingEventArgs(InputElement.LosingFocusEvent)
                {
                    OldFocusedElement = FocusedElement,
                    NewFocusedElement = element,
                    NavigationMethod = method,
                    KeyModifiers = keyModifiers,
                    CanCancelOrRedirectFocus = isFocusChangeCancellable
                };

                interactive?.RaiseEvent(losingFocus);

                if (losingFocus.Canceled)
                {
                    changeFocus = false;
                }

                if (changeFocus && losingFocus.NewFocusedElement is Interactive newFocus)
                {
                    var gettingFocus = new FocusChangingEventArgs(InputElement.GettingFocusEvent)
                    {
                        OldFocusedElement = FocusedElement,
                        NewFocusedElement = losingFocus.NewFocusedElement,
                        NavigationMethod = method,
                        KeyModifiers = keyModifiers,
                        CanCancelOrRedirectFocus = isFocusChangeCancellable
                    };

                    newFocus.RaiseEvent(gettingFocus);

                    if (gettingFocus.Canceled)
                    {
                        changeFocus = false;
                    }

                    element = gettingFocus.NewFocusedElement;
                }

                if (changeFocus)
                {
                    var oldElement = FocusedElement;

                    // Clear keyboard focus from currently focused element
                    if (oldElement != null &&
                        (!((Visual)oldElement).IsAttachedToVisualTree ||
                         _focusedRoot != ((Visual?)element)?.GetInputRoot()) &&
                        _focusedRoot != null)
                    {
                        ClearChildrenFocusWithin(_focusedRoot.RootElement, true);
                    }

                    SetIsFocusWithin(oldElement, element);
                    _focusedElement = element;
                    _focusedRoot = (_focusedElement as Visual)?.GetInputRoot();

                    interactive?.RaiseEvent(new FocusChangedEventArgs(InputElement.LostFocusEvent)
                    {
                        OldFocusedElement = oldElement,
                        NewFocusedElement = element,
                        NavigationMethod = method,
                        KeyModifiers = keyModifiers
                    });

                    (element as Interactive)?.RaiseEvent(new FocusChangedEventArgs(InputElement.GotFocusEvent)
                    {
                        OldFocusedElement = oldElement,
                        NewFocusedElement = element,
                        NavigationMethod = method,
                        KeyModifiers = keyModifiers
                    });

                    _textInputManager.SetFocusedElement(element);
                    RaisePropertyChanged(nameof(FocusedElement));
                }
            }
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if(e.Handled)
                return;

            var element = FocusedElement ?? e.Root.FocusRoot;

            if (e is RawKeyEventArgs keyInput)
            {
                switch (keyInput.Type)
                {
                    case RawKeyEventType.KeyDown:
                    case RawKeyEventType.KeyUp:
                        var routedEvent = keyInput.Type == RawKeyEventType.KeyDown
                            ? InputElement.KeyDownEvent
                            : InputElement.KeyUpEvent;

                        var ev = new KeyEventArgs
                        {
                            RoutedEvent = routedEvent,
                            Key = keyInput.Key,
                            KeyModifiers = keyInput.Modifiers.ToKeyModifiers(),
                            PhysicalKey = keyInput.PhysicalKey,
                            KeySymbol = keyInput.KeySymbol,
                            KeyDeviceType = keyInput.KeyDeviceType,
                            Source = element
                        };
                        
                        element.RaiseEvent(ev);
                        e.Handled = ev.Handled;
                        break;
                }
            }

            if (e is RawTextInputEventArgs text)
            {
                var ev = new TextInputEventArgs()
                {
                    Text = text.Text,
                    Source = element,
                    RoutedEvent = InputElement.TextInputEvent
                };

                element.RaiseEvent(ev);
                e.Handled = ev.Handled;
            }
        }
    }
}
