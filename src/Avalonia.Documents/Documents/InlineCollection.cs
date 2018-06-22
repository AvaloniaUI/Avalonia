using System;
using System.Collections;
using System.Text;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace Avalonia.Documents
{
    [WhitespaceSignificantCollection]
    public class InlineCollection : AvaloniaList<Inline>, IList
    {
        bool _isNull;

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

        public event EventHandler Invalidated;

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

        protected void Invalidate()
        {
            Invalidated?.Invoke(this, EventArgs.Empty);
        }

        private void Invalidate(object sender, EventArgs e) => Invalidate();
    }
}
