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

        private Dictionary<AvaloniaProperty, IDisposable> _previousTransitions;

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public Transitions Transitions
        {
            get
            {
                if (_transitions is null)
                    _transitions = new Transitions();

                if (_previousTransitions is null)
                    _previousTransitions = new Dictionary<AvaloniaProperty, IDisposable>();

                return _transitions;
            }
            set
            {
                if (value is null)
                    return;

                if (_previousTransitions is null)
                    _previousTransitions = new Dictionary<AvaloniaProperty, IDisposable>();

                SetAndRaise(TransitionsProperty, ref _transitions, value);
            }
        }

        protected override void OnPropertyChanged<T>(
            AvaloniaProperty<T> property,
            Optional<T> oldValue,
            BindingValue<T> newValue,
            BindingPriority priority)
        {
            if (_transitions is null || _previousTransitions is null || priority == BindingPriority.Animation)
                return;

            // PERF-SENSITIVE: Called on every property change. Don't use LINQ here (too many allocations).
            foreach (var transition in _transitions)
            {
                if (transition.Property == property)
                {
                    if (_previousTransitions.TryGetValue(property, out var dispose))
                        dispose.Dispose();

                    var instance = transition.Apply(
                        this,
                        Clock ?? Avalonia.Animation.Clock.GlobalClock,
                        oldValue.GetValueOrDefault(),
                        newValue.GetValueOrDefault());

                    _previousTransitions[property] = instance;
                    return;
                }
            }
        }
    }
}
