using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Thickness"/> type.
    /// </summary>  
    public class ThicknessTransition : Transition<Thickness>
    {
        internal override IObservable<Thickness> DoTransition(IObservable<double> progress, Thickness oldValue, Thickness newValue) => 
            AnimatorDrivenTransition<Thickness, ThicknessAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
