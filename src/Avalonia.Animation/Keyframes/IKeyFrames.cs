using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Animation.Keyframes
{
    /// <summary>
    /// Interface for Keyframe group object
    /// </summary>
    public interface IKeyFrames
    {
        /// <summary>
        /// Applies the current KeyFrame group to the specified control.
        /// </summary>
        IDisposable Apply(Animation animation, Animatable control, ulong IterationToken, IObservable<bool> obsMatch);
    }
}
