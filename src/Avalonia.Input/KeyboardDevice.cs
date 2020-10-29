using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class KeyboardDevice : IKeyboardDevice, INotifyPropertyChanged
    {
        private IInputElement? _focusedElement;

        public event PropertyChangedEventHandler? PropertyChanged;

        public static IKeyboardDevice Instance => AvaloniaLocator.Current.GetService<IKeyboardDevice>();

        public IInputManager InputManager => AvaloniaLocator.Current.GetService<IInputManager>();

        public IFocusManager FocusManager => AvaloniaLocator.Current.GetService<IFocusManager>();

        public IInputElement? FocusedElement
        {
            get
            {
                return _focusedElement;
            }

            private set
            {
                _focusedElement = value;
                RaisePropertyChanged();
            }
        }
        
        private void ClearFocusWithin(IInputElement element, bool clearRoot)
        {
            foreach (IInputElement el in element.VisualChildren)
            {
                if (el.IsKeyboardFocusWithin)
                {
                    ClearFocusWithin(el, true);
                    break;
                }
            }
            
            if(clearRoot)
            {
                if (element is InputElement ie)
                {
                    ie.IsKeyboardFocusWithin = false;
                }
            }
        }

        private void SetIsFocusWithin(IInputElement oldElement, IInputElement newElement)
        {
            IInputElement? branch = null;

            IInputElement el = newElement;

            while (el != null)
            {
                if (el.IsKeyboardFocusWithin)
                {
                    branch = el;
                    break;
                }

                el = (IInputElement)el.VisualParent;
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

                el = (IInputElement)el.VisualParent;
            }    
        }

        public void SetFocusedElement(
            IInputElement? element, 
            NavigationMethod method,
            KeyModifiers keyModifiers)
        {
            if (element != FocusedElement)
            {
                var interactive = FocusedElement as IInteractive;

                SetIsFocusWithin(FocusedElement, element);
                
                FocusedElement = element;

                interactive?.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = InputElement.LostFocusEvent,
                });

                interactive = element as IInteractive;

                interactive?.RaiseEvent(new GotFocusEventArgs
                {
                    RoutedEvent = InputElement.GotFocusEvent,
                    NavigationMethod = method,
                    KeyModifiers = keyModifiers,
                });
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

                        KeyEventArgs ev = new KeyEventArgs
                        {
                            RoutedEvent = routedEvent,
                            Device = this,
                            Key = keyInput.Key,
                            KeyModifiers = KeyModifiersUtils.ConvertToKey(keyInput.Modifiers),
                            Source = element,
                        };

                        IVisual currentHandler = element;
                        while (currentHandler != null && !ev.Handled && keyInput.Type == RawKeyEventType.KeyDown)
                        {
                            var bindings = (currentHandler as IInputElement)?.KeyBindings;
                            if (bindings != null)
                                foreach (var binding in bindings)
                                {
                                    if (ev.Handled)
                                        break;
                                    binding.TryHandle(ev);
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
                    Device = this,
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
