using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
