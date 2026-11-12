using System.Threading.Tasks;
using Avalonia.Animation;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides extension methods for <see cref="INavigation"/> to simplify navigation operations.
    /// </summary>
    public static class PageNavigationExtensions
    {
        extension(INavigation navigation)
        {
            /// <summary>
            /// Pushes a new page of type <typeparamref name="T"/> onto the navigation stack, using <paramref name="transition"/> with optional <paramref name="parameter"/>.
            /// </summary>
            public Task PushAsync<T>(IPageTransition? transition = null, object? parameter = null) where T : Page, new()
            {
                var page = new T();
                return navigation.PushAsync(page, transition, parameter);
            }

            /// <summary>
            /// Replaces the current top page with a new page of type <typeparamref name="T"/>, using <paramref name="transition"/> with optional <paramref name="parameter"/>.
            /// </summary>
            public Task ReplaceAsync<T>(IPageTransition? transition = null, object? parameter = null) where T : Page, new()
            {
                var page = new T();
                return navigation.ReplaceAsync(page, transition, parameter);
            }

            /// <summary>
            /// Pushes a new modal page of type <typeparamref name="T"/> onto the modal stack, using <paramref name="transition"/> with optional <paramref name="parameter"/>.
            /// </summary>
            public Task PushModalAsync<T>(IPageTransition? transition = null, object? parameter = null) where T : Page, new()
            {
                var page = new T();
                return navigation.PushModalAsync(page, transition, parameter);
            }
        }
    }
}
