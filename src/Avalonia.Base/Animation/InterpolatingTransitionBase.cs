using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

/// <summary>
/// The base class for user-defined transition that are doing simple value interpolation
/// </summary>
public abstract class InterpolatingTransitionBase<T> : Transition<T>
{
    class Animator : Animator<T>
    {
        private readonly InterpolatingTransitionBase<T> _parent;

        public Animator(InterpolatingTransitionBase<T> parent)
        {
            _parent = parent;
        }
        
        public override T Interpolate(double progress, T oldValue, T newValue) =>
            _parent.Interpolate(progress, oldValue, newValue);
    }
    
    protected abstract T Interpolate(double progress, T from, T to);
    
    internal override IObservable<T> DoTransition(IObservable<double> progress, T oldValue, T newValue) =>
        new AnimatorTransitionObservable<T, Animator>(new Animator(this), progress, Easing, oldValue, newValue);
}