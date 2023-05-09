using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for animations that transition between two pages.
    /// </summary>
    public interface IPageTransition
    {
        /// <summary>
        /// Starts the animation.
        /// </summary>
        /// <param name="from">
        /// The control that is being transitioned away from. May be null.
        /// </param>
        /// <param name="to">
        /// The control that is being transitioned to. May be null.
        /// </param>
        /// <param name="forward">
        /// If the animation is bidirectional, controls the direction of the animation.
        /// </param>
        /// <param name="cancellationToken">
        /// Animation cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        /// <remarks>
        /// The <paramref name="from"/> and <paramref name="to"/> controls will be made visible
        /// and <paramref name="from"/> transitioned to <paramref name="to"/>. At the end of the
        /// animation (when the returned task completes), <paramref name="from"/> will be made
        /// invisible but all other properties involved in the transition will have been left
        /// unchanged.
        /// </remarks>
        Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken);
    }
}
