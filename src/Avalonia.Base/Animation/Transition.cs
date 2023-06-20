using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Animation.Easings;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines how a property should be animated using a transition.
    /// </summary>
    public abstract class Transition<T> : AvaloniaObject, ITransition
    {
        private AvaloniaProperty? _prop;

        /// <summary>
        /// Gets or sets the duration of the transition.
        /// </summary> 
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets delay before starting the transition.
        /// </summary> 
        public TimeSpan Delay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Gets the easing class to be used.
        /// </summary>
        public Easing Easing { get; set; } = new LinearEasing();

        /// <inheritdocs/>
        [DisallowNull]
        public AvaloniaProperty? Property
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

        AvaloniaProperty ITransition.Property
        {
            get => Property ?? throw new InvalidOperationException("Transition has no property specified.");
            set => Property = value;
        }

        /// <summary>
        /// Apply interpolation to the property.
        /// </summary>
        internal abstract IObservable<T> DoTransition(IObservable<double> progress, T oldValue, T newValue);

        /// <inheritdocs/>
        IDisposable ITransition.Apply(Animatable control, IClock clock, object? oldValue, object? newValue)
            => Apply(control, clock, oldValue, newValue);
        
        internal virtual IDisposable Apply(Animatable control, IClock clock, object? oldValue, object? newValue)
        {
            if (Property is null)
                throw new InvalidOperationException("Transition has no property specified.");

            var transition = DoTransition(new TransitionInstance(clock, Delay, Duration), (T)oldValue!, (T)newValue!);
            return control.Bind<T>((AvaloniaProperty<T>)Property, transition, Data.BindingPriority.Animation);
        }
    }
}
