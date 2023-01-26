using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Reactive;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public class TextBoxAutomationPeer : ControlAutomationPeer, ITextProvider, IValueProvider
    {
        public TextBoxAutomationPeer(TextBox owner)
            : base(owner)
        {
            owner.GetObservable(TextBox.SelectionStartProperty).Subscribe(OnSelectionChanged);
            owner.GetObservable(TextBox.SelectionEndProperty).Subscribe(OnSelectionChanged);
            owner.GetObservable(TextBox.TextProperty).Subscribe(OnTextChanged);
        }

        public int CaretIndex => Owner.CaretIndex;
        public new TextBox Owner => (TextBox)base.Owner;
        public bool IsReadOnly => Owner.IsReadOnly;
        public int LineCount => Owner.Presenter?.TextLayout.TextLines.Count ?? 0;
        public string? Value => Owner.Text;
        public TextRange DocumentRange => new TextRange(0, Owner.Text?.Length ?? 0);
        public string? PlaceholderText => Owner.Watermark;
        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.Single;

        public event EventHandler? SelectedRangesChanged;
        public event EventHandler? TextChanged;

        public IReadOnlyList<Rect> GetBounds(TextRange range)
        {
            if (Owner.GetVisualRoot() is Visual root &&
                Owner.Presenter?.TransformToVisual(root) is Matrix m)
            {
                var source = Owner.Presenter?.TextLayout.HitTestTextRange(range.Start, range.Length)
                    ?? Array.Empty<Rect>();
                var result = new List<Rect>();
                foreach (var rect in source)
                    result.Add(rect.TransformToAABB(m));
                return result;
            }

            return Array.Empty<Rect>();
        }

        public int GetLineForIndex(int index)
        {
            return Owner.Presenter?.TextLayout.GetLineIndexFromCharacterIndex(index, false) ?? -1;
        }

        public TextRange GetLineRange(int lineIndex)
        {
            if (Owner.Presenter is null)
                return TextRange.Empty;

            var line = Owner.Presenter.TextLayout.TextLines[lineIndex];
            return new TextRange(line.FirstTextSourceIndex, line.Length);
        }

        public IReadOnlyList<TextRange> GetSelection()
        {
            var range = TextRange.FromInclusiveStartEnd(Owner.SelectionStart, Owner.SelectionEnd);
            return new[] { range };
        }

        public string GetText(TextRange range)
        {
            var text = Owner.Text ?? string.Empty;
            var start = MathUtilities.Clamp(range.Start, 0, text.Length);
            var end = MathUtilities.Clamp(range.End, 0, text.Length);
            return text.Substring(start, end - start);
        }

        public IReadOnlyList<TextRange> GetVisibleRanges()
        {
            // Not sure this is necessary, QT just returns the document range too.
            return new[] { DocumentRange };
        }

        public TextRange RangeFromPoint(Point p)
        {
            if (Owner.Presenter is null)
                return TextRange.Empty;

            var i = 0;

            if (Owner.GetVisualRoot() is Visual root &&
                root.TransformToVisual(Owner) is Matrix m)
            {
                i = Owner.Presenter.TextLayout.HitTestPoint(p.Transform(m)).TextPosition;
            }

            return new TextRange(i, 1);
        }

        public void ScrollIntoView(TextRange range)
        {
            if (Owner.Presenter is null || Owner.Scroll is null)
                return;

            var rects = Owner.Presenter.TextLayout.HitTestTextRange(range.Start, range.Length);
            var rect = default(Rect);

            foreach (var r in rects)
                rect = rect.Union(r);

            Owner.Presenter.BringIntoView(rect);
        }

        public void Select(TextRange range)
        {
            Owner.SelectionStart = range.Start;
            Owner.SelectionEnd = range.End;
        }

        public void SetValue(string? value) => Owner.Text = value;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }


        private void OnSelectionChanged(int obj) => SelectedRangesChanged?.Invoke(this, EventArgs.Empty);
        private void OnTextChanged(string? text) => TextChanged?.Invoke(this, EventArgs.Empty);
    }
}
