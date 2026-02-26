namespace Avalonia.Controls
{
    /// <summary>
    /// A button for use in a <see cref="CommandBar"/>.
    /// </summary>
    public class AppBarButton : Button, ICommandBarElement
    {
        public static readonly StyledProperty<string?> LabelProperty =
            AvaloniaProperty.Register<AppBarButton, string?>(nameof(Label));

        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<AppBarButton, object?>(nameof(Icon));

        public static readonly StyledProperty<bool> IsCompactProperty =
            AvaloniaProperty.Register<AppBarButton, bool>(nameof(IsCompact));

        public static readonly StyledProperty<int> DynamicOverflowOrderProperty =
            AvaloniaProperty.Register<AppBarButton, int>(nameof(DynamicOverflowOrder));

        public static readonly StyledProperty<CommandBarDefaultLabelPosition> LabelPositionProperty =
            AvaloniaProperty.Register<AppBarButton, CommandBarDefaultLabelPosition>(nameof(LabelPosition), CommandBarDefaultLabelPosition.Bottom);

        public static readonly StyledProperty<bool> IsInOverflowProperty =
            AvaloniaProperty.Register<AppBarButton, bool>(nameof(IsInOverflow));

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
