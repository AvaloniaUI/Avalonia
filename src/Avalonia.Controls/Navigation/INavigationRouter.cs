using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    public interface INavigationRouter
    {
        /// <summary>
        /// Event triggered when navigation is complete.
        /// </summary>
        event EventHandler<NavigatedEventArgs>? Navigated;

        /// <summary>
        /// If true, then <see cref="BackAsync"/> or <see cref="ClearAsync"/> will leave the <see cref="CurrentPage"/> null.
        /// Otherwise there will always be a page left on the <see cref="CurrentPage"/>.
        /// </summary>
        bool AllowEmpty { get; set; }

        /// <summary>
        /// True if its possible to navigate backwards.
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// The currently navigated page. Can be null if no navigation has taken place or
        /// the user cleared (see <see cref="AllowEmpty"/>
        /// </summary>
        object? CurrentPage { get; }

        /// <summary>
        /// Navigates to the next page in the stack if there is one.
        /// </summary>
        /// <returns>Task to await the navigation process.</returns>
        Task ForwardAsync();

        /// <summary>
        /// True if its possible to navigate forward.
        /// </summary>
        bool CanGoForward { get; }

        /// <summary>
        /// Navigates to a new page.
        /// </summary>
        /// <param name="destination">The destination page / url / viewmodel depending on the implementation.</param>
        /// <param name="mode">How the navigation stack will behave when navigating.</param>
        /// <returns>Task to await the navigation process.</returns>
        Task NavigateToAsync(object? destination, NavigationMode mode = NavigationMode.Normal);

        /// <summary>
        /// Navigates to the previous page in the stack if there is one.
        /// </summary>
        /// <returns>Task to await the navigation process.</returns>
        Task BackAsync();

        /// <summary>
        /// Clears the navigation stack and navigates to the last page or null according to <see cref="AllowEmpty"/>
        /// </summary>
        /// <returns>Task to await the navigation process.</returns>
        Task ClearAsync();
    }

    public class NavigatedEventArgs
    {
        public NavigatedEventArgs(object? from, object? to)
        {
            From = from;
            To = to;
        }

        public object? From { get; }
        public object? To { get; }
    }
}
