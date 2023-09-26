using System;
using Avalonia.Interactivity;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents spin directions that are valid.
    /// </summary>
    [Flags]
    public enum ValidSpinDirections
    {
        /// <summary>
        /// Can not increase nor decrease.
        /// </summary>
        None = 0,

        /// <summary>
        /// Can increase.
        /// </summary>
        Increase = 1,

        /// <summary>
        /// Can decrease.
        /// </summary>
        Decrease = 2
    }

    /// <summary>
    /// Represents spin directions that could be initiated by the end-user.
    /// </summary>
    public enum SpinDirection
    {
        /// <summary>
        /// Represents a spin initiated by the end-user in order to Increase a value.
        /// </summary>
        Increase = 0,

        /// <summary>
        /// Represents a spin initiated by the end-user in order to Decrease a value.
        /// </summary>
        Decrease = 1
    }

    /// <summary>
    /// Provides data for the Spinner.Spin event.
    /// </summary>
    public class SpinEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets the SpinDirection for the spin that has been initiated by the end-user.
        /// </summary>
        public SpinDirection Direction { get; }

        /// <summary>
        /// Get or set whether the spin event originated from a mouse wheel event.
        /// </summary>
        public bool UsingMouseWheel{ get; }

        /// <summary>
        /// Initializes a new instance of the SpinEventArgs class.
        /// </summary>
        /// <param name="direction">Spin direction.</param>
        public SpinEventArgs(SpinDirection direction)
        {
            Direction = direction;
        }

        public SpinEventArgs(RoutedEvent routedEvent, SpinDirection direction)
            : base(routedEvent)
        {
            Direction = direction;
        }

        public SpinEventArgs(SpinDirection direction, bool usingMouseWheel)
        {
            Direction = direction;
            UsingMouseWheel = usingMouseWheel;
        }

        public SpinEventArgs(RoutedEvent routedEvent, SpinDirection direction, bool usingMouseWheel)
            : base(routedEvent)
        {
            Direction = direction;
            UsingMouseWheel = usingMouseWheel;
        }
    }

    /// <summary>
    /// Base class for controls that represents controls that can spin.
    /// </summary>
    public abstract class Spinner : ContentControl
    {
        /// <summary>
        /// Defines the <see cref="ValidSpinDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<ValidSpinDirections> ValidSpinDirectionProperty =
            AvaloniaProperty.Register<Spinner, ValidSpinDirections>(nameof(ValidSpinDirection),
                ValidSpinDirections.Increase | ValidSpinDirections.Decrease);

        /// <summary>
        /// Defines the <see cref="Spin"/> event.
        /// </summary>
        public static readonly RoutedEvent<SpinEventArgs> SpinEvent =
            RoutedEvent.Register<Spinner, SpinEventArgs>(nameof(Spin), RoutingStrategies.Bubble);

        /// <summary>
        /// Initializes static members of the <see cref="Spinner"/> class.
        /// </summary>
        static Spinner()
        {
            ValidSpinDirectionProperty.Changed.Subscribe(OnValidSpinDirectionPropertyChanged);
        }

        /// <summary>
        /// Occurs when spinning is initiated by the end-user.
        /// </summary>
        public event EventHandler<SpinEventArgs>? Spin
        {
            add => AddHandler(SpinEvent, value);
            remove => RemoveHandler(SpinEvent, value);
        }

        /// <summary>
        /// Gets or sets <see cref="ValidSpinDirections"/> allowed for this control.
        /// </summary>
        public ValidSpinDirections ValidSpinDirection
        {
            get => GetValue(ValidSpinDirectionProperty);
            set => SetValue(ValidSpinDirectionProperty, value);
        }

        /// <summary>
        /// Called when valid spin direction changed.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        protected virtual void OnValidSpinDirectionChanged(ValidSpinDirections oldValue, ValidSpinDirections newValue)
        {
        }

        /// <summary>
        /// Raises the OnSpin event when spinning is initiated by the end-user.
        /// </summary>
        /// <param name="e">Spin event args.</param>
        protected virtual void OnSpin(SpinEventArgs e)
        {
            var valid = e.Direction == SpinDirection.Increase
                ? ValidSpinDirections.Increase
                : ValidSpinDirections.Decrease;

            //Only raise the event if spin is allowed.
            if ((ValidSpinDirection & valid) == valid)
            {
                RaiseEvent(e);
            }
        }

        /// <summary>
        /// Called when the <see cref="ValidSpinDirection"/> property value changed.
        /// </summary>
        /// <param name="e">The event args.</param>
        private static void OnValidSpinDirectionPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is Spinner spinner)
            {
                var oldValue = (ValidSpinDirections)e.OldValue!;
                var newValue = (ValidSpinDirections)e.NewValue!;
                spinner.OnValidSpinDirectionChanged(oldValue, newValue);
            }
        }
    }
}
