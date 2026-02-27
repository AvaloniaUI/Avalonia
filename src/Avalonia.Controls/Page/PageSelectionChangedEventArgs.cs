using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for a page selection-changed event.
    /// </summary>
    public class PageSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageSelectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousPage">The page that was selected before the change, or <see langword="null"/> if no page was selected.</param>
        /// <param name="currentPage">The page that is now selected, or <see langword="null"/> if selection was cleared.</param>
        public PageSelectionChangedEventArgs(Page? previousPage, Page? currentPage)
        {
            PreviousPage = previousPage;
            CurrentPage = currentPage;
        }

        /// <summary>
        /// Gets the page that was selected before the change.
        /// </summary>
        public Page? PreviousPage { get; }

        /// <summary>
        /// Gets the page that is now selected.
        /// </summary>
        public Page? CurrentPage { get; }
    }
}
