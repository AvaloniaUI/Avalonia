// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Perspex.Input.Raw;
using Perspex.Interactivity;
using Splat;

namespace Perspex.Input
{
    public abstract class KeyboardDevice : IKeyboardDevice, INotifyPropertyChanged
    {
        private IInputElement _focusedElement;

        public KeyboardDevice()
        {
            InputManager.RawEventReceived
                .OfType<RawInputEventArgs>()
                .Where(x => x.Device == this)
                .Subscribe(ProcessRawEvent);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static IKeyboardDevice Instance
        {
            get { return Locator.Current.GetService<IKeyboardDevice>(); }
        }

        public IInputManager InputManager
        {
            get { return Locator.Current.GetService<IInputManager>(); }
        }

        public IFocusManager FocusManager
        {
            get { return Locator.Current.GetService<IFocusManager>(); }
        }

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

        public void SetFocusedElement(IInputElement element, NavigationMethod method)
        {
            if (element != FocusedElement)
            {
                var interactive = FocusedElement as IInteractive;

                if (interactive != null)
                {
                    interactive.RaiseEvent(new RoutedEventArgs
                    {
                        RoutedEvent = InputElement.LostFocusEvent,
                    });
                }

                FocusedElement = element;
                interactive = element as IInteractive;

                if (interactive != null)
                {
                    interactive.RaiseEvent(new GotFocusEventArgs
                    {
                        RoutedEvent = InputElement.GotFocusEvent,
                        NavigationMethod = method,
                    });
                }
            }
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ProcessRawEvent(RawInputEventArgs e)
        {
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

                            element.RaiseEvent(ev);
                            break;
                    }
                }
                var text = e as RawTextInputEventArgs;
                if (text != null)
                {
                    element.RaiseEvent(new TextInputEventArgs()
                    {
                        Device = this,
                        Text = text.Text,
                        Source = element,
                        RoutedEvent = InputElement.TextInputEvent
                    });
                }
            }
        }
    }
}
