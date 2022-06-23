using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class KeyboardDevice : IKeyboardDevice, INotifyPropertyChanged
    {
        private IInputElement? _focusedElement;
        private readonly TextInputMethodManager _textInputManager = new TextInputMethodManager();

        public event PropertyChangedEventHandler? PropertyChanged;

        public static IKeyboardDevice? Instance => AvaloniaLocator.Current.GetService<IKeyboardDevice>();

        public IInputManager? InputManager => AvaloniaLocator.Current.GetService<IInputManager>();

        public IInputElement? FocusedElement => _focusedElement;

        public void SetFocusedElement(IInputElement? element)
        {
            _focusedElement = element;
            _textInputManager.SetFocusedElement(element);
            RaisePropertyChanged(nameof(FocusedElement));
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
                            KeyModifiers = keyInput.Modifiers.ToKeyModifiers(),
                            Source = element,
                        };

                        IVisual? currentHandler = element;
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
