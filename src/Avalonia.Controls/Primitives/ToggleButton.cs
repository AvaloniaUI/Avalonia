using System;
using System.ComponentModel;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a control that a user can select (check) or clear (uncheck). Base class for controls that can switch states.
    /// </summary>
    [PseudoClasses(":checked", ":unchecked", ":indeterminate")]
    public class ToggleButton : Button
    {
        /// <summary>
        /// Defines the <see cref="IsChecked"/> property.
        /// </summary>
        public static readonly StyledProperty<bool?> IsCheckedProperty =
            AvaloniaProperty.Register<ToggleButton, bool?>(nameof(IsChecked), false,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="IsThreeState"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsThreeStateProperty =
            AvaloniaProperty.Register<ToggleButton, bool>(nameof(IsThreeState));

        /// <summary>
        /// Defines the <see cref="Checked"/> event.
        /// </summary>
        [Obsolete("Use IsCheckedChangedEvent instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly RoutedEvent<RoutedEventArgs> CheckedEvent =
            RoutedEvent.Register<ToggleButton, RoutedEventArgs>(
                nameof(Checked),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Unchecked"/> event.
        /// </summary>
        [Obsolete("Use IsCheckedChangedEvent instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly RoutedEvent<RoutedEventArgs> UncheckedEvent =
            RoutedEvent.Register<ToggleButton, RoutedEventArgs>(
                nameof(Unchecked),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="Unchecked"/> event.
        /// </summary>
        [Obsolete("Use IsCheckedChangedEvent instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly RoutedEvent<RoutedEventArgs> IndeterminateEvent =
            RoutedEvent.Register<ToggleButton, RoutedEventArgs>(
                nameof(Indeterminate),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="IsCheckedChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IsCheckedChangedEvent =
            RoutedEvent.Register<ToggleButton, RoutedEventArgs>(
                nameof(IsCheckedChanged),
                RoutingStrategies.Bubble);

        static ToggleButton()
        {
        }

        public ToggleButton()
        {
            UpdatePseudoClasses(IsChecked);
        }

        /// <summary>
        /// Raised when a <see cref="ToggleButton"/> is checked.
        /// </summary>
        [Obsolete("Use IsCheckedChanged instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler<RoutedEventArgs>? Checked
        {
            add => AddHandler(CheckedEvent, value);
            remove => RemoveHandler(CheckedEvent, value);
        }

        /// <summary>
        /// Raised when a <see cref="ToggleButton"/> is unchecked.
        /// </summary>
        [Obsolete("Use IsCheckedChanged instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler<RoutedEventArgs>? Unchecked
        {
            add => AddHandler(UncheckedEvent, value);
            remove => RemoveHandler(UncheckedEvent, value);
        }

        /// <summary>
        /// Raised when a <see cref="ToggleButton"/> is neither checked nor unchecked.
        /// </summary>
        [Obsolete("Use IsCheckedChanged instead."), EditorBrowsable(EditorBrowsableState.Never)]
        public event EventHandler<RoutedEventArgs>? Indeterminate
        {
            add => AddHandler(IndeterminateEvent, value);
            remove => RemoveHandler(IndeterminateEvent, value);
        }

        /// <summary>
        /// Raised when the <see cref="IsChecked"/> property value changes.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? IsCheckedChanged
        {
            add => AddHandler(IsCheckedChangedEvent, value);
            remove => RemoveHandler(IsCheckedChangedEvent, value);
        }

        /// <summary>
        /// Gets or sets whether the <see cref="ToggleButton"/> is checked.
        /// </summary>
        public bool? IsChecked
        {
            get => GetValue(IsCheckedProperty);
            set => SetValue(IsCheckedProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the control supports three states.
        /// </summary>
        public bool IsThreeState
        {
            get => GetValue(IsThreeStateProperty);
            set => SetValue(IsThreeStateProperty, value);
        }

        protected override void OnClick()
        {
            Toggle();
            base.OnClick();
        }

        /// <summary>
        /// Toggles the <see cref="IsChecked"/> property.
        /// </summary>
        protected virtual void Toggle()
        {
            bool? newValue;
            if (IsChecked.HasValue)
            {
                if (IsChecked.Value)
                {
                    if (IsThreeState)
                    {
                        newValue = null;
                    }
                    else
                    {
                        newValue = false;
                    }
                }
                else
                {
                    newValue = true;
                }
            }
            else
            {
                newValue = false;
            }

            SetCurrentValue(IsCheckedProperty, newValue);
        }

        /// <summary>
        /// Called when <see cref="IsChecked"/> becomes true.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        [Obsolete("Use OnIsCheckedChanged instead."), EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual void OnChecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Called when <see cref="IsChecked"/> becomes false.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        [Obsolete("Use OnIsCheckedChanged instead."), EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual void OnUnchecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Called when <see cref="IsChecked"/> becomes null.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        [Obsolete("Use OnIsCheckedChanged instead."), EditorBrowsable(EditorBrowsableState.Never)]
        protected virtual void OnIndeterminate(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Called when <see cref="IsChecked"/> changes.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnIsCheckedChanged(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ToggleButtonAutomationPeer(this);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsCheckedProperty)
            {
                var newValue = change.GetNewValue<bool?>();

                UpdatePseudoClasses(newValue);

#pragma warning disable CS0618 // Type or member is obsolete
                switch (newValue)
                {
                    case true:
                        OnChecked(new RoutedEventArgs(CheckedEvent));
                        break;
                    case false:
                        OnUnchecked(new RoutedEventArgs(UncheckedEvent));
                        break;
                    default:
                        OnIndeterminate(new RoutedEventArgs(IndeterminateEvent));
                        break;
                }
#pragma warning restore CS0618 // Type or member is obsolete

                OnIsCheckedChanged(new RoutedEventArgs(IsCheckedChangedEvent));
            }
        }

        private void UpdatePseudoClasses(bool? isChecked)
        {
            PseudoClasses.Set(":checked", isChecked == true);
            PseudoClasses.Set(":unchecked", isChecked == false);
            PseudoClasses.Set(":indeterminate", isChecked == null);
        }
    }
}
