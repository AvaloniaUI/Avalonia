using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="RelativePoint"/> type.
    /// </summary>  
    public class RelativePointTransition : Transition<RelativePoint>
    {
        internal override IObservable<RelativePoint> DoTransition(IObservable<double> progress, RelativePoint oldValue, RelativePoint newValue) =>
            AnimatorDrivenTransition<RelativePoint, RelativePointAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
