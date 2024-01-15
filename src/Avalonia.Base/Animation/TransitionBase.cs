using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Animation.Easings;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines how a property should be animated using a transition.
    /// </summary>
    public abstract class TransitionBase : AvaloniaObject, ITransition
    {
        /// <summary>
        /// Defines the <see cref="Duration"/> property.
        /// </summary>
        public static readonly DirectProperty<TransitionBase, TimeSpan> DurationProperty =
            AvaloniaProperty.RegisterDirect<TransitionBase, TimeSpan>(
                nameof(Duration),
                o => o._duration,
                (o, v) => o._duration = v);
        
        /// <summary>
        /// Defines the <see cref="Delay"/> property.
        /// </summary>
        public static readonly DirectProperty<TransitionBase, TimeSpan> DelayProperty =
            AvaloniaProperty.RegisterDirect<TransitionBase, TimeSpan>(
                nameof(Delay),
                o => o._delay,
                (o, v) => o._delay = v);
        
        /// <summary>
        /// Defines the <see cref="Easing"/> property.
        /// </summary>
        public static readonly DirectProperty<TransitionBase, Easing> EasingProperty =
            AvaloniaProperty.RegisterDirect<TransitionBase, Easing>(
                nameof(Easing),
                o => o._easing,
                (o, v) => o._easing = v);
        
        /// <summary>
        /// Defines the <see cref="Property"/> property.
        /// </summary>
        public static readonly DirectProperty<TransitionBase, AvaloniaProperty?> PropertyProperty =
            AvaloniaProperty.RegisterDirect<TransitionBase, AvaloniaProperty?>(
                nameof(Property),
                o => o._prop,
                (o, v) => o._prop = v);

        private TimeSpan _duration;
        private TimeSpan _delay = TimeSpan.Zero;
        private Easing _easing = new LinearEasing();
        private AvaloniaProperty? _prop;

        /// <summary>
        /// Gets or sets the duration of the transition.
        /// </summary> 
        public TimeSpan Duration
        {
            get { return _duration; }
            set { SetAndRaise(DurationProperty, ref _duration, value); }
        }

        /// <summary>
        /// Gets or sets delay before starting the transition.
        /// </summary> 
        public TimeSpan Delay
        {
            get { return _delay; }
            set { SetAndRaise(DelayProperty, ref _delay, value); }
        }

        /// <summary>
        /// Gets the easing class to be used.
        /// </summary>
        public Easing Easing
        {
            get { return _easing; }
            set { SetAndRaise(EasingProperty, ref _easing, value); }
        }

        /// <inheritdoc/>
        [DisallowNull]
        public AvaloniaProperty? Property
        {
            get { return _prop; }
            set { SetAndRaise(PropertyProperty, ref _prop, value); }
        }

        AvaloniaProperty ITransition.Property
        {
            get => Property ?? throw new InvalidOperationException("Transition has no property specified.");
            set => Property = value;
        }

        /// <inheritdoc/>
        IDisposable ITransition.Apply(Animatable control, IClock clock, object? oldValue, object? newValue)
            => Apply(control, clock, oldValue, newValue);
        
        internal abstract IDisposable Apply(Animatable control, IClock clock, object? oldValue, object? newValue);
    }
}
