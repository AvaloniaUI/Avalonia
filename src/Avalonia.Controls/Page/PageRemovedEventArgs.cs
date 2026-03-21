using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="NavigationPage.PageRemoved"/> event.
    /// </summary>
    public class PageRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PageRemovedEventArgs"/> class.
        /// </summary>
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
