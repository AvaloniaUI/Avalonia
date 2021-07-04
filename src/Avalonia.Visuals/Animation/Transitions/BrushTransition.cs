using System;
using System.Linq;

using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;
using Avalonia.Media;

#nullable enable

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition class that handles <see cref="AvaloniaProperty"/> with <see cref="IBrush"/> type.
    /// </summary>
    public class BrushTransition : Transition<IBrush?>
    {
        public override IObservable<IBrush?> DoTransition(IObservable<double> progress, IBrush? oldValue, IBrush? newValue)
        {
            var type = oldValue?.GetType() ?? newValue?.GetType();
            if (type == null)
            {
                return new IncompatibleTransitionObservable(progress, Easing, oldValue, newValue);
            }

            var animator = BaseBrushAnimator.CreateAnimatorFromType(type);
            if (animator == null)
            {
                return new IncompatibleTransitionObservable(progress, Easing, oldValue, newValue);
            }

            var animatorType = animator.GetType();
            var animatorGenericArgument = animatorType.BaseType.GetGenericArguments().FirstOrDefault() ?? type;

            var observableType = typeof(AnimatorTransitionObservable<,>).MakeGenericType(animatorGenericArgument, animatorType);
            var observable = Activator.CreateInstance(observableType, animator, progress, Easing, oldValue, newValue) as IObservable<IBrush>;
            if (observable == null)
            {
                return new IncompatibleTransitionObservable(progress, Easing, oldValue, newValue);
            }

            return observable;
        }

        private class IncompatibleTransitionObservable : TransitionObservableBase<IBrush?>
        {
            private readonly IBrush? _from;
            private readonly IBrush? _to;

            public IncompatibleTransitionObservable(IObservable<double> progress, Easing easing, IBrush? from, IBrush? to) : base(progress, easing)
            {
                _from = from;
                _to = to;
            }

            protected override IBrush? ProduceValue(double progress)
            {
                return progress < 0.5 ? _from : _to;
            }
        }
    }
}
