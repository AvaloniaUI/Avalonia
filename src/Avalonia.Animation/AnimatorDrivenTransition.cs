using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    public abstract class AnimatorDrivenTransition<T, TAnimator> : Transition<T> where TAnimator : Animator<T>, new()
    {
        private static readonly TAnimator s_animator = new TAnimator();

        public override IObservable<T> DoTransition(IObservable<double> progress, T oldValue, T newValue)
        {
            return new AnimatorTransitionObservable<T, TAnimator>(s_animator, progress, oldValue, newValue);
        }
    }
}
