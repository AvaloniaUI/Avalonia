using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    public class FocusChangingEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Provides data for focus changing.
        /// </summary>
        internal FocusChangingEventArgs(RoutedEvent routedEvent) : base(routedEvent)
        {
        }

        /// <summary>
        /// Gets or sets the element that focus has moved to.
        /// </summary>
        public IInputElement? NewFocus { get; init; }

        /// <summary>
        /// Gets or sets the element that previously had focus.
        /// </summary>
        public IInputElement? OldFocus { get; init; }

        /// <summary>
        /// Gets or sets a value indicating how the change in focus occurred.
        /// </summary>
        public NavigationMethod NavigationMethod { get; init; }

        /// <summary>
        /// Gets or sets any key modifiers active at the time of focus.
        /// </summary>
        public KeyModifiers KeyModifiers { get; init; }

        /// <summary>
        /// Gets whether focus change is canceled.
        /// </summary>
        public bool Cancelled { get; private set; }

        internal bool IsCancellable { get; init; }

        /// <summary>
        /// Attempts to cancel the current focus change
        /// </summary>
        /// <returns>true if focus change was cancelled; otherwise, false</returns>
        public bool TryCancel()
        {
            Cancelled = IsCancellable;

            return Cancelled;
        }
    }
}
