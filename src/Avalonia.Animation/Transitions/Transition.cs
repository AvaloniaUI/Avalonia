// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Metadata;
using System;
using System.Reactive.Linq;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines how a property should be animated using a transition.
    /// </summary>
    public abstract class Transition<T> : ITransition
    {
        /// <summary>
        /// Gets the duration of the animation.
        /// </summary> 
        public TimeSpan Duration { get; set; }
 
        /// <summary>
        /// Gets the easing class to be used.
        /// </summary>
        public IEasing Easing { get; set; }

        private AvaloniaProperty _prop;

        /// <inheritdocs/>
        public AvaloniaProperty Property
        {
            get
            {
                return _prop;
            }
            set
            {
                if (!(typeof(T) == value.PropertyType))                
                    throw new InvalidCastException
                        ($"Invalid property type \"{typeof(T).Name}\" for this {GetType().Name} transition.");

                _prop = value;
            }
        }

        /// <summary>
        /// Apply interpolation to the property.
        /// </summary>
        public abstract IObservable<T> DoInterpolation(IObservable<double> progress, T oldValue, T newValue);

        /// <inheritdocs/>
        public IDisposable Apply(Animatable control, object oldValue, object newValue)
        {
            var transition = DoInterpolation(Timing.GetTimer(Duration), (T)oldValue, (T)newValue).Select(p => (object)p);
            return control.Bind(Property, transition, Data.BindingPriority.Animation);
        }

    }


}
