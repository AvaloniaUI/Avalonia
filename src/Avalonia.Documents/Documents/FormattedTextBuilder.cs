using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace Avalonia.Documents
{
    /// <summary>
    /// Builds a <see cref="FormattedText"/> instance.
    /// </summary>
    public class FormattedTextBuilder
    {
        private StringBuilder _builder = new StringBuilder();
        private List<FormattedTextStyleSpan> _spans = new List<FormattedTextStyleSpan>();

        /// <summary>
        /// Gets the length of the current text.
        /// </summary>
        public int Length => _builder.Length;

        /// <summary>
        /// Adds text and an option style span.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="style">An optional style span.</param>
        public void Add(string text, FormattedTextStyleSpan style)
        {
            _builder.Append(text);

            if (style != null)
            {
                _spans.Add(style);
            }
        }

        /// <summary>
        /// Returns the built formatted text.
        /// </summary>
        /// <returns>A <see cref="FormattedText"/> instance.</returns>
        public FormattedText ToFormattedText()
        {
            return new FormattedText
            {
                Spans = _spans,
                Text = _builder.ToString(),
            };
        }
    }
}
