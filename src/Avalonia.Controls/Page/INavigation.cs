using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides navigation operations for stack-based and modal page navigation.
    /// Exposed via <see cref="Page.Navigation"/> when a page is hosted in a NavigationPage.
    /// </summary>
    [NotClientImplementable]
    public interface INavigation
    {
        /// <summary>
        /// Gets the current navigation stack. The root page is at index 0; the visible page is last.
        /// </summary>
        IReadOnlyList<Page> NavigationStack { get; }

        /// <summary>
        /// Gets the current modal stack.
        /// Ordering is implementation-defined.
        /// </summary>
        IReadOnlyCollection<Page> ModalStack { get; }

        /// <summary>
        /// Gets the number of pages in the navigation stack.
        /// </summary>
        int StackDepth { get; }

        /// <summary>
        /// Gets whether a pop operation is possible (stack has more than one entry).
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// Pushes <paramref name="page"/> using the host's default transition.
        /// </summary>
        Task PushAsync(Page page);

        /// <summary>
        /// Pushes <paramref name="page"/> using <paramref name="transition"/>. Pass <see langword="null"/> for no animation.
        /// </summary>
        Task PushAsync(Page page, IPageTransition? transition);

        /// <summary>
        /// Pops the top page using the host's default transition.
        /// </summary>
        Task<Page?> PopAsync();

        /// <summary>
        /// Pops the top page using <paramref name="transition"/>. Pass <see langword="null"/> for no animation.
        /// </summary>
        Task<Page?> PopAsync(IPageTransition? transition);

        /// <summary>
        /// Pops all pages above the root using the host's default transition.
        /// </summary>
        Task PopToRootAsync();

        /// <summary>
        /// Pops all pages above the root using <paramref name="transition"/>. Pass <see langword="null"/> for no animation.
        /// </summary>
        Task PopToRootAsync(IPageTransition? transition);

        /// <summary>
        /// Pops all pages above <paramref name="page"/> using the host's default transition.
        /// </summary>
        Task PopToPageAsync(Page page);

        /// <summary>
        /// Pops all pages above <paramref name="page"/> using <paramref name="transition"/>. Pass <see langword="null"/> for no animation.
        /// </summary>
        Task PopToPageAsync(Page page, IPageTransition? transition);

        /// <summary>
        /// Replaces the current top page with <paramref name="page"/> using the host's default transition.
        /// </summary>
        Task ReplaceAsync(Page page);

        /// <summary>
        /// Replaces the current top page with <paramref name="page"/> using <paramref name="transition"/>. Pass <see langword="null"/> for no animation.
        /// </summary>
        Task ReplaceAsync(Page page, IPageTransition? transition);

        /// <summary>
        /// Pushes <paramref name="page"/> as a modal using the host's modal transition.
        /// </summary>
        Task PushModalAsync(Page page);

        /// <summary>
        /// Pushes <paramref name="page"/> as a modal using <paramref name="transition"/>. Pass <see langword="null"/> for no animation.
        /// </summary>
        Task PushModalAsync(Page page, IPageTransition? transition);

        /// <summary>
        /// Pops the top modal page using the host's modal transition.
        /// </summary>
        Task<Page?> PopModalAsync();

        /// <summary>
        /// Pops the top modal page using <paramref name="transition"/>. Pass <see langword="null"/> for no animation.
        /// </summary>
        Task<Page?> PopModalAsync(IPageTransition? transition);

        /// <summary>
        /// Pops all modal pages, animating only the topmost dismissal.
        /// </summary>
        Task PopAllModalsAsync();

        /// <summary>
        /// Pops all modal pages using <paramref name="transition"/>. Pass <see langword="null"/> for no animation.
        /// </summary>
        Task PopAllModalsAsync(IPageTransition? transition);

        /// <summary>
        /// Inserts <paramref name="page"/> immediately before <paramref name="before"/> in the stack.
        /// Does not change the currently visible page.
        /// </summary>
        void InsertPage(Page page, Page before);

        /// <summary>
        /// Removes <paramref name="page"/> from the navigation stack without animation.
        /// </summary>
        void RemovePage(Page page);
    }
}
