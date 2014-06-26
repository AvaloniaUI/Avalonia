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
    using Perspex.Controls;
    using Perspex.Input.Raw;
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

        public IInputManager InputManager
        {
            get { return Locator.Current.GetService<IInputManager>(); }
        }

        public IFocusManager FocusManager
        {
            get { return Locator.Current.GetService<IFocusManager>(); }
        }

        public abstract ModifierKeys Modifiers { get; }

        private void ProcessRawEvent(RawKeyEventArgs e)
        {
            Interactive interactive = FocusManager.Current as Interactive;

            if (interactive != null)
            {
                switch (e.Type)
                {
                    case RawKeyEventType.KeyDown:
                        interactive.RaiseEvent(new KeyEventArgs
                        {
                            RoutedEvent = Control.KeyDownEvent,
                            Device = this,
                            Key = e.Key,
                            Text = e.Text,
                            Source = interactive,
                            OriginalSource = interactive,
                        });
                        break;
                }
            }
        }
    }
}
