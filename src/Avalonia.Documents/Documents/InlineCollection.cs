using System;
using System.Collections;
using System.Text;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Documents
{
    /// <summary>
    /// A collection of <see cref="Inline"/>s.
    /// </summary>
    [WhitespaceSignificantCollection]
    public class InlineCollection : AvaloniaList<Inline>, IList
    {
        bool _isNull;

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineCollection"/> class.
        /// </summary>
        public InlineCollection()
        {
            ResetBehavior = ResetBehavior.Remove;
            this.ForEachItem(
                x =>
                {
                    x.Invalidated += Invalidate;
                    Invalidate();
                },
                x =>
                {
                    x.Invalidated -= Invalidate;
                    Invalidate();
                },
                () => throw new NotSupportedException());
        }

        /// <summary>
        /// Gets a string representation of the inlines.
        /// </summary>
        public string Text
        {
            get
            {
                if (Count == 0)
                {
                    return _isNull ? null : string.Empty;
                }
                else if (Count == 1)
                {
                    return ((IHasText)this[0]).Text ?? string.Empty;
                }
                else
                {
                    var result = new StringBuilder();

                    foreach (var i in this)
                    {
                        if (i is IHasText t)
                        {
                            result.Append(t.Text);
                        }
                    }

                    return result.ToString();
                }
            }

            set
            {
                if (Count == 1 && this[0] is Run r)
                {
                    if (r.Text == value)
                    {
                        return;
                    }
                }

                Clear();

                if (!string.IsNullOrEmpty(value))
                {
                    Add(new Run(value));
                }

                _isNull = value == null;
            }
        }

        /// <summary>
        /// Raised when an inline in the collection changes.
        /// </summary>
        public event EventHandler Invalidated;

        /// <inheirtdoc/>
        int IList.Add(object value)
        {
            if (value is string s)
            {
                Add(new Run(s));
            }
            else
            {
                Add((Inline)value);
            }

            return Count - 1;
        }

        /// <summary>
        /// Raises the <see cref="Invalidated"/> event.
        /// </summary>
        protected void Invalidate() => Invalidated?.Invoke(this, EventArgs.Empty);

        private void Invalidate(object sender, EventArgs e) => Invalidate();
    }
}
