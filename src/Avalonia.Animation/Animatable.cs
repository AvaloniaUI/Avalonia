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

        private int _disableCount;

        private Dictionary<AvaloniaProperty, IDisposable> _activeIterations;

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public Transitions Transitions
        {
            get
            {
                if (_transitions is null)
                    _transitions = new Transitions();

                if (_activeIterations is null)
                    _activeIterations = new Dictionary<AvaloniaProperty, IDisposable>();

                return _transitions;
            }
            set
            {
                if (value is null)
                    return;

                if (_activeIterations is null)
                    _activeIterations = new Dictionary<AvaloniaProperty, IDisposable>();

                SetAndRaise(TransitionsProperty, ref _transitions, value);
            }
        }

        internal protected void EnableTransitions() => _disableCount--;
        internal protected void DisableTransitions()
        {
            _disableCount++;

            if (_disableCount > 0 && (_activeIterations?.Count ?? 0) > 0)
            {
                foreach (var iterationKP in _activeIterations.ToList())
                {
                    iterationKP.Value?.Dispose();
                }

                _activeIterations.Clear();
            }
        }

        /// <summary>
        /// Reacts to a change in a <see cref="AvaloniaProperty"/> value in 
        /// order to animate the change if a <see cref="ITransition"/> is set for the property.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_transitions is null ||
                _activeIterations is null ||
                e.Priority == BindingPriority.Animation)
                return;

            // PERF-SENSITIVE: Called on every property change. Don't use LINQ here (too many allocations).
            foreach (var transition in _transitions)
            {
                if (transition.Property == e.Property)
                {

                    if (_activeIterations.TryGetValue(e.Property, out var dispose))
                        dispose?.Dispose();

                    var clk = Clock ?? Avalonia.Animation.Clock.GlobalClock;
                    var instance = transition.Apply(this, clk, e.OldValue, e.NewValue);

                    _activeIterations[e.Property] = instance;

                    return;
                }
            }
        }
    }
}
