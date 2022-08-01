using System;
using System.Text;
using Avalonia.Collections;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// A collection of <see cref="Inline"/>s.
    /// </summary>
    [WhitespaceSignificantCollection]
    public class InlineCollection : AvaloniaList<Inline>
    {
        private ILogical? _parent;
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
                    ((ISetLogicalParent)x).SetParent(Parent);
                    x.InlineHost = InlineHost;
                    Invalidate();
                },
                x =>
                {
                    ((ISetLogicalParent)x).SetParent(null);
                    x.InlineHost = InlineHost;
                    Invalidate();
                },
                () => throw new NotSupportedException());
        }

        internal ILogical? Parent
        {
            get => _parent;
            set
            {
                _parent = value;

                OnParentChanged(value);
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
                var builder = StringBuilderCache.Acquire();

                foreach (var inline in this)
                {
                    inline.AppendText(builder);
                }

                return StringBuilderCache.GetStringAndRelease(builder);
            }

        }

        /// <summary>
        /// Add a text segment to the collection.
        /// <remarks>
        /// For non complex content this appends the text to the end of currently held text.
        /// For complex content this adds a <see cref="Run"/> to the collection.
        /// </remarks>
        /// </summary>
        /// <param name="text"></param>
        public void Add(string text)
        {
            AddText(text);
        }

        public override void Add(Inline inline)
        {
            OnAdd();

            base.Add(inline);
        }

        public void Add(IControl child)
        {
            OnAdd();

            base.Add(new InlineUIContainer(child));
        }

        private void AddText(string text)
        {
            if(Parent is RichTextBlock textBlock && !textBlock.HasComplexContent)
            {
                textBlock._text += text;
            }
            else
            {
                base.Add(new Run(text));
            }
        }

        private void OnAdd()
        {
            if (Parent is RichTextBlock textBlock)
            {
                if (!textBlock.HasComplexContent && !string.IsNullOrEmpty(textBlock._text))
                {
                    base.Add(new Run(textBlock._text));

                    textBlock._text = null;
                }
            }
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
            if (InlineHost != null)
            {
                InlineHost.Invalidate();
            }

            Invalidated?.Invoke(this, EventArgs.Empty);
        }

        private void OnParentChanged(ILogical? parent)
        {
            foreach (var child in this)
            {
                ((ISetLogicalParent)child).SetParent(parent);
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
