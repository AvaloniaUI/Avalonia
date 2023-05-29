using System;
using Avalonia.Animation.Animators;

namespace Avalonia.Animation;

public abstract class CustomAnimatorBase
{
    internal abstract IAnimator CreateWrapper();
    internal abstract Type WrapperType { get; }
}

public abstract class CustomAnimatorBase<T> : CustomAnimatorBase
{
    public abstract T Interpolate(double progress, T oldValue, T newValue);

    internal override Type WrapperType => typeof(AnimatorWrapper);
    internal override IAnimator CreateWrapper() => new AnimatorWrapper(this);

    internal class AnimatorWrapper : Animator<T>
    {
        private readonly CustomAnimatorBase<T> _parent;

        public AnimatorWrapper(CustomAnimatorBase<T> parent)
        {
            _parent = parent;
        }
        
        public override T Interpolate(double progress, T oldValue, T newValue) => _parent.Interpolate(progress, oldValue, newValue);
    }
}