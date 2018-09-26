// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia.Animation.Easings;
using Avalonia.Animation.Utils;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines how a property should be animated using a transition.
    /// </summary>
    public abstract class Transition<T> : AvaloniaObject, ITransition
    {
        private AvaloniaProperty _prop;

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary> 
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets the easing class to be used.
        /// </summary>
        public Easing Easing { get; set; } = new LinearEasing();

        /// <inheritdocs/>
        public AvaloniaProperty Property
        {
            get
            {
                return _prop;
            }
            set
            {
                if (!(value.PropertyType.IsAssignableFrom(typeof(T))))
                    throw new InvalidCastException
                        ($"Invalid property type \"{typeof(T).Name}\" for this transition: {GetType().Name}.");

                _prop = value;
            }
        }

        /// <summary>
        /// Apply interpolation to the property.
        /// </summary>
        public abstract IObservable<T> DoTransition(IObservable<double> progress, T oldValue, T newValue);

        /// <inheritdocs/>
        public virtual IDisposable Apply(Animatable control, IClock clock, object oldValue, object newValue)
        {
            var transition = DoTransition(new TransitionInstance(clock, Duration), (T)oldValue, (T)newValue);
            return control.Bind<T>((AvaloniaProperty<T>)Property, transition, Data.BindingPriority.Animation);
        }
    }
}
