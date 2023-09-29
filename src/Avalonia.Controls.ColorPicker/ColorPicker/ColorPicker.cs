using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    /// <summary>
    /// Presents a color for user editing using a spectrum, palette and component sliders within a drop down.
    /// Editing is available when the drop down flyout is opened; otherwise, only the preview color is shown.
    /// </summary>
    [PseudoClasses(PC_Empty)]
    public class ColorPicker : ColorView
    {
        private const string PC_Empty = ":empty";
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPicker"/> class.
        /// </summary>
        public ColorPicker() : base()
        {
        }

        /// <summary>
        /// Defines the <see cref="Content"/> property. 
        /// </summary>
        public static readonly StyledProperty<object?> ContentProperty = AvaloniaProperty.Register<ColorPicker, object?>(
            nameof(Content));

        /// <summary>
        /// Get or set the Content displayed in Dropdown Button. 
        /// </summary>
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property. 
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty = AvaloniaProperty.Register<ColorPicker, IDataTemplate?>(
            nameof(ContentTemplate));

        /// <summary>
        /// Get or set the ContentTemplate displayed in Dropdown Button. 
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get => GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (Content is null)
            {
                PseudoClasses.Set(PC_Empty, true);
            }
        }

        /// <inheirtdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ContentProperty)
            {
                PseudoClasses.Set(PC_Empty, change.GetNewValue<object?>() is null);
            }
        }
    }
}
