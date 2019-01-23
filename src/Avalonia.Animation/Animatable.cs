// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Data;
using Avalonia.Animation.Animators; 

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for all animatable objects.
    /// </summary>
    public class Animatable : AvaloniaObject
    {
        public static readonly StyledProperty<IClock> ClockProperty =
            AvaloniaProperty.Register<Animatable, IClock>(nameof(Clock), inherits: true);

        public IClock Clock
        {
            get => GetValue(ClockProperty);
            set => SetValue(ClockProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="Transitions"/> property.
        /// </summary>
        public static readonly DirectProperty<Animatable, Transitions> TransitionsProperty =
            AvaloniaProperty.RegisterDirect<Animatable, Transitions>(
                nameof(Transitions),
                o => o.Transitions,
                (o, v) => o.Transitions = v);

        private Transitions _transitions;

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public Transitions Transitions
        {
            get { return _transitions ?? (_transitions = new Transitions()); }
            set { SetAndRaise(TransitionsProperty, ref _transitions, value); }
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
                    match.Apply(this, Clock ?? Avalonia.Animation.Clock.GlobalClock, e.OldValue, e.NewValue);
                }
            }
        }
    }
}
