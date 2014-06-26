// -----------------------------------------------------------------------
// <copyright file="IFocusManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Perspex.Controls;

namespace Perspex.Input
{
    public class FocusManager : IFocusManager
    {
        public IInputElement Current
        {
            get;
            private set;
        }

        public void Focus(IInputElement control)
        {
            Interactive current = this.Current as Interactive;
            Interactive next = control as Interactive;

            if (current != null)
            {
                current.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = Control.LostFocusEvent,
                    Source = current,
                    OriginalSource = current,
                });
            }

            this.Current = control;

            if (next != null)
            {
                next.RaiseEvent(new RoutedEventArgs
                {
                    RoutedEvent = Control.GotFocusEvent,
                    Source = next,
                    OriginalSource = next,
                });
            }
        }
    }
}
