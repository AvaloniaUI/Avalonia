using Avalonia.Animation;

namespace Avalonia.Animation
{
    /// <summary>
    /// An <see cref="IPageTransition"/> that supports interactive, gesture-driven updates.
    /// </summary>
    /// <remarks>
    /// Transitions implementing this interface can be driven by a normalized progress value
    /// (0.0 to 1.0) during swipe gestures, rather than running as a timed animation.
    /// </remarks>
    public interface IInteractivePageTransition : IPageTransition
    {
        /// <summary>
        /// Updates the transition to reflect the given progress.
        /// </summary>
        /// <param name="progress">The normalized progress value from 0.0 (start) to 1.0 (complete).</param>
        /// <param name="from">The visual being transitioned away from. May be null.</param>
        /// <param name="to">The visual being transitioned to. May be null.</param>
        /// <param name="forward">Whether the transition direction is forward (next) or backward (previous).</param>
        /// <param name="orientation">The slide orientation for the transition.</param>
        /// <param name="size">The size of the transition area.</param>
        void Update(double progress, Visual? from, Visual? to, bool forward, PageSlide.SlideAxis orientation, Size size);
    }
}
