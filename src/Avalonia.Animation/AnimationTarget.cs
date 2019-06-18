// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Data.Core;

namespace Avalonia.Animation
{
    /// <summary>
    /// Stores the target of the animation based from Property path info
    /// </summary>
    public class AnimationTarget : IEquatable<AnimationTarget>
    {
        public AnimationTarget(Animatable root, AvaloniaProperty property)
        {
            RootAnimatable = root;
            TargetAnimatable = root;
            TargetProperty = property;
        }

        public Animatable RootAnimatable { get; internal set; }

        /// <summary>
        /// The target property of the animation.
        /// </summary>
        public AvaloniaProperty TargetProperty { get; internal set; }

        /// <summary>
        /// The target object of the animation.
        /// </summary>
        public Animatable TargetAnimatable { get; internal set; }

        /// <summary>
        /// Used as a store of the targeted object in a property path
        /// when it targets an object that is not <see cref="Animatable"/>.
        /// Mostly used for custom animators that doesn't map one-to-one type wise
        /// see SolidColorBrushAnimator for an example.
        /// </summary>
        public object TargetObject { get; internal set; }


        public bool Equals(AnimationTarget other)
        {
            if (TargetProperty == null || TargetAnimatable == null ||
                other.TargetProperty == null || other.TargetAnimatable == null)
            {
                return false;
            }

            return ((IPropertyInfo)this.TargetProperty).Equals((IPropertyInfo)other.TargetProperty) &&
                   this.TargetAnimatable.Equals(other.TargetAnimatable);
        }
    }
}
