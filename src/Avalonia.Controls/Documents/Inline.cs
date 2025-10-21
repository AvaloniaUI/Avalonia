using System.Collections.Generic;
using System.Text;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// Inline element.
    /// </summary>
    public abstract class Inline : TextElement
    {
        // TODO12: change the field type to an AttachedProperty for consistency (breaking change)
        /// <summary>
        /// AvaloniaProperty for <see cref="TextDecorations" /> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationCollection?> TextDecorationsProperty =
            AvaloniaProperty.RegisterAttached<Inline, Inline, TextDecorationCollection?>(
                nameof(TextDecorations),
                inherits: true);

        /// <summary>
        /// AvaloniaProperty for <see cref="BaselineAlignment" /> property.
        /// </summary>
        public static readonly StyledProperty<BaselineAlignment> BaselineAlignmentProperty =
            AvaloniaProperty.Register<Inline, BaselineAlignment>(
                nameof(BaselineAlignment),
                BaselineAlignment.Baseline);

        /// <summary>
        /// The TextDecorations property specifies decorations that are added to the text of an element.
        /// </summary>
        public TextDecorationCollection? TextDecorations
        {
            get => GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        /// <summary>
        /// Describes how the baseline for a text-based element is positioned on the vertical axis,
        /// relative to the established baseline for text.
        /// </summary>
        public BaselineAlignment BaselineAlignment
        {
            get => GetValue(BaselineAlignmentProperty);
            set => SetValue(BaselineAlignmentProperty, value);
        }

        /// <summary>
        /// Gets the value of the attached <see cref="TextDecorationsProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The font style.</returns>
        public static TextDecorationCollection? GetTextDecorations(Control control)
        {
            return control.GetValue(TextDecorationsProperty);
        }

        /// <summary>
        /// Sets the value of the attached <see cref="TextDecorationsProperty"/> on a control.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetTextDecorations(Control control, TextDecorationCollection? value)
        {
            control.SetValue(TextDecorationsProperty, value);
        }
        
        internal abstract void BuildTextRun(IList<TextRun> textRuns, Size blockSize);

        internal abstract void AppendText(StringBuilder stringBuilder);

        protected TextRunProperties CreateTextRunProperties()
        {
            var parentOrSelfBackground = Background ?? FindParentBackground();

            var typeface = new Typeface(
                FontFamily, 
                FontStyle, 
                FontWeight, 
                FontStretch);

            return new GenericTextRunProperties(
                typeface,
                FontFeatures, 
                FontSize,
                TextDecorations, 
                Foreground,
                parentOrSelfBackground,
                BaselineAlignment);
        }

        /// <summary>
        /// Searches for the next parent inline element with a non-null Background and returns its Background brush.
        /// </summary>
        /// <returns>The first non-null Background brush found in parent inline elements, or null if none is found.</returns>
        private IBrush? FindParentBackground()
        {
            var parent = Parent;

            while (parent is Inline inline)
            {
                if (inline.Background != null)
                {
                    return inline.Background;
                }
                  
                parent = inline.Parent;
            }

            return null;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(TextDecorations):
                case nameof(BaselineAlignment):
                    InlineHost?.Invalidate();
                    break;
            }
        }
    }
}
