using System;
using System.Reflection.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides data for the <see cref="Page.NavigatedFrom"/> event.
    /// </summary>
    public class NavigatedFromEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigatedFromEventArgs"/> class.
        /// </summary>
        /// <param name="destinationPage">The page that became active after this navigation, or <see langword="null"/> when popping to root.</param>
        /// <param name="navigationType">The type of navigation that triggered this event.</param>
        public NavigatedFromEventArgs(Page? destinationPage, NavigationType navigationType)
        {
            DestinationPage = destinationPage;
            NavigationType = navigationType;
        }

        public NavigatedFromEventArgs(Page? destinationPage, NavigationType navigationType, object? parameter) : this(destinationPage, navigationType)
        {
            Parameter = parameter;
        }

        /// <summary>
        /// Gets the page that became active after this navigation.
        /// </summary>
        public Page? DestinationPage { get; }

        /// <summary>
        /// Gets the type of navigation that triggered this event.
        /// </summary>
        public NavigationType NavigationType { get; }

        public object? Parameter { get; }
    }
}
