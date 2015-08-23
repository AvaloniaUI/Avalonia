// -----------------------------------------------------------------------
// <copyright file="Animatable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System.Linq;

    /// <summary>
    /// Base class for control which can have property transitions.
    /// </summary>
    public class Animatable : PerspexObject
    {
        /// <summary>
        /// The property transitions for the control.
        /// </summary>
        private PropertyTransitions propertyTransitions;

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        /// <value>
        /// The property transitions for the control.
        /// </value>
        public PropertyTransitions PropertyTransitions
        {
            get
            {
                if (this.propertyTransitions == null)
                {
                    this.propertyTransitions = new PropertyTransitions();
                }

                return this.propertyTransitions;
            }

            set
            {
                this.propertyTransitions = value;
            }
        }

        /// <summary>
        /// Reacts to a change in a <see cref="PerspexProperty"/> value in order to animate the
        /// change if a <see cref="PropertyTransition"/> is set for the property..
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPropertyChanged(PerspexPropertyChangedEventArgs e)
        {
            if (e.Priority != BindingPriority.Animation && this.propertyTransitions != null)
            {
                var match = this.propertyTransitions.FirstOrDefault(x => x.Property == e.Property);

                if (match != null)
                {
                    Animate.Property(this, e.Property, e.OldValue, e.NewValue, match.Easing, match.Duration);
                }
            }
        }
    }
}
