// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
        private readonly StringBuilder _builder = new StringBuilder();
        private readonly List<FormattedTextStyleSpan> _spans = new List<FormattedTextStyleSpan>();

        /// <summary>
        /// Gets the length of the current text.
        /// </summary>
        public int Length => _builder.Length;

        /// <summary>
        /// Gets or sets the typeface.
        /// </summary>
        /// <value>
        /// The typeface.
        /// </value>
        public Typeface Typeface { get; set; }

        /// <summary>
        /// Gets or sets the size of the font.
        /// </summary>
        /// <value>
        /// The size of the font.
        /// </value>
        public double FontSize { get; set; }

        /// <summary>
        /// Gets or sets the text alignment.
        /// </summary>
        /// <value>
        /// The text alignment.
        /// </value>
        public TextAlignment TextAlignment { get; set; }

        /// <summary>
        /// Gets or sets the text wrapping.
        /// </summary>
        /// <value>
        /// The text wrapping.
        /// </value>
        public TextWrapping TextWrapping { get; set; }

        /// <summary>
        /// Gets or sets the constraint.
        /// </summary>
        /// <value>
        /// The constraint.
        /// </value>
        public Size Constraint { get; set; }

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
        public FormattedText Build()
        {
            return new FormattedText
            {               
                Text = _builder.ToString(),
                Typeface = Typeface,
                FontSize = FontSize,
                TextAlignment = TextAlignment,
                TextWrapping = TextWrapping,
                Constraint = Constraint,
                Spans = _spans
            };
        }
    }
}
