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
        private string? _text = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineCollection"/> class.
        /// </summary>
        public InlineCollection(ILogical parent) : base(0)
        {
            ResetBehavior = ResetBehavior.Remove;
            
            this.ForEachItem(
                x =>
                {
                    ((ISetLogicalParent)x).SetParent(parent);
                    x.Invalidated += Invalidate;
                    Invalidate();
                },
                x =>
                {
                    ((ISetLogicalParent)x).SetParent(null);
                    x.Invalidated -= Invalidate;
                    Invalidate();
                },
                () => throw new NotSupportedException());
        }

        public bool HasComplexContent => Count > 0;

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
                if (!HasComplexContent)
                {
                    return _text;
                }
                
                var builder = new StringBuilder();

                foreach(var inline in this)
                {
                    inline.AppendText(builder);
                }

                return builder.ToString();
            }
            set
            {
                if (HasComplexContent)
                {
                    Add(new Run(value));
                }
                else
                {
                    _text = value;
                }
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
            if (HasComplexContent)
            {
                Add(new Run(text));
            }
            else
            {
                _text += text;
            }
        }

        public override void Add(Inline item)
        {
            if (!HasComplexContent)
            {
                base.Add(new Run(_text));
                
                _text = string.Empty;
            }
            
            base.Add(item);
        }

        /// <summary>
        /// Raised when an inline in the collection changes.
        /// </summary>
        public event EventHandler? Invalidated;
        
        /// <summary>
        /// Raises the <see cref="Invalidated"/> event.
        /// </summary>
        protected void Invalidate() => Invalidated?.Invoke(this, EventArgs.Empty);

        private void Invalidate(object? sender, EventArgs e) => Invalidate();
    }
}
