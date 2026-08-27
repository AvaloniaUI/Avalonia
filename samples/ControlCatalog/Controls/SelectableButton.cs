using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace ControlCatalog.Controls
{
    [PseudoClasses(":selected")]
    public class SelectableButton : Button, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.Register<SelectableButton, bool>(nameof(IsSelected), false,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="IsSelectedChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> IsSelectedChangedEvent =
            RoutedEvent.Register<SelectableButton, RoutedEventArgs>(
                nameof(IsSelectedChanged),
                RoutingStrategies.Bubble);

        public SelectableButton()
        {
            UpdatePseudoClasses(IsSelected);
        }

        /// <summary>
        /// Gets or sets whether the <see cref="SelectableButton"/> is selected.
        /// </summary>
        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Raised when the <see cref="IsSelected"/> property value changes.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? IsSelectedChanged
        {
            add => AddHandler(IsSelectedChangedEvent, value);
            remove => RemoveHandler(IsSelectedChangedEvent, value);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsSelectedProperty)
            {
                var newValue = change.GetNewValue<bool>();
                UpdatePseudoClasses(newValue);
                OnIsSelectedChanged(new RoutedEventArgs(IsSelectedChangedEvent));
            }
        }

        private void UpdatePseudoClasses(bool isSelected)
        {
            PseudoClasses.Set(":selected", isSelected);
        }

        /// <summary>
        /// Called when <see cref="IsSelected"/> changes.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnIsSelectedChanged(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }
    }
}
