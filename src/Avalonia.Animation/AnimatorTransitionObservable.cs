using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation
{
    public class AnimatorTransitionObservable<T, TAnimator> : TransitionObservableBase<T> where TAnimator : Animator<T>
    {
        private readonly TAnimator _animator;
        private readonly T _oldValue;
        private readonly T _newValue;

        public AnimatorTransitionObservable(TAnimator animator, IObservable<double> progress, T oldValue, T newValue) : base(progress)
        {
            _animator = animator;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        protected override T ProduceValue(double progress)
        {
            return _animator.Interpolate(progress, _oldValue, _newValue);
        }
    }
}