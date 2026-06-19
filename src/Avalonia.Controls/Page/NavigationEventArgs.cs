using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for <see cref="NavigationPage"/> push and pop events.
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationEventArgs"/> class.
        /// </summary>
        /// <param name="page">The page involved in the navigation operation.</param>
        /// <param name="navigationType">The type of navigation that triggered this event.</param>
        public NavigationEventArgs(Page page, NavigationType navigationType)
        {
            Page = page;
            NavigationType = navigationType;
        }

        /// <summary>
        /// Gets the page involved in the navigation operation.
        /// </summary>
        public Page Page { get; }

        /// <summary>
        /// Gets the type of navigation that triggered this event.
        /// </summary>
        public NavigationType NavigationType { get; }
    }
}
