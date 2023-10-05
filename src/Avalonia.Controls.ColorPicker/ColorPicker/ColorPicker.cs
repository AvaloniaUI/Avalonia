using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Presents a color for user editing using a spectrum, palette and component sliders within a drop down.
    /// Editing is available when the drop down flyout is opened; otherwise, only the preview content area is shown.
    /// </summary>
    public class ColorPicker : ColorView
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPicker"/> class.
        /// </summary>
        public ColorPicker() : base()
        {
        }

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ColorPicker>();

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
            ContentControl.ContentTemplateProperty.AddOwner<ColorPicker>();

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<ColorPicker>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<ColorPicker>();

        /// <summary>
        /// Gets or sets any content displayed in the ColorPicker's preview content area.
        /// </summary>
        /// <remarks>
        /// By default this should show a preview of the currently selected color. 
        /// </remarks>
        [Content]
        [DependsOn(nameof(ContentTemplate))]
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the content of the ColorPicker's preview content area.
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get => GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the ColorPicker's preview content area.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the ColorPicker's preview content area.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }
    }
}
