using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="Page.Navigating"/> event.
    /// </summary>
    public class NavigatingFromEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigatingFromEventArgs"/> class.
        /// </summary>
        /// <param name="destinationPage">The page that will become active after this navigation, or <see langword="null"/> when popping to root.</param>
        /// <param name="navigationType">The type of navigation that triggered this event.</param>
        public NavigatingFromEventArgs(Page? destinationPage, NavigationType navigationType)
        {
            DestinationPage = destinationPage;
            NavigationType = navigationType;
        }

        /// <summary>
        /// Gets the page that will become active after this navigation.
        /// </summary>
        public Page? DestinationPage { get; }

        /// <summary>
        /// Gets the type of navigation that triggered this event.
        /// </summary>
        public NavigationType NavigationType { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the navigation should be cancelled.
        /// </summary>
        public bool Cancel { get; set; }
    }
}
