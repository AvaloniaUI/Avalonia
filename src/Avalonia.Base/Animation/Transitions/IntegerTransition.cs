using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="int"/> types.
    /// </summary>  
    public class IntegerTransition : Transition<int>
    {
        internal override IObservable<int> DoTransition(IObservable<double> progress, int oldValue, int newValue) => 
            AnimatorDrivenTransition<int, Int32Animator>.Transition(Easing, progress, oldValue, newValue);
    }
}
