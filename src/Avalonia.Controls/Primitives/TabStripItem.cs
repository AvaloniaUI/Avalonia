using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a tab in a <see cref="TabStrip"/>.
    /// </summary>
    public class TabStripItem : ListBoxItem
    {
        /// <summary>
        /// Defines the <see cref="Icon"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> IconProperty =
            AvaloniaProperty.Register<TabStripItem, object?>(nameof(Icon));

        /// <summary>
        /// Defines the <see cref="IconTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty =
            AvaloniaProperty.Register<TabStripItem, IDataTemplate?>(nameof(IconTemplate));

        /// <summary>
        /// Gets or sets the icon displayed alongside the content.
        /// </summary>
        public object? Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the icon of the control.
        /// </summary>
        public IDataTemplate? IconTemplate
        {
            get => GetValue(IconTemplateProperty);
            set => SetValue(IconTemplateProperty, value);
        }

        protected override void OnGotFocus(FocusChangedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateSelectionFromEvent(e);
        }
    }
}
