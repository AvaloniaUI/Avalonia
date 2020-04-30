using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Data;

#nullable enable

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for all animatable objects.
    /// </summary>
    public class Animatable : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="Clock"/> property.
        /// </summary>
        public static readonly StyledProperty<IClock> ClockProperty =
            AvaloniaProperty.Register<Animatable, IClock>(nameof(Clock), inherits: true);

        /// <summary>
        /// Defines the <see cref="Transitions"/> property.
        /// </summary>
        public static readonly DirectProperty<Animatable, Transitions> TransitionsProperty =
            AvaloniaProperty.RegisterDirect<Animatable, Transitions>(
                nameof(Transitions),
                o => o.Transitions,
                (o, v) => o.Transitions = v);

        private bool _transitionsEnabled;
        private Transitions? _transitions;
        private Dictionary<ITransition, TransitionState>? _transitionState;

        /// <summary>
        /// Gets or sets the clock which controls the animations on the control.
        /// </summary>
        public IClock Clock
        {
            get => GetValue(ClockProperty);
            set => SetValue(ClockProperty, value);
        }

        /// <summary>
        /// Gets or sets the property transitions for the control.
        /// </summary>
        public Transitions Transitions
        {
            get
            {
                if (_transitions is null)
                {
                    _transitions = new Transitions();
                    _transitions.CollectionChanged += TransitionsCollectionChanged;
                }

                return _transitions;
            }
            set
            {
                // TODO: This is a hack, Setter should not replace transitions, but should add/remove.
                if (value is null)
                {
                    return;
                }

                if (_transitions is object)
                {
                    RemoveTransitions(_transitions);
                    _transitions.CollectionChanged -= TransitionsCollectionChanged;
                }

                SetAndRaise(TransitionsProperty, ref _transitions, value);
                _transitions.CollectionChanged += TransitionsCollectionChanged;
                AddTransitions(_transitions);
            }
        }

        /// <summary>
        /// Enables transitions for the control.
        /// </summary>
        /// <remarks>
        /// This method should not be called from user code, it will be called automatically by the framework
        /// when a control is added to the visual tree.
        /// </remarks>
        protected void EnableTransitions()
        {
            if (!_transitionsEnabled)
            {
                _transitionsEnabled = true;

                if (_transitions is object)
                {
                    AddTransitions(_transitions);
                }
            }
        }

        /// <summary>
        /// Disables transitions for the control.
        /// </summary>
        /// <remarks>
        /// This method should not be called from user code, it will be called automatically by the framework
        /// when a control is added to the visual tree.
        /// </remarks>
        protected void DisableTransitions()
        {
            if (_transitionsEnabled)
            {
                _transitionsEnabled = false;

                if (_transitions is object)
                {
                    RemoveTransitions(_transitions);
                }
            }
        }

        protected sealed override void OnPropertyChangedCore<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (_transitionsEnabled &&
                _transitions is object &&
                _transitionState is object &&
                change.Priority > BindingPriority.Animation)
            {
                foreach (var transition in _transitions)
                {
                    if (transition.Property == change.Property)
                    {
                        var state = _transitionState[transition];
                        var oldValue = state.BaseValue;
                        var newValue = GetAnimationBaseValue(transition.Property);

                        if (!Equals(oldValue, newValue))
                        {
                            state.BaseValue = newValue;

                            // We need to transition from the current animated value if present,
                            // instead of the old base value.
                            var animatedValue = GetValue(transition.Property);

                            if (!Equals(newValue, animatedValue))
                            {
                                oldValue = animatedValue;
                            }

                            state.Instance?.Dispose();
                            state.Instance = transition.Apply(
                                this,
                                Clock ?? AvaloniaLocator.Current.GetService<IGlobalClock>(),
                                oldValue,
                                newValue);
                            return;
                        }
                    }
                }
            }

            base.OnPropertyChangedCore(change);
        }

        private void TransitionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_transitionsEnabled)
            {
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddTransitions(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveTransitions(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveTransitions(e.OldItems);
                    AddTransitions(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException("Transitions collection cannot be reset.");
            }
        }

        private void AddTransitions(IList items)
        {
            if (!_transitionsEnabled)
            {
                return;
            }

            _transitionState ??= new Dictionary<ITransition, TransitionState>();

            for (var i = 0; i < items.Count; ++i)
            {
                var t = (ITransition)items[i];

                _transitionState.Add(t, new TransitionState
                {
                    BaseValue = GetAnimationBaseValue(t.Property),
                });
            }
        }

        private void RemoveTransitions(IList items)
        {
            if (_transitionState is null)
            {
                return;
            }

            for (var i = 0; i < items.Count; ++i)
            {
                var t = (ITransition)items[i];

                if (_transitionState.TryGetValue(t, out var state))
                {
                    state.Instance?.Dispose();
                    _transitionState.Remove(t);
                }
            }
        }

        private object GetAnimationBaseValue(AvaloniaProperty property)
        {
            var value = this.GetBaseValue(property, BindingPriority.LocalValue);

            if (value == AvaloniaProperty.UnsetValue)
            {
                value = GetValue(property);
            }

            return value;
        }

        private class TransitionState
        {
            public IDisposable? Instance { get; set; }
            public object? BaseValue { get; set; }
        }
    }
}
