// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public class KeyboardDevice : IKeyboardDevice, INotifyPropertyChanged
    {
        private IInputElement _focusedElement;

        public event PropertyChangedEventHandler PropertyChanged;

        public static IKeyboardDevice Instance => AvaloniaLocator.Current.GetService<IKeyboardDevice>();

        public IInputManager InputManager => AvaloniaLocator.Current.GetService<IInputManager>();

        public IFocusManager FocusManager => AvaloniaLocator.Current.GetService<IFocusManager>();

        public IInputElement FocusedElement
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

        public void SetFocusedElement(
            IInputElement element, 
            NavigationMethod method,
            InputModifiers modifiers)
        {
            if (element != FocusedElement)
            {
                var interactive = FocusedElement as IInteractive;

                interactive?.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = InputElement.LostFocusEvent,
                });

                FocusedElement = element;
                interactive = element as IInteractive;

                interactive?.RaiseEvent(new GotFocusEventArgs
                {
                    RoutedEvent = InputElement.GotFocusEvent,
                    NavigationMethod = method,
                    InputModifiers = modifiers,
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
            IInputElement element = FocusedElement;

            if (element != null)
            {
                var keyInput = e as RawKeyEventArgs;

                if (keyInput != null)
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
                                Modifiers = keyInput.Modifiers,
                                Source = element,
                            };

                            IVisual currentHandler = element;
                            while (currentHandler != null && !ev.Handled && keyInput.Type == RawKeyEventType.KeyDown)
                            {
                                var bindings = (currentHandler as IInputElement)?.KeyBindings;
                                if(bindings!=null)
                                    foreach (var binding in bindings)
                                    {
                                        if(ev.Handled)
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

                var text = e as RawTextInputEventArgs;

                if (text != null)
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
}
