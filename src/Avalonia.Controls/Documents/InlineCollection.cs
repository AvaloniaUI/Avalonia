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
                var builder = new StringBuilder();

                foreach (var inline in this)
                {
                    inline.AppendText(builder);
                }

                return builder.ToString();
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
            Add(new Run(text));
        }

        public void Add(IControl child)
        {
            Add(new InlineUIContainer(child));
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
