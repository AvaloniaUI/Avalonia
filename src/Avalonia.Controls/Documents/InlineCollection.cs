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
        private readonly IInlineHost? _host;
        private string? _text = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineCollection"/> class.
        /// </summary>
        public InlineCollection(ILogical parent) : this(parent, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineCollection"/> class.
        /// </summary>
        internal InlineCollection(ILogical parent, IInlineHost? host = null) : base(0)
        {
            _host = host;

            ResetBehavior = ResetBehavior.Remove;
            
            this.ForEachItem(
                x =>
                {
                    ((ISetLogicalParent)x).SetParent(parent);
                    x.InlineHost = host;
                    host?.Invalidate();
                },
                x =>
                {
                    ((ISetLogicalParent)x).SetParent(null);
                    x.InlineHost = host;
                    host?.Invalidate();
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

        public void Add(IControl child)
        {
            var implicitRun = new InlineUIContainer(child);

            Add(implicitRun);
        }

        public override void Add(Inline item)
        {
            if (!HasComplexContent)
            {
                if (!string.IsNullOrEmpty(_text))
                {
                    base.Add(new Run(_text));
                }
                             
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
        protected void Invalidate()
        {
            if(_host != null)
            {
                _host.Invalidate();
            }

            Invalidated?.Invoke(this, EventArgs.Empty);
        }

        private void Invalidate(object? sender, EventArgs e) => Invalidate();
    }
}
