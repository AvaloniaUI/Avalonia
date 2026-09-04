using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace ControlCatalog.Controls
{
    public class HomeItemExpander : Expander, ISelectable
    {
        /// <summary>
        /// Defines the <see cref="Command"/> property.
        /// </summary>
        public static readonly StyledProperty<ICommand?> CommandProperty =
            Button.CommandProperty.AddOwner<HomeItemExpander>();

        /// <summary>
        /// Defines the <see cref="CommandParameter"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<HomeItemExpander>();

        /// <summary>
        /// Defines the <see cref="CanExpand"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> CanExpandProperty =
            AvaloniaProperty.Register<HomeItemExpander, bool>(
                nameof(CanExpand),
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="CanExpand"/> property.
        /// </summary>
        public static readonly DirectProperty<HomeItemExpander, bool> IsEffectivelyExpandedProperty =
            AvaloniaProperty.RegisterDirect<HomeItemExpander, bool>(
                nameof(IsEffectivelyExpanded),
                x => x.IsEffectivelyExpanded,
                (x, o) => x.IsEffectivelyExpanded = o,
                defaultBindingMode: BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="IsSelected"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty =
            SelectingItemsControl.IsSelectedProperty.AddOwner<HomeItemExpander>();

        /// <summary>
        /// Gets or sets an <see cref="ICommand"/> to be invoked when the button is clicked.
        /// </summary>
        public ICommand? Command
        {
            get => GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Gets or sets a parameter to be passed to the <see cref="Command"/>.
        /// </summary>
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="HomeItemExpander"/>
        /// content area is open and visible.
        /// </summary>
        public bool CanExpand
        {
            get => GetValue(CanExpandProperty);
            set => SetValue(CanExpandProperty, value);
        }

        /// <summary>
        /// Gets or sets the selection state of the item.
        /// </summary>
        public bool IsSelected
        {
            get => GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Gets whether the <see cref="HomeItemExpander"/> is expanded
        /// </summary>
        public bool IsEffectivelyExpanded
        {
            get => field;
            private set => SetAndRaise(IsEffectivelyExpandedProperty, ref field, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == CanExpandProperty || change.Property == IsExpandedProperty)
            {
                IsEffectivelyExpanded = CanExpand && IsExpanded;
            }
        }
    }
}
