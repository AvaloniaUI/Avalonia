using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Animation objects
    /// </summary>
    [NotClientImplementable]
    public interface IAnimation
    {
        /// <summary>
        /// Apply the animation to the specified control and run it when <paramref name="match" /> produces <c>true</c>.
        /// </summary>
        internal IDisposable Apply(Animatable control, IClock? clock, IObservable<bool> match, Action? onComplete = null);

        /// <summary>
        /// Run the animation on the specified control.
        /// </summary>
        internal Task RunAsync(Animatable control, IClock clock, CancellationToken cancellationToken = default);
    }
}
