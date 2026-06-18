using System.Collections.Generic;
using Avalonia.VisualTree;

namespace Avalonia.Animation
{
    /// <summary>
    /// An <see cref="IPageTransition"/> that supports progress-driven updates.
    /// </summary>
    /// <remarks>
    /// Transitions implementing this interface can be driven by a normalized progress value
    /// (0.0 to 1.0) during swipe gestures or programmatic animations, rather than running
    /// as a timed animation via <see cref="IPageTransition.Start"/>.
    /// </remarks>
    public interface IProgressPageTransition : IPageTransition
    {
        /// <summary>
        /// Updates the transition to reflect the given progress.
        /// </summary>
        /// <param name="progress">The normalized progress value from 0.0 (start) to 1.0 (complete).</param>
        /// <param name="from">The visual being transitioned away from. May be null.</param>
        /// <param name="to">The visual being transitioned to. May be null.</param>
        /// <param name="forward">Whether the transition direction is forward (next) or backward (previous).</param>
        /// <param name="pageLength">The size of a page along the transition axis.</param>
        /// <param name="visibleItems">The currently visible realized pages, if more than one page is visible.</param>
        void Update(
            double progress,
            Visual? from,
            Visual? to,
            bool forward,
            double pageLength,
            IReadOnlyList<PageTransitionItem> visibleItems);

        /// <summary>
        /// Resets any visual state applied to the given visual by this transition.
        /// </summary>
        /// <param name="visual">The visual to reset.</param>
        void Reset(Visual visual);
    }
}
