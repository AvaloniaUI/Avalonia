// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines how a property should be animated using a transition.
    /// </summary>
    public class PropertyTransition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyTransition"/> class.
        /// </summary>
        /// <param name="property">The property to be animated/</param>
        /// <param name="duration">The duration of the animation.</param>
        /// <param name="easing">The easing function to use.</param>
        public PropertyTransition(AvaloniaProperty property, TimeSpan duration, IEasing easing)
        {
            Property = property;
            Duration = duration;
            Easing = easing;
        }

        /// <summary>
        /// Gets the property to be animated.
        /// </summary>
        /// <value>
        /// The property to be animated.
        /// </value>
        public AvaloniaProperty Property { get; }

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        /// <value>
        /// The duration of the animation.
        /// </value>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Gets the easing function used.
        /// </summary>
        /// <value>
        /// The easing function.
        /// </value>
        public IEasing Easing { get; }
    }
}
