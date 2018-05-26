using System;

namespace Avalonia.Animation
{
    /// <summary>
    /// Attribute for <see cref="IAnimationSetter"/> objects
    /// that maps the setter to it's <see cref="Animator{T}"/>.
    /// </summary>
    public class AnimatorAttribute : Attribute
    {
        public Type HandlerType;

        public AnimatorAttribute(Type handler)
        {
            this.HandlerType = handler;
        }
    }
}