using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public class TextBoxAutomationPeer : ControlAutomationPeer,
        ITextPeer,
        ITextProvider,
        IValueProvider
    {
        public TextBoxAutomationPeer(TextBox owner)
            : base(owner)
        {
            owner.GetObservable(TextBox.SelectionStartProperty).Subscribe(OnSelectionChanged);
            owner.GetObservable(TextBox.SelectionEndProperty).Subscribe(OnSelectionChanged);
            owner.GetObservable(TextBox.TextProperty).Subscribe(OnTextChanged);
        }

        public new TextBox Owner => (TextBox)base.Owner;
        public bool IsReadOnly => Owner.IsReadOnly;
        public string? Value => Owner.Text;
        public ITextRangeProvider DocumentRange => new AutomationTextRange(this, 0, Owner.Text?.Length ?? 0);
        public string? PlaceholderText => Owner.Watermark;
        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.Single;

        int ITextPeer.LineCount => Owner.Presenter?.TextLayout.TextLines.Count ?? 1;
        string ITextPeer.Text => Owner.Text ?? string.Empty;

        public event EventHandler? SelectedRangesChanged;
        public event EventHandler? TextChanged;

        public ITextRangeProvider? GetCaretRange()
        {
            return new AutomationTextRange(this, Owner.CaretIndex, Owner.CaretIndex);
        }

        public IReadOnlyList<ITextRangeProvider> GetSelection()
        {
            var start = Owner.SelectionStart;
            var end = Owner.SelectionEnd;
            return new[] { new AutomationTextRange(this, Math.Min(start, end), Math.Max(start, end)) };
        }

        public IReadOnlyList<ITextRangeProvider> GetVisibleRanges()
        {
            // Not sure this is necessary, QT just returns the document range too.
            return new[] { DocumentRange };
        }

        public ITextRangeProvider RangeFromChild(AutomationPeer childElement)
        {
            // We don't currently support embedding.
            throw new ArgumentException(nameof(childElement));
        }

        public ITextRangeProvider RangeFromPoint(Point p)
        {
            if (Owner.Presenter is null)
                return new AutomationTextRange(this, 0, 0);

            var i = 0;

            if (Owner.GetVisualRoot() is Visual root &&
                root.TransformToVisual(Owner) is Matrix m)
            {
                i = Owner.Presenter.TextLayout.HitTestPoint(p.Transform(m)).TextPosition;
            }

            return new AutomationTextRange(this, i, i);
        }

        public void SetValue(string? value) => Owner.Text = value;

        IReadOnlyList<Rect> ITextPeer.GetBounds(int start, int end)
        {
            if (Owner.Presenter is TextPresenter presenter &&
                presenter.TransformedBounds is TransformedBounds t)
            {
                var source = presenter.TextLayout.HitTestTextRange(start, end - start);
                var result = new List<Rect>();

                foreach (var rect in source)
                {
                    var r = rect.TransformToAABB(t.Transform).Intersect(t.Clip);
                    if (!r.IsDefault)
                        result.Add(r);
                }

                return result;
            }

            return Array.Empty<Rect>();
        }

        int ITextPeer.LineFromChar(int charIndex)
        {
            if (Owner.Presenter is null)
                return 0;

            var l = 0;
            var c = 0;

            foreach (var line in Owner.Presenter.TextLayout.TextLines)
            {
                if ((c += line.Length) > charIndex)
                    return l;
                ++l;
            }

            return l;
        }

        int ITextPeer.LineIndex(int lineIndex)
        {
            if (Owner.Presenter is null)
                return 0;

            var c = 0;
            var l = 0;
            var lines = Owner.Presenter.TextLayout.TextLines;            

            foreach (var line in lines)
            {
                if (l++ == lineIndex)
                    break;
                c+= line.Length;
            }

            return c;
        }

        void ITextPeer.ScrollIntoView(int start, int end)
        {
            if (Owner.Presenter is null || Owner.Scroll is null)
                return;

            var rects = Owner.Presenter.TextLayout.HitTestTextRange(start, end - start);
            var rect = default(Rect);

            foreach (var r in rects)
                rect = rect.Union(r);

            Owner.Presenter.BringIntoView(rect);
        }

        void ITextPeer.Select(int start, int end)
        {
            Owner.SelectionStart = start;
            Owner.SelectionEnd = end;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        private void OnSelectionChanged(int obj) => SelectedRangesChanged?.Invoke(this, EventArgs.Empty);
        private void OnTextChanged(string? text) => TextChanged?.Invoke(this, EventArgs.Empty);
    }
}
