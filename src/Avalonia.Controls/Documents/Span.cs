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
        public static readonly StyledProperty<InlineCollection> InlinesProperty =
            AvaloniaProperty.Register<Span, InlineCollection>(
                nameof(Inlines));

        public Span()
        {
            Inlines = new InlineCollection
            {
                Parent = this
            };
        }

        /// <summary>
        /// Gets or sets the inlines.
        /// </summary>
        [Content]
        public InlineCollection Inlines
        {
            get => GetValue(InlinesProperty);
            set => SetValue(InlinesProperty, value);
        }

        internal override void BuildTextRun(IList<TextRun> textRuns)
        {
            foreach (var inline in Inlines)
            {
                inline.BuildTextRun(textRuns);
            }
        }

        internal override void AppendText(StringBuilder stringBuilder)
        {
            foreach (var inline in Inlines)
            {
                inline.AppendText(stringBuilder);
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(InlinesProperty):
                    OnInlinesChanged(change.OldValue as InlineCollection, change.NewValue as InlineCollection);
                    InlineHost?.Invalidate();
                    break;
            }
        }

        internal override void OnInlineHostChanged(IInlineHost? oldValue, IInlineHost? newValue)
        {
            base.OnInlineHostChanged(oldValue, newValue);

            if (Inlines is not null)
            {
                Inlines.InlineHost = newValue;
            }
        }

        private void OnInlinesChanged(InlineCollection? oldValue, InlineCollection? newValue)
        {
            if (oldValue is not null)
            {
                oldValue.Parent = null;
                oldValue.InlineHost = null;
                oldValue.Invalidated -= (s, e) => InlineHost?.Invalidate();
            }

            if (newValue is not null)
            {
                newValue.Parent = this;
                newValue.InlineHost = InlineHost;
                newValue.Invalidated += (s, e) => InlineHost?.Invalidate();
            }
        }
    }
}
