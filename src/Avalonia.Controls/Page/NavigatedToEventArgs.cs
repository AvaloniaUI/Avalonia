using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="Page.NavigatedTo"/> event.
    /// </summary>
    public class NavigatedToEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigatedToEventArgs"/> class.
        /// </summary>
        /// <param name="previousPage">The page that was active before this navigation, or <see langword="null"/> for the root page.</param>
        /// <param name="navigationType">The type of navigation that triggered this event.</param>
        public NavigatedToEventArgs(Page? previousPage, NavigationType navigationType)
        {
            PreviousPage = previousPage;
            NavigationType = navigationType;
        }

        /// <summary>
        /// Gets the page that was active before this navigation.
        /// </summary>
        public Page? PreviousPage { get; }

        /// <summary>
        /// Gets the type of navigation that triggered this event.
        /// </summary>
        public NavigationType NavigationType { get; }
    }
}
