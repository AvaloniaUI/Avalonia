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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1012", 
            Justification = "Collection properties shouldn't be set with SetCurrentValue.")]
        public Span()
        {
            Inlines = new InlineCollection
            {
                LogicalChildren = LogicalChildren
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

        /// <inheritdoc />
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

            Inlines.InlineHost = newValue;
        }

        private void OnInlinesChanged(InlineCollection? oldValue, InlineCollection? newValue)
        {
            void OnInlinesInvalidated(object? sender, EventArgs e)
                => InlineHost?.Invalidate();

            if (oldValue is not null)
            {
                oldValue.LogicalChildren = null;
                oldValue.InlineHost = null;
                oldValue.Invalidated -= OnInlinesInvalidated;
            }

            if (newValue is not null)
            {
                newValue.LogicalChildren = LogicalChildren;
                newValue.InlineHost = InlineHost;
                newValue.Invalidated += OnInlinesInvalidated;
            }
        }
    }
}
