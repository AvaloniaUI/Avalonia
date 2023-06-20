using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="CornerRadius"/> type.
    /// </summary>  
    public class CornerRadiusTransition : Transition<CornerRadius>
    {
        internal override IObservable<CornerRadius> DoTransition(IObservable<double> progress, CornerRadius oldValue,
            CornerRadius newValue) =>
            AnimatorDrivenTransition<CornerRadius, CornerRadiusAnimator>.Transition(Easing, progress, oldValue,
                newValue);
    }
}
