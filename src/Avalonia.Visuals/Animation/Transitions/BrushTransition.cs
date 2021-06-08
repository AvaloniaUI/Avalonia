using System;
using Avalonia.Animation.Animators;
using Avalonia.Media;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="IBrush"/> type.
    /// Only values of <see cref="ISolidColorBrush"/> will correctly transition.
    /// </summary>
    public class ISolidColorBrushTransition : Transition<IBrush>
    {
        private static readonly ISolidColorBrushAnimator s_animator = new ISolidColorBrushAnimator();

        public override IObservable<IBrush> DoTransition(IObservable<double> progress, IBrush oldValue, IBrush newValue)
        {
            var oldSolidBrush = AsImmutable(oldValue);
            var newSolidBrush = AsImmutable(newValue);

            return new AnimatorTransitionObservable<ISolidColorBrush, ISolidColorBrushAnimator>(
                s_animator, progress, Easing, oldSolidBrush, newSolidBrush);
        }

        private static ISolidColorBrush AsImmutable(IBrush brush)
        {
            return (ISolidColorBrush)(brush as ISolidColorBrush)?.ToImmutable();
        }
    }
}
