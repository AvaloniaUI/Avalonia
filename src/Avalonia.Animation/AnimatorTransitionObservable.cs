using System;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;

namespace Avalonia.Animation
{
    public class AnimatorTransitionObservable<T, TAnimator> : TransitionObservableBase<T> where TAnimator : Animator<T>
    {
        private readonly TAnimator _animator;
        private readonly Easing _easing;
        private readonly T _oldValue;
        private readonly T _newValue;

        public AnimatorTransitionObservable(TAnimator animator, IObservable<double> progress, Easing easing, T oldValue, T newValue) : base(progress)
        {
            _animator = animator;
            _easing = easing;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        protected override T ProduceValue(double progress)
        {
            progress = _easing.Ease(progress);

            return _animator.Interpolate(progress, _oldValue, _newValue);
        }
    }
}
