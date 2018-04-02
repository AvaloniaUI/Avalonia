// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Data;
using System;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Animation.Transitions;

namespace Avalonia.Animation
{
    /// <summary>
    /// 
    /// </summary>
    public enum AnimationPlayState
    {
        /// <summary>
        /// 
        /// </summary>
        Running,

        /// <summary>
        /// 
        /// </summary>
        Paused
    }

    /// <summary>
    /// Base class for control which can have property transitions.
    /// </summary>
    public class Animatable : AvaloniaObject
    {

        /// <summary>
        /// Defines the <see cref="AnimationPlayState"/> property.
        /// </summary>
        public static readonly StyledProperty<AnimationPlayState> AnimationPlayStateProperty =
                AvaloniaProperty.Register<Animatable, AnimationPlayState>(nameof(AnimationPlayState));

        /// <summary>
        /// Gets or sets the state of the animation for this
        /// control.
        /// </summary>
        public AnimationPlayState AnimationPlayState
        {
            get { return GetValue(AnimationPlayStateProperty); }
            set { SetValue(AnimationPlayStateProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Transitions"/> property.
        /// </summary>
        public static readonly StyledProperty<Transitions.Transitions> TransitionsProperty =
                AvaloniaProperty.Register<Animatable, Transitions.Transitions>(nameof(Transitions));

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public Transitions.Transitions Transitions
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
