// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for control which can have property transitions.
    /// </summary>
    public class Animatable : AvaloniaObject
    {
        /// <summary>
        /// The property transitions for the control.
        /// </summary>
        private PropertyTransitions _propertyTransitions;

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
                return _propertyTransitions ?? (_propertyTransitions = new PropertyTransitions());
            }

            set
            {
                _propertyTransitions = value;
            }
        }

        /// <summary>
        /// Reacts to a change in a <see cref="AvaloniaProperty"/> value in order to animate the
        /// change if a <see cref="PropertyTransition"/> is set for the property..
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Priority != BindingPriority.Animation && _propertyTransitions != null)
            {
                var match = _propertyTransitions.FirstOrDefault(x => x.Property == e.Property);

                if (match != null)
                {
                    Animate.Property(this, e.Property, e.OldValue, e.NewValue, match.Easing, match.Duration);
                }
            }
        }
    }
}
