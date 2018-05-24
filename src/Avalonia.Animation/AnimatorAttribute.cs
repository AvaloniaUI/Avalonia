using System;

namespace Avalonia.Animation
{
    public class AnimatorAttribute : Attribute
    {
        public Type HandlerType;

        public AnimatorAttribute(Type handler)
        {
            this.HandlerType = handler;
        }
    }
}