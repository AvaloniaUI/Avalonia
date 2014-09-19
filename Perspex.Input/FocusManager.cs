// -----------------------------------------------------------------------
// <copyright file="IFocusManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using Perspex.Interactivity;
    using Splat;

    public class FocusManager : IFocusManager
    {
        public IInputElement Current
        {
            get;
            private set;
        }

        public void Focus(IInputElement control)
        {
            IInteractive current = this.Current as IInteractive;
            IInteractive next = control as IInteractive;

            if (current != null)
            {
                current.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = InputElement.LostFocusEvent,
                    Source = current,
                    OriginalSource = current,
                });
            }

            this.Current = control;

            IKeyboardDevice keyboard = Locator.Current.GetService<IKeyboardDevice>();

            if (keyboard != null)
            {
                keyboard.FocusedElement = control;
            }

            if (next != null)
            {
                next.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = InputElement.GotFocusEvent,
                    Source = next,
                    OriginalSource = next,
                });
            }
        }
    }
}
