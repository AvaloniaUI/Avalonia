using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="bool"/> types.
    /// </summary>  
    public class BoolTransition : Transition<bool>
    {
        internal override IObservable<bool> DoTransition(IObservable<double> progress, bool oldValue, bool newValue) => 
            AnimatorDrivenTransition<bool, BoolAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
