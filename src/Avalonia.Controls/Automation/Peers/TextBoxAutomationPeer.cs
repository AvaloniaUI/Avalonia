using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

#nullable enable

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
        public string? PlaceholderText => Owner.Watermark;
        public string? Value => Owner.Text;
        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.Single;

        public ITextRangeProvider DocumentRange => new AutomationTextRange(this, 0, Owner.Text?.Length ?? 0);

        int ITextPeer.LineCount => Owner.Presenter?.FormattedText.GetLines().Count() ?? 1;
        string ITextPeer.Text => Owner.Text ?? string.Empty;
        
        public event EventHandler? SelectedRangesChanged;
        public event EventHandler? TextChanged;

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
            var i = 0;

            if (Owner.GetVisualRoot() is IVisual root &&
                root.TransformToVisual(Owner) is Matrix m)
            {
                i = Owner.Presenter.GetCaretIndex(p.Transform(m));
            }

            return new AutomationTextRange(this, i, i);
        }

        public void SetValue(string? value) => Owner.Text = value;

        IReadOnlyList<Rect> ITextPeer.GetBounds(int start, int end)
        {
            if (Owner.Presenter is TextPresenter presenter &&
                Owner.GetVisualRoot() is IVisual root &&
                presenter.TransformToVisual(root) is Matrix m)
            {
                var scroll = Owner.Scroll as Control;
                var clip = new Rect(scroll?.Bounds.Size ?? presenter.Bounds.Size);
                var source = presenter.FormattedText.HitTestTextRange(start, end - start);
                var result = new List<Rect>();

                foreach (var rect in source)
                {
                    var r = rect.Intersect(clip);
                    if (!r.IsEmpty)
                        result.Add(r.TransformToAABB(m));
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

            foreach (var line in Owner.Presenter.FormattedText.GetLines())
            {
                if ((c += line.Length) > charIndex)
                    return l;
                ++l;
            }

            return l;
        }

        int ITextPeer.LineIndex(int lineIndex)
        {
            var c = 0;
            var l = 0;
            var lines = Owner.Presenter.FormattedText.GetLines();            

            foreach (var line in lines)
            {
                if (l++ == lineIndex)
                    break;
                c+= line.Length;
            }

            return c;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        private void OnSelectionChanged(int obj) => SelectedRangesChanged?.Invoke(this, EventArgs.Empty);
        private void OnTextChanged(string? text) => TextChanged?.Invoke(this, EventArgs.Empty);
    }
}
