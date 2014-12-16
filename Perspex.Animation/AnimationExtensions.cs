// -----------------------------------------------------------------------
// <copyright file="PropertyTransition.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;

    public static class AnimationExtensions
    {
        public static PropertyTransition Transition<T>(this PerspexProperty<T> property, int milliseconds)
        {
            return Transition(property, TimeSpan.FromMilliseconds(milliseconds));
        }

        public static PropertyTransition Transition<T>(this PerspexProperty<T> property, TimeSpan duration)
        {
            return new PropertyTransition
            {
                Property = property,
                Duration = duration,
                Easing = LinearEasing.For<T>(),
            };
        }
    }
}
