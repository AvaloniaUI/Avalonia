// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Metadata;
using System;

namespace Avalonia.Animation
{
    internal interface ITransition
    {
        /// <summary>
        /// Applies the transition to the specified <see cref="Animatable"/>.
        /// </summary>
        /// <param name="control"></param>
        void Apply(Animatable control);

        /// <summary>
        /// Gets the property to be animated.
        /// </summary>
        AvaloniaProperty Property { get; set; }
    }

    /// <summary>
    /// Defines how a property should be animated using a transition.
    /// </summary>
    public abstract class Transition<T> : ITransition
    {
        /// <summary>
        /// Gets the duration of the animation.
        /// </summary> 
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Instantiates the base abstract class <see cref="Transition{T}"/>.
        /// </summary>
        public Transition()
        {
            if(!(typeof(T) == Property.PropertyType))
            {
                throw new InvalidCastException
                    ($"Invalid property type {typeof(T).Name} for this {this.GetType().Name}");
            }
        }

        /// <summary>
        /// Gets the easing class to be used.
        /// </summary>
        public IEasing Easing { get; set; }

        /// <inheritdocs/>
        public abstract AvaloniaProperty Property { get; set; }

        /// <inheritdocs/>
        public abstract void Apply(Animatable control);

    }


}
