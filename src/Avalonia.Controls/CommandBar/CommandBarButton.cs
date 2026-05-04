namespace Avalonia.Controls
{
    /// <summary>
    /// A button for use in a <see cref="CommandBar"/>.
    /// </summary>
    public class CommandBarButton : Button, ICommandBarElement
    {
        /// <summary>
        /// Defines the <see cref="Label"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> LabelProperty =
            AvaloniaProperty.Register<CommandBarButton, string?>(nameof(Label));

        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<CommandBarButton, object?>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="IsCompact"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsCompactProperty =
            AvaloniaProperty.Register<CommandBarButton, bool>(nameof(IsCompact));

        /// <summary>
        /// Defines the <see cref="DynamicOverflowOrder"/> property.
        /// </summary>
        public static readonly StyledProperty<int> DynamicOverflowOrderProperty =
            AvaloniaProperty.Register<CommandBarButton, int>(nameof(DynamicOverflowOrder));

        /// <summary>
        /// Defines the <see cref="LabelPosition"/> property.
        /// </summary>
        public static readonly StyledProperty<CommandBarDefaultLabelPosition> LabelPositionProperty =
            AvaloniaProperty.Register<CommandBarButton, CommandBarDefaultLabelPosition>(nameof(LabelPosition), CommandBarDefaultLabelPosition.Bottom);

        /// <summary>
        /// Defines the <see cref="IsInOverflow"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsInOverflowProperty =
            AvaloniaProperty.Register<CommandBarButton, bool>(nameof(IsInOverflow));

        /// <summary>
        /// Gets or sets the text label for the button.
        /// </summary>
        public string? Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        /// <summary>
        /// Gets or sets the icon content for the button.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the button is in compact mode (icon only, label hidden).
        /// </summary>
        public bool IsCompact
        {
            get => GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }

        /// <summary>
        /// Gets or sets the order in which this button moves to the overflow menu when space is limited.
        /// Lower values have higher priority (stay visible longer).
        /// </summary>
        public int DynamicOverflowOrder
        {
            get => GetValue(DynamicOverflowOrderProperty);
            set => SetValue(DynamicOverflowOrderProperty, value);
        }

        /// <summary>
        /// Gets or sets the label position. This is set automatically by the parent <see cref="CommandBar"/>.
        /// </summary>
        public CommandBarDefaultLabelPosition LabelPosition
        {
            get => GetValue(LabelPositionProperty);
            set => SetValue(LabelPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this button is displayed inside the overflow popup.
        /// Set automatically by <see cref="CommandBar"/> when moving items between primary and overflow.
        /// </summary>
        public bool IsInOverflow
        {
            get => GetValue(IsInOverflowProperty);
            set => SetValue(IsInOverflowProperty, value);
        }
    }
}
