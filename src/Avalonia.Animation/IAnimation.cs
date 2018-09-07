using System;
using System.Threading.Tasks;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Animation objects
    /// </summary>
    public interface IAnimation
    {
        /// <summary>
        /// Apply the animation to the specified control
        /// </summary>
        IDisposable Apply(Animatable control, Clock clock, IObservable<bool> match, Action onComplete = null);

        /// <summary>
        /// Run the animation to the specified control
        /// </summary>
        Task RunAsync(Animatable control, Clock clock);
    }
}
