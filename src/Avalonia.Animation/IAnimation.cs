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
        IDisposable Apply(Animatable control, IClock clock, IObservable<bool> match, Action onComplete = null);

        /// <summary>
        /// Run the animation to the specified control
        /// </summary>
        Task RunAsync(Animatable control, IClock clock);
    }
}
