using System;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Utilities;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Base class for controls that display a value within a range.
    /// </summary>
    public abstract class RangeBase : TemplatedControl
    {
        private bool _isDataContextChanging;
        
        /// <summary>
        /// Defines the <see cref="Minimum"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(Minimum), coerce: CoerceMinimum);

        /// <summary>
        /// Defines the <see cref="Maximum"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(Maximum), 100, coerce: CoerceMaximum);

        /// <summary>
        /// Defines the <see cref="Value"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(Value),
                defaultBindingMode: BindingMode.TwoWay,
                coerce: CoerceValue);

        /// <summary>
        /// Defines the <see cref="SmallChange"/> property.
        /// </summary>
        public static readonly StyledProperty<double> SmallChangeProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(SmallChange), 1);

        /// <summary>
        /// Defines the <see cref="LargeChange"/> property.
        /// </summary>
        public static readonly StyledProperty<double> LargeChangeProperty =
            AvaloniaProperty.Register<RangeBase, double>(nameof(LargeChange), 10);

        /// <summary>
        /// Defines the <see cref="ValueChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RangeBaseValueChangedEventArgs> ValueChangedEvent =
            RoutedEvent.Register<RangeBase, RangeBaseValueChangedEventArgs>(
                nameof(ValueChanged), RoutingStrategies.Bubble);

        /// <summary>
        /// Occurs when the <see cref="Value"/> property changes.
        /// </summary>
        public event EventHandler<RangeBaseValueChangedEventArgs>? ValueChanged
        {
            add => AddHandler(ValueChangedEvent, value);
            remove => RemoveHandler(ValueChangedEvent, value);
        }

        /// <summary>
        /// Gets or sets the minimum possible value.
        /// </summary>
        public double Minimum
        {
            get => GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        private static double CoerceMinimum(AvaloniaObject sender, double value)
        {
            return ValidateDouble(value) ? value : sender.GetValue(MinimumProperty);
        }

        private void OnMinimumChanged()
        {
            if (IsInitialized && !_isDataContextChanging)
            {
                CoerceValue(MaximumProperty);
                CoerceValue(ValueProperty);
            }
        }

        /// <summary>
        /// Gets or sets the maximum possible value.
        /// </summary>
        public double Maximum
        {
            get => GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        private static double CoerceMaximum(AvaloniaObject sender, double value)
        {
            return ValidateDouble(value)
                ? Math.Max(value, sender.GetValue(MinimumProperty))
                : sender.GetValue(MaximumProperty);
        }

        private void OnMaximumChanged()
        {
            if (IsInitialized && !_isDataContextChanging)
            {
                CoerceValue(MinimumProperty);
                CoerceValue(ValueProperty);
            }
        }

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static double CoerceValue(AvaloniaObject sender, double value)
        {
            return ValidateDouble(value)
                ? MathUtilities.Clamp(value, sender.GetValue(MinimumProperty), sender.GetValue(MaximumProperty))
                : sender.GetValue(ValueProperty);
        }

        /// <summary>
        /// Gets or sets the small increment value added or subtracted from the <see cref="Value"/>.
        /// </summary>
        public double SmallChange
        {
            get => GetValue(SmallChangeProperty);
            set => SetValue(SmallChangeProperty, value);
        }

        /// <summary>
        /// Gets or sets the large increment value added or subtracted from the <see cref="Value"/>.
        /// </summary>
        public double LargeChange
        {
            get => GetValue(LargeChangeProperty);
            set => SetValue(LargeChangeProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            CoerceValue(MaximumProperty);
            CoerceValue(ValueProperty);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == MinimumProperty)
            {
                OnMinimumChanged();
            }
            else if (change.Property == MaximumProperty)
            {
                OnMaximumChanged();
            }
            else if (change.Property == ValueProperty)
            {
                var valueChangedEventArgs = new RangeBaseValueChangedEventArgs(
                    change.GetOldValue<double>(),
                    change.GetNewValue<double>(),
                    ValueChangedEvent);
                RaiseEvent(valueChangedEventArgs);
            }
        }
        
        /// <inheritdoc />
        protected override void OnDataContextBeginUpdate()
        {
            _isDataContextChanging = true;
            base.OnDataContextBeginUpdate();
        }

        /// <inheritdoc />
        protected override void OnDataContextEndUpdate()
        {
            base.OnDataContextEndUpdate();
            _isDataContextChanging = false;
        }

        /// <summary>
        /// Checks if the double value is not infinity nor NaN.
        /// </summary>
        /// <param name="value">The value.</param>
        private static bool ValidateDouble(double value)
        {
            return !double.IsInfinity(value) && !double.IsNaN(value);
        }
    }
}
