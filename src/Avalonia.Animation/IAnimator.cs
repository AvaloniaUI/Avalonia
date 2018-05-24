using System;
using System.Collections.Generic;
using System.Text;

namespace Avalonia.Animation
{
    /// <summary>
    /// Interface for Animator objects
    /// </summary>
    public interface IAnimator : IList<AnimatorKeyFrame>
    {
        /// <summary>
        /// The target property.
        /// </summary>
        AvaloniaProperty Property {get; set;}

        /// <summary>
        /// Applies the current KeyFrame group to the specified control.
        /// </summary>
        IDisposable Apply(Animation animation, Animatable control, IObservable<bool> obsMatch);
        
    }
}
