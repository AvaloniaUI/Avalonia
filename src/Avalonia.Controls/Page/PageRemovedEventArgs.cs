using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the NavigationPage.PageRemoved event.
    /// </summary>
    public class PageRemovedEventArgs : EventArgs
    {
        /// <param name="page">The page that was removed.</param>
        public PageRemovedEventArgs(Page page)
        {
            Page = page;
        }

        /// <summary>
        /// Gets the page that was removed.
        /// </summary>
        public Page Page { get; }
    }
}
