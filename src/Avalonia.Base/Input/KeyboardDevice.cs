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
                foreach (var visual in v.VisualChildren)
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
                foreach (var visual in v.VisualChildren)
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
                    // Clear keyboard focus from currently focused element
                    if (FocusedElement != null &&
                        (!((Visual)FocusedElement).IsAttachedToVisualTree ||
                         _focusedRoot != ((Visual?)element)?.VisualRoot as IInputRoot) &&
                        _focusedRoot != null)
                    {
                        ClearChildrenFocusWithin(_focusedRoot, true);
                    }

                    SetIsFocusWithin(FocusedElement, element);
                    _focusedElement = element;
                    _focusedRoot = ((Visual?)_focusedElement)?.VisualRoot as IInputRoot;

                    interactive?.RaiseEvent(new RoutedEventArgs(InputElement.LostFocusEvent));

                    (element as Interactive)?.RaiseEvent(new GotFocusEventArgs
                    {
                        NavigationMethod = method,
                        KeyModifiers = keyModifiers,
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

            var element = FocusedElement ?? e.Root;

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
                        
                        var currentHandler = element as Visual;
                        while (currentHandler != null && !ev.Handled && keyInput.Type == RawKeyEventType.KeyDown)
                        {
                            var bindings = (currentHandler as IInputElement)?.KeyBindings;
                            if (bindings != null)
                            {
                                KeyBinding[]? bindingsCopy = null;

                                // Create a copy of the KeyBindings list if there's a binding which matches the event.
                                // If we don't do this the foreach loop will throw an InvalidOperationException when the KeyBindings list is changed.
                                // This can happen when a new view is loaded which adds its own KeyBindings to the handler.
                                foreach (var binding in bindings)
                                {
                                    if (binding.Gesture?.Matches(ev) == true)
                                    {
                                        bindingsCopy = bindings.ToArray();
                                        break;
                                    }
                                }

                                if (bindingsCopy is object)
                                {
                                    foreach (var binding in bindingsCopy)
                                    {
                                        if (ev.Handled)
                                            break;
                                        binding.TryHandle(ev);
                                    }
                                }
                            }
                            currentHandler = currentHandler.VisualParent;
                        }

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
