using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// A visual separator for use in a <see cref="CommandBar"/>.
    /// </summary>
    public class AppBarSeparator : TemplatedControl, ICommandBarElement
    {
        /// <summary>
        /// Defines the <see cref="IsCompact"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsCompactProperty =
            AvaloniaProperty.Register<AppBarSeparator, bool>(nameof(IsCompact));

        /// <summary>
        /// Defines the <see cref="IsInOverflow"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsInOverflowProperty =
            AvaloniaProperty.Register<AppBarSeparator, bool>(nameof(IsInOverflow));

        /// <summary>
        /// Gets or sets whether the separator is in compact mode.
        /// </summary>
        public bool IsCompact
        {
            get => GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the separator is displayed inside the overflow popup.
        /// Set automatically by <see cref="CommandBar"/> when moving items between primary and overflow.
        /// </summary>
        public bool IsInOverflow
        {
            get => GetValue(IsInOverflowProperty);
            set => SetValue(IsInOverflowProperty, value);
        }
    }
}
