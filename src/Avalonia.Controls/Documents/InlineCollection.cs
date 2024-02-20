using System;
using Avalonia.Collections;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// A collection of <see cref="Inline"/>s.
    /// </summary>
    [WhitespaceSignificantCollection]
    public class InlineCollection : AvaloniaList<Inline>
    {
        private IAvaloniaList<ILogical>? _logicalChildren;
        private IInlineHost? _inlineHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineCollection"/> class.
        /// </summary>
        public InlineCollection()
        {
            ResetBehavior = ResetBehavior.Remove;

            this.ForEachItem(
                x =>
                {
                    x.InlineHost = InlineHost;

                    LogicalChildren?.Add(x);

                    if (x is InlineUIContainer container)
                    {
                        InlineHost?.VisualChildren.Add(container.Child);
                    }

                    Invalidate();
                },
                x =>
                {
                    LogicalChildren?.Remove(x);

                    if(x is InlineUIContainer container)
                    {
                        InlineHost?.VisualChildren.Remove(container.Child);
                    }

                    x.InlineHost = null;

                    Invalidate();
                },
                () => throw new NotSupportedException());
        }

        internal IAvaloniaList<ILogical>? LogicalChildren
        {
            get => _logicalChildren;
            set
            {
                var oldValue = _logicalChildren;

                _logicalChildren = value;

                OnParentChanged(oldValue, value);
            }
        }

        internal IInlineHost? InlineHost
        {
            get => _inlineHost;
            set
            {
                _inlineHost = value;

                OnInlineHostChanged(value);
            }
        }

        /// <summary>
        /// Gets or adds the text held by the inlines collection.
        /// <remarks>
        /// Can be null for complex content.
        /// </remarks>
        /// </summary>
        public string? Text
        {
            get
            {
                if (Count == 0)
                {
                    return null;
                }

                var builder = StringBuilderCache.Acquire();

                foreach (var inline in this)
                {
                    inline.AppendText(builder);
                }

                return StringBuilderCache.GetStringAndRelease(builder);
            }

        }

        public override void Add(Inline inline)
        {
            if (InlineHost is TextBlock textBlock && !string.IsNullOrEmpty(textBlock.Text))
            {
                base.Add(new Run(textBlock.Text));

                textBlock.ClearTextInternal();
            }

            base.Add(inline);
        }

        /// <summary>
        /// Adds a text segment to the collection.
        /// <remarks>
        /// For non complex content this appends the text to the end of currently held text.
        /// For complex content this adds a <see cref="Run"/> to the collection.
        /// </remarks>
        /// </summary>
        /// <param name="text">The to be added text.</param>
        public void Add(string text)
        {
            if (InlineHost is TextBlock textBlock && !textBlock.HasComplexContent)
            {
                textBlock.Text += text;
            }
            else
            {
                Add(new Run(text));
            }
        }

        /// <summary>
        /// Adds a control wrapped inside a <see cref="InlineUIContainer"/> to the collection.
        /// </summary>
        /// <param name="control">The to be added control.</param>
        public void Add(Control control)
        {
            Add(new InlineUIContainer(control));
        }

        /// <summary>
        /// Raised when an inline in the collection changes.
        /// </summary>
        public event EventHandler? Invalidated;

        /// <summary>
        /// Raises the <see cref="Invalidated"/> event.
        /// </summary>
        protected void Invalidate()
        {
            InlineHost?.Invalidate();

            Invalidated?.Invoke(this, EventArgs.Empty);
        }

        private void OnParentChanged(IAvaloniaList<ILogical>? oldParent, IAvaloniaList<ILogical>? newParent)
        {
            foreach (var child in this)
            {
                if (oldParent != newParent)
                {
                    oldParent?.Remove(child);

                    newParent?.Add(child);
                }
            }
        }

        private void OnInlineHostChanged(IInlineHost? inlineHost)
        {
            foreach (var child in this)
            {
                child.InlineHost = inlineHost;
            }
        }
    }
}
