using System;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides event data for RefreshRequested events.
    /// </summary>
    public class RefreshRequestedEventArgs : RoutedEventArgs
    {
        private RefreshCompletionDeferral _refreshCompletionDeferral;

        /// <summary>
        /// Gets a deferral object for managing the work done in the RefreshRequested event handler.
        /// </summary>
        /// <returns>A <see cref="RefreshCompletionDeferral"/> object</returns>
        public RefreshCompletionDeferral GetDeferral()
        {
            return _refreshCompletionDeferral.Get();
        }

        public RefreshRequestedEventArgs(Action deferredAction, RoutedEvent? routedEvent) : base(routedEvent)
        {
            _refreshCompletionDeferral = new RefreshCompletionDeferral(deferredAction);
        }

        public RefreshRequestedEventArgs(RefreshCompletionDeferral completionDeferral, RoutedEvent? routedEvent) : base(routedEvent)
        {
            _refreshCompletionDeferral = completionDeferral;
        }

        internal void IncrementCount()
        {
            _refreshCompletionDeferral?.Get();
        }

        internal void DecrementCount()
        {
            _refreshCompletionDeferral?.Complete();
        }
    }
}
