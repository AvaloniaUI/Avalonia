using System;
using Avalonia.Metadata;

namespace Avalonia.Documents
{
    /// <summary>
    /// Represents a run of formatted text.
    /// </summary>
    public class Run : Inline, IHasText
    {
        /// <summary>
        /// Defines the <see cref="Text"/> property.
        /// </summary>
        public static readonly DirectProperty<Run, string> TextProperty =
            AvaloniaProperty.RegisterDirect<Run, string>(
                nameof(Text),
                o => o.Text,
                (o, v) => o.Text = v);

        string _text;

        /// <summary>
        /// Initializes a new instance of the <see cref="Run"/> class.
        /// </summary>
        public Run()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Run"/> class.
        /// </summary>
        /// <param name="text">The run text.</param>
        public Run(string text)
        {
            _text = text;
        }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [Content]
        public string Text
        {
            get { return _text; }
            set { SetAndRaise(TextProperty, ref _text, value); }
        }

        /// <inheritdoc/>
        public override void BuildFormattedText(FormattedTextBuilder builder)
        {
            if (Text?.Length > 0)
            {
                builder.Add(Text, CreateStyleSpan(builder.Length, Text.Length));
            }
        }
    }
}
