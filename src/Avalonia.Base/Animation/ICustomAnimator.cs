using System;
using Avalonia.Animation.Animators;
namespace Avalonia.Animation;

public interface ICustomAnimator
{
    internal IAnimator CreateWrapper();
    internal Type WrapperType { get; }
}

public abstract class InterpolatingAnimator<T> : ICustomAnimator
{
    public abstract T Interpolate(double progress, T oldValue, T newValue);

    Type ICustomAnimator.WrapperType => typeof(AnimatorWrapper);
    IAnimator ICustomAnimator.CreateWrapper() => new AnimatorWrapper(this);
    internal IAnimator CreateWrapper() => new AnimatorWrapper(this);

    internal class AnimatorWrapper : Animator<T>
    {
        private readonly InterpolatingAnimator<T> _parent;

        public AnimatorWrapper(InterpolatingAnimator<T> parent)
        {
            _parent = parent;
        }
        
        public override T Interpolate(double progress, T oldValue, T newValue) => _parent.Interpolate(progress, oldValue, newValue);
    }
}
