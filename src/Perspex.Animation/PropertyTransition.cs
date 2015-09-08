





namespace Perspex.Animation
{
    using System;

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
        public PropertyTransition(PerspexProperty property, TimeSpan duration, IEasing easing)
        {
            this.Property = property;
            this.Duration = duration;
            this.Easing = easing;
        }

        /// <summary>
        /// Gets the property to be animated.
        /// </summary>
        /// <value>
        /// The property to be animated.
        /// </value>
        public PerspexProperty Property { get; }

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
