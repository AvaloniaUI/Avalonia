using System;
using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="Color"/> type.
    /// </summary>
    public class ColorTransition : Transition<Color>
    {
        internal override IObservable<Color> DoTransition(IObservable<double> progress, Color oldValue, Color newValue)
            => AnimatorDrivenTransition<Color, ColorAnimator>.Transition(Easing, progress, oldValue, newValue);
    }
}
