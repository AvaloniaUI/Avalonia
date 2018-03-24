// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Data;
using System;
using System.Reactive.Linq;
using Avalonia.Collections;

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for control which can have property transitions.
    /// </summary>
    public class Animatable : AvaloniaObject
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<ITransition>> TransitionsProperty =
                AvaloniaProperty.Register<Animatable, AvaloniaList<ITransition>>(nameof(Transitions));

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public AvaloniaList<ITransition> Transitions
        {
            get { return GetValue(TransitionsProperty); }
            set { SetValue(TransitionsProperty, value); }
        }

        /// <summary>
        /// Reacts to a change in a <see cref="AvaloniaProperty"/> value in 
        /// order to animate the change if a <see cref="ITransition"/> is set for the property.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Priority != BindingPriority.Animation && Transitions != null)
            {
                var match = Transitions.FirstOrDefault(x => x.Property == e.Property);

                if (match != null)
                {
                    match.Apply(this, e.OldValue, e.NewValue);
                }
            }
        }

    }
}
