using System;
using System.Collections.Generic;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control that displays a block of text.
    /// </summary>
    public class RichTextBlock : TextBlock, IInlineHost
    {
        /// <summary>
        /// Defines the <see cref="Inlines"/> property.
        /// </summary>
        public static readonly StyledProperty<InlineCollection> InlinesProperty =
            AvaloniaProperty.Register<RichTextBlock, InlineCollection>(
                nameof(Inlines));

        public RichTextBlock()
        {
            Inlines = new InlineCollection
            {
                Parent = this,
                InlineHost = this
            };
        }

        /// <summary>
        /// Gets or sets the inlines.
        /// </summary>
        public InlineCollection Inlines
        {
            get => GetValue(InlinesProperty);
            set => SetValue(InlinesProperty, value);
        }

        public void Add(Inline inline)
        {
            if (Inlines is not null)
            {
                Inlines.Add(inline);
            }
        }

        public new void Add(string text)
        {
            if (Inlines is not null)
            {
                Inlines.Add(text);
            }
        }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to format.</param>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        protected override TextLayout CreateTextLayout(Size constraint, string? text)
        {
            var defaultProperties = new GenericTextRunProperties(
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize,
                TextDecorations,
                Foreground);

            var paragraphProperties = new GenericTextParagraphProperties(FlowDirection, TextAlignment, true, false,
                defaultProperties, TextWrapping, LineHeight, 0);

            ITextSource textSource;

            var inlines = Inlines;

            if (inlines is not null && inlines.HasComplexContent)
            {
                var textRuns = new List<TextRun>();

                foreach (var inline in inlines)
                {
                    inline.BuildTextRun(textRuns);
                }

                textSource = new InlinesTextSource(textRuns);
            }
            else
            {
                textSource = new SimpleTextSource((text ?? "").AsMemory(), defaultProperties);
            }

            return new TextLayout(
                textSource,
                paragraphProperties,
                TextTrimming,
                constraint.Width,
                constraint.Height,
                maxLines: MaxLines,
                lineHeight: LineHeight);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(InlinesProperty):
                    {
                        OnInlinesChanged(change.OldValue as InlineCollection, change.NewValue as InlineCollection);
                        InvalidateTextLayout();
                        break;
                    }
                case nameof(TextProperty):
                    {
                        OnTextChanged(change.OldValue as string, change.NewValue as string);
                        break;
                    }
            }
        }

        private void OnTextChanged(string? oldValue, string? newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            if (Inlines is null)
            {
                return;
            }

            Inlines.Text = newValue;
        }

        private void OnInlinesChanged(InlineCollection? oldValue, InlineCollection? newValue)
        {
            if (oldValue is not null)
            {
                oldValue.Parent = null;
                oldValue.InlineHost = null;
                oldValue.Invalidated -= (s, e) => InvalidateTextLayout();
            }

            if (newValue is not null)
            {
                newValue.Parent = this;
                newValue.InlineHost = this;
                newValue.Invalidated += (s, e) => InvalidateTextLayout();
            }
        }

        void IInlineHost.AddVisualChild(IControl child)
        {
            if (child.VisualParent == null)
            {
                VisualChildren.Add(child);
            }
        }

        void IInlineHost.Invalidate()
        {
            InvalidateTextLayout();
        }

        private readonly struct InlinesTextSource : ITextSource
        {
            private readonly IReadOnlyList<TextRun> _textRuns;

            public InlinesTextSource(IReadOnlyList<TextRun> textRuns)
            {
                _textRuns = textRuns;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                var currentPosition = 0;

                foreach (var textRun in _textRuns)
                {
                    if (textRun.TextSourceLength == 0)
                    {
                        continue;
                    }

                    if (currentPosition >= textSourceIndex)
                    {
                        return textRun;
                    }

                    currentPosition += textRun.TextSourceLength;
                }

                return null;
            }
        }
    }
}
