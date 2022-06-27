using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

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
            Inlines.Invalidated += (s, e) => InlineHost?.Invalidate();
        }

        /// <summary>
        /// Gets or sets the inlines.
        /// </summary>
        [Content]
        public InlineCollection Inlines { get; }

        internal override void BuildTextRun(IList<TextRun> textRuns)
        {
            if (Inlines.HasComplexContent)
            {
                foreach (var inline in Inlines)
                {
                    inline.BuildTextRun(textRuns);
                }
            }
            else
            {
                if (Inlines.Text is string text)
                {
                    var textRunProperties = CreateTextRunProperties();

                    var textCharacters = new TextCharacters(text.AsMemory(), textRunProperties);

                    textRuns.Add(textCharacters);
                }          
            }
        }

        internal override void AppendText(StringBuilder stringBuilder)
        {
            if (Inlines.HasComplexContent)
            {
                foreach (var inline in Inlines)
                {
                    inline.AppendText(stringBuilder);
                }
            }

            if (Inlines.Text is string text)
            {
                stringBuilder.Append(text);
            }
        }
    }
}
