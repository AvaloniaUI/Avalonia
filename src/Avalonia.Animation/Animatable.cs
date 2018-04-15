// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Data;
using System;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Animation.Transitions;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for control which can have property transitions.
    /// </summary>
    public class Animatable : AvaloniaObject
    {
        /// <summary>
        /// Initializes this <see cref="Animatable"/> object.
        /// </summary>
        public Animatable()
        {
            Transitions = new Transitions.Transitions();
            AnimatableTimer = Timing.AnimationStateTimer
                                .Select(p =>
                                {
                                    if (PlayState == PlayState.Pause)
                                    {
                                        return PlayState.Pause;
                                    }
                                    else return p;
                                })
                                .Publish()
                                .RefCount();
        }

        /// <summary>
        /// The specific animations timer for this control.
        /// </summary>
        /// <returns></returns>
        public IObservable<PlayState> AnimatableTimer;

        /// <summary>
        /// Defines the <see cref="PlayState"/> property.
        /// </summary>
        public static readonly StyledProperty<PlayState> PlayStateProperty =
            AvaloniaProperty.Register<Animatable, PlayState>(nameof(PlayState), PlayState.Run);

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public PlayState PlayState
        {
            get { return GetValue(PlayStateProperty); }
            set { SetValue(PlayStateProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Transitions"/> property.
        /// </summary>  
        public static readonly StyledProperty<IEnumerable<ITransition>> TransitionsProperty =
            AvaloniaProperty.Register<Animatable, IEnumerable<ITransition>>(nameof(Transitions));

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public IEnumerable<ITransition> Transitions
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
