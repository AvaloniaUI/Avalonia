using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the NavigationPage.PageInserted event.
    /// </summary>
    public class PageInsertedEventArgs : EventArgs
    {
        /// <param name="page">The page that was inserted.</param>
        /// <param name="before">The page before which the new page was inserted.</param>
        public PageInsertedEventArgs(Page page, Page before)
        {
            Page = page;
            Before = before;
        }

        /// <summary>
        /// Gets the page that was inserted.
        /// </summary>
        public Page Page { get; }

        /// <summary>
        /// Gets the page before which the new page was inserted.
        /// </summary>
        public Page Before { get; }
    }
}
