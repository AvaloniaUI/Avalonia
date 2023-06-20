using System;
using Avalonia.Animation.Animators;
using Avalonia.Animation.Easings;

namespace Avalonia.Animation
{
    /// <summary>
    /// <see cref="Transition{T}"/> using an <see cref="Animator{T}"/> to transition between values.
    /// </summary>
    /// <typeparam name="T">Type of the transitioned value.</typeparam>
    /// <typeparam name="TAnimator">Type of the animator.</typeparam>
    internal static class AnimatorDrivenTransition<T, TAnimator> where TAnimator : Animator<T>, new()
    {
        private static readonly TAnimator s_animator = new TAnimator();

        public static IObservable<T> Transition(IEasing easing, IObservable<double> progress, T oldValue, T newValue) =>
            new AnimatorTransitionObservable<T, TAnimator>(s_animator, progress, easing, oldValue, newValue);
    }
}
