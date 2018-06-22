using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace Avalonia.Documents
{
    public class FormattedTextBuilder
    {
        private StringBuilder _builder = new StringBuilder();
        private List<FormattedTextStyleSpan> _spans = new List<FormattedTextStyleSpan>();

        public int StartIndex => _builder.Length;

        public void Add(string text, FormattedTextStyleSpan style)
        {
            _builder.Append(text);

            if (style != null)
            {
                _spans.Add(style);
            }
        }

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
