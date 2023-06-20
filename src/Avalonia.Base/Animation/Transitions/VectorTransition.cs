using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Vector"/> type.
    /// </summary>  
    public class VectorTransition : Transition<Vector>
    {
        internal override IObservable<Vector> DoTransition(IObservable<double> progress, Vector oldValue, Vector newValue) => 
            AnimatorDrivenTransition<Vector, VectorAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
