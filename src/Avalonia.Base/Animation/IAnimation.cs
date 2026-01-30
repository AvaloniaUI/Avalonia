using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Animation objects
    /// </summary>
    [NotClientImplementable, PrivateApi]
    public interface IAnimation
    {
    }

    [NotClientImplementable, PrivateApi]
    public interface ICompositionAnimation : IAnimation
    {
        /// <summary>
        /// Occurs when the transition is invalidated and needs to be re-applied.
        /// </summary>
        event EventHandler? AnimationInvalidated;

        /// <summary>
        /// Apply the animation to the specified visual and return a disposable to remove it.
        /// </summary>
        IDisposable Apply(Visual parent);
    }

    /// <summary>
    /// Interface for Animation objects
    /// </summary>
    [NotClientImplementable, PrivateApi]
    public interface IPropertyAnimation : IAnimation
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
