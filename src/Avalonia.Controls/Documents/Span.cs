using System.Collections.Generic;
using System.Text;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// Span element used for grouping other Inline elements.
    /// </summary>
    public class Span : Inline
    {
        /// <summary>
        /// Defines the <see cref="Inlines"/> property.
        /// </summary>
        public static readonly DirectProperty<Span, InlineCollection> InlinesProperty =
            AvaloniaProperty.RegisterDirect<Span, InlineCollection>(
                nameof(Inlines),
                o => o.Inlines);

        /// <summary>
        /// Initializes a new instance of a Span element.
        /// </summary>
        public Span()
        {
            Inlines = new InlineCollection(this);

            Inlines.Invalidated += (s, e) => Invalidate();
        }

        /// <summary>
        /// Gets or sets the inlines.
        /// </summary>
        [Content]
        public InlineCollection Inlines { get; }

        internal override int BuildRun(StringBuilder stringBuilder, IList<ValueSpan<TextRunProperties>> textStyleOverrides, int firstCharacterIndex)
        {
            var length = 0;

            if (Inlines.HasComplexContent)
            {
                foreach (var inline in Inlines)
                {
                    var inlineLength = inline.BuildRun(stringBuilder, textStyleOverrides, firstCharacterIndex);

                    firstCharacterIndex += inlineLength;

                    length += inlineLength;
                }
            }
            else
            {
                if (Inlines.Text == null)
                {
                    return length;
                }
                
                stringBuilder.Append(Inlines.Text);

                length = Inlines.Text.Length;

                textStyleOverrides.Add(new ValueSpan<TextRunProperties>(firstCharacterIndex, length,
                    CreateTextRunProperties()));
            }

            return length;
        }

        internal override int AppendText(StringBuilder stringBuilder)
        {
            if (Inlines.HasComplexContent)
            {
                var length = 0;

                foreach (var inline in Inlines)
                {
                    length += inline.AppendText(stringBuilder);
                }

                return length;
            }

            if (Inlines.Text == null)
            {
                return 0;
            }
         
            stringBuilder.Append(Inlines.Text);

            return Inlines.Text.Length;
        }
    }
}
