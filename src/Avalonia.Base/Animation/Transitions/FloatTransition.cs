using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="float"/> types.
    /// </summary>  
    public class FloatTransition : Transition<float>
    {
        internal override IObservable<float> DoTransition(IObservable<double> progress, float oldValue, float newValue) => 
            AnimatorDrivenTransition<float, FloatAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
