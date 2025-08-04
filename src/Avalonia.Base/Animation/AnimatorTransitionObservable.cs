using System;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;

namespace Avalonia.Animation
{
    /// <summary>
    /// Transition observable based on an <see cref="Animator{T}"/> producing a value.
    /// </summary>
    /// <typeparam name="T">Type of the transitioned value.</typeparam>
    /// <typeparam name="TAnimator">Type of the animator.</typeparam>
    internal class AnimatorTransitionObservable<T, TAnimator>(TAnimator animator, IObservable<double> progress, IEasing easing, T oldValue, T newValue) : TransitionObservableBase<T> where TAnimator : Animator<T>
    {

        protected override T ProduceValue(double progress)
        {
            return _animator.Interpolate(progress, oldValue, newValue);
        }
    }
}
