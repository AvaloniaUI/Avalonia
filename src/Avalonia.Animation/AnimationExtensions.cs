// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines animation extension methods.
    /// </summary>
    public static class AnimationExtensions
    {
        ///// <summary>
        ///// Returns a new <see cref="Avalonia.Animation.Transition"/> for the specified
        ///// <see cref="AvaloniaProperty"/> using linear easing.
        ///// </summary>
        ///// <typeparam name="T">The type of the <see cref="AvaloniaProperty"/>.</typeparam>
        ///// <param name="property">The property to animate.</param>
        ///// <param name="milliseconds">The animation duration in milliseconds.</param>
        ///// <returns>
        ///// A <see cref="Avalonia.Animation.Transition"/> that can be added to the
        ///// <see cref="Animatable.Transitions"/> collection.
        ///// </returns>
        //public static Transition Transition<T>(this AvaloniaProperty<T> property, int milliseconds)
        //{
        //    return Transition(property, TimeSpan.FromMilliseconds(milliseconds));
        //}

        ///// <summary>
        ///// Returns a new <see cref="Avalonia.Animation.Transition"/> for the specified
        ///// <see cref="AvaloniaProperty"/> using linear easing.
        ///// </summary>
        ///// <typeparam name="T">The type of the <see cref="AvaloniaProperty"/>.</typeparam>
        ///// <param name="property">The property to animate.</param>
        ///// <param name="duration">The animation duration.</param>
        ///// <returns>
        ///// A <see cref="Avalonia.Animation.Transition"/> that can be added to the
        ///// <see cref="Animatable.Transitions"/> collection.
        ///// </returns>
        //public static Transition Transition<T>(this AvaloniaProperty<T> property, TimeSpan duration)
        //{
        //    return new Transition(property, duration, LinearEasing.For<T>());
        //}
    }
}
