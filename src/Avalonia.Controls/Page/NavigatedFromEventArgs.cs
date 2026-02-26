using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="Page.NavigatedFrom"/> event.
    /// </summary>
    public class NavigatedFromEventArgs : EventArgs
    {
        /// <param name="destinationPage">The page that became active after this navigation, or <see langword="null"/> when popping to root.</param>
        /// <param name="navigationType">The type of navigation that triggered this event.</param>
        public NavigatedFromEventArgs(Page? destinationPage, NavigationType navigationType)
        {
            DestinationPage = destinationPage;
            NavigationType = navigationType;
        }

        /// <summary>
        /// Gets the page that became active after this navigation.
        /// </summary>
        public Page? DestinationPage { get; }

        /// <summary>
        /// Gets the type of navigation that triggered this event.
        /// </summary>
        public NavigationType NavigationType { get; }
    }
}
