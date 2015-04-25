// -----------------------------------------------------------------------
// <copyright file="KeyboardDevice.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Input.Raw;
    using Perspex.Interactivity;
    using Splat;

    public abstract class KeyboardDevice : IKeyboardDevice
    {
        public KeyboardDevice()
        {
            this.InputManager.RawEventReceived
                .OfType<RawKeyEventArgs>()
                .Where(x => x.Device == this)
                .Subscribe(this.ProcessRawEvent);
        }

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
            get;
            private set;
        }

        public abstract ModifierKeys Modifiers { get; }

        public void SetFocusedElement(IInputElement element, bool keyboardNavigated)
        {
            var interactive = this.FocusedElement as IInteractive;

            if (interactive != null)
            {
                interactive.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = InputElement.LostFocusEvent,
                });
            }

            this.FocusedElement = element;
            interactive = element as IInteractive;

            if (interactive != null)
            {
                interactive.RaiseEvent(new GotFocusEventArgs
                {
                    RoutedEvent = InputElement.GotFocusEvent,
                    KeyboardNavigated = keyboardNavigated,
                });
            }
        }

        private void ProcessRawEvent(RawKeyEventArgs e)
        {
            IInputElement element = this.FocusedElement;

            if (element != null)
            {
                switch (e.Type)
                {
                    case RawKeyEventType.KeyDown:
                        KeyEventArgs ev = new KeyEventArgs
                        {
                            RoutedEvent = InputElement.KeyDownEvent,
                            Device = this,
                            Key = e.Key,
                            Text = e.Text,
                            Source = element,
                            OriginalSource = element,
                        };

                        element.RaiseEvent(ev);
                        break;
                }
            }
        }
    }
}
