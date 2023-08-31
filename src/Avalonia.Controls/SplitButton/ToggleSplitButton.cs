using System;

using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// A button with primary and secondary parts that can each be pressed separately.
    /// The primary part behaves like a <see cref="ToggleButton"/> with two states and
    /// the secondary part opens a flyout.
    /// </summary>
    [PseudoClasses(pcChecked)]
    public class ToggleSplitButton : SplitButton
    {
        /// <summary>
        /// Raised when the <see cref="IsChecked"/> property value changes.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? IsCheckedChanged
        {
            add => AddHandler(IsCheckedChangedEvent, value);
            remove => RemoveHandler(IsCheckedChangedEvent, value);
        }

        /// <summary>
        /// Defines the <see cref="IsCheckedChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IsCheckedChangedEvent =
            RoutedEvent.Register<ToggleSplitButton, RoutedEventArgs>(
                nameof(IsCheckedChanged),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="IsChecked"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsCheckedProperty =
            AvaloniaProperty.Register<ToggleSplitButton, bool>(nameof(IsChecked), false, defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleSplitButton"/> class.
        /// </summary>
        public ToggleSplitButton()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="ToggleSplitButton"/> is checked.
        /// </summary>
        public bool IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        /// <inheritdoc/>
        internal override bool InternalIsChecked => IsChecked;

        /// <inheritdoc/>
        /// <remarks>
        /// Both <see cref="ToggleSplitButton"/> and <see cref="SplitButton"/> share
        /// the same exact default style.
        /// </remarks>
        protected override Type StyleKeyOverride => typeof(SplitButton);

        /// <summary>
        /// Toggles the <see cref="IsChecked"/> property between true and false.
        /// </summary>
        protected void Toggle()
        {
            SetCurrentValue(IsCheckedProperty, !IsChecked);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == IsCheckedProperty)
            {
                OnIsCheckedChanged();
            }
        }

        /// <summary>
        /// Invokes the <see cref="IsCheckedChanged"/> event when the <see cref="IsChecked"/>
        /// property changes.
        /// </summary>
        protected virtual void OnIsCheckedChanged()
        {
            // IsLoaded check
            if (Parent is not null)
            {
                var eventArgs = new RoutedEventArgs(IsCheckedChangedEvent);
                RaiseEvent(eventArgs);
            }

            UpdatePseudoClasses();
        }

        /// <inheritdoc/>
        protected override void OnClickPrimary(RoutedEventArgs? e)
        {
            Toggle();

            base.OnClickPrimary(e);
        }
    }
}
