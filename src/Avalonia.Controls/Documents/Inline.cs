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
        /// <summary>
        /// AvaloniaProperty for <see cref="TextDecorations" /> property.
        /// </summary>
        public static readonly StyledProperty<TextDecorationCollection> TextDecorationsProperty =
            AvaloniaProperty.Register<Inline, TextDecorationCollection>(
                nameof(TextDecorations));

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
        public TextDecorationCollection TextDecorations
        {
            get { return GetValue(TextDecorationsProperty); }
            set { SetValue(TextDecorationsProperty, value); }
        }

        /// <summary>
        /// Describes how the baseline for a text-based element is positioned on the vertical axis,
        /// relative to the established baseline for text.
        /// </summary>
        public BaselineAlignment BaselineAlignment
        {
            get { return GetValue(BaselineAlignmentProperty); }
            set { SetValue(BaselineAlignmentProperty, value); }
        }

        internal abstract void BuildTextRun(IList<TextRun> textRuns);

        internal abstract void AppendText(StringBuilder stringBuilder);

        protected TextRunProperties CreateTextRunProperties()
        {
            return new GenericTextRunProperties(new Typeface(FontFamily, FontStyle, FontWeight), FontSize,
                TextDecorations, Foreground, Background, BaselineAlignment);
        }
        
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
