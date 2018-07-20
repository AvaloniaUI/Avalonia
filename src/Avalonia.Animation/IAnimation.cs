using System;
using System.Collections.Generic;
using System.Text;
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
        IDisposable Apply(Animatable control, IObservable<bool> match);

        /// <summary>
        /// Run the animation to the specified control
        /// </summary>
        Task Run(Animatable control);
    }
}