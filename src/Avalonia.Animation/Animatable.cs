// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Data;
using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for control which can have property transitions.
    /// </summary>
    public class Animatable : AvaloniaObject
    {

        private Transitions _transitions;

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public Transitions Transitions
        {
            get
            {
                return _transitions ?? (_transitions = new Transitions());
            }

            set
            {
                SetAndRaise(TransitionsProperty, ref _transitions, value);
            }
        }

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public static readonly DirectProperty<Animatable, Transitions> TransitionsProperty =
                AvaloniaProperty.RegisterDirect<Animatable, Transitions>(nameof(Transitions), o => o.Transitions);

        /// <summary>
        /// Reacts to a change in a <see cref="AvaloniaProperty"/> value in 
        /// order to animate the change if a <see cref="ITransition"/> 
        /// is set for the property.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Priority != BindingPriority.Animation && Transitions != null)
            {
                var match = Transitions.FirstOrDefault(x => x.Property == e.Property);


                if (match != null)
                {
                    
                    //    //BindAnimateProperty(this, e.Property, e.OldValue, e.NewValue, match.Easing, match.Duration);
                }
            }
        }

        /// <summary>
        /// Animates a <see cref="AvaloniaProperty"/>.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="property">The target property.</param>
        /// <param name="start">The value of the property at the start of the animation.</param>
        /// <param name="finish">The value of the property at the end of the animation.</param>
        /// <param name="easing">The easing function to use.</param>
        /// <param name="duration">The duration of the animation.</param>
        /// <returns>An <see cref="Animation"/> that can be used to track or stop the animation.</returns>
        //public static Animation BindAnimateProperty(
        //    IAvaloniaObject target,
        //    AvaloniaProperty property,
        //    object start,
        //    object finish,
        //    IEasing easing,
        //    TimeSpan duration)
        //{
        //    var k = start.GetType();
        //    if (k == typeof(double))
        //    {
        //        var o = Timing.GetTimer(duration).Select(progress => easing.Ease(progress, start, finish));
        //        return new Animation(o, target.Bind(property, o, BindingPriority.Animation));
        //    }
        //    else
        //        return null;
        //}

        ///// <summary>
        ///// Animates a <see cref="AvaloniaProperty"/>.
        ///// </summary>
        ///// <typeparam name="T">The property type.</typeparam>
        ///// <param name="target">The target object.</param>
        ///// <param name="property">The target property.</param>
        ///// <param name="start">The value of the property at the start of the animation.</param>
        ///// <param name="finish">The value of the property at the end of the animation.</param>
        ///// <param name="easing">The easing function to use.</param>
        ///// <param name="duration">The duration of the animation.</param>
        ///// <returns>An <see cref="Animation"/> that can be used to track or stop the animation.</returns>
        //public static Animation<T> Property<T>(
        //    IAvaloniaObject target,
        //    AvaloniaProperty<T> property,
        //    T start,
        //    T finish,
        //    IEasing<T> easing,
        //    TimeSpan duration)
        //{
        //    var o = Timing.GetTimer(duration).Select(progress => easing.Ease(progress, start, finish));
        //    return new Animation<T>(o, target.Bind(property, o, BindingPriority.Animation));
        //}

    }
}
