using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Reactive;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public class TextBlockAutomationPeer : ControlAutomationPeer, ITextProvider
    {
        public TextBlockAutomationPeer(TextBlock owner)
            : base(owner)
        {
            owner.GetObservable(TextBox.TextProperty).Subscribe(OnTextChanged);
        }

        public int CaretIndex => -1;
        public new TextBlock Owner => (TextBlock)base.Owner;
        public TextRange DocumentRange => new TextRange(0, Owner.Text?.Length ?? 0);
        public bool IsReadOnly => true;
        public int LineCount => Owner.TextLayout.TextLines.Count;
        public string? PlaceholderText => null;
        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.None;

        public event EventHandler? TextChanged;
        event EventHandler? ITextProvider.SelectedRangesChanged { add { } remove { } }

        public IReadOnlyList<Rect> GetBounds(TextRange range)
        {
            if (Owner.GetVisualRoot() is Visual root &&
                Owner.TransformToVisual(root) is Matrix m)
            {
                var source = Owner.TextLayout.HitTestTextRange(range.Start, range.Length);
                var result = new List<Rect>();
                foreach (var rect in source)
                    result.Add(rect.TransformToAABB(m));
                return result;
            }

            return Array.Empty<Rect>();
        }

        public int GetLineForIndex(int index)
        {
            return Owner.TextLayout.GetLineIndexFromCharacterIndex(index, false);
        }

        public TextRange GetLineRange(int lineIndex)
        {
            var line = Owner.TextLayout.TextLines[lineIndex];
            return new TextRange(line.FirstTextSourceIndex, line.Length);
        }

        public string GetText(TextRange range)
        {
            var text = Owner.Text ?? string.Empty;
            var start = MathUtilities.Clamp(range.Start, 0, text.Length);
            var end = MathUtilities.Clamp(range.End, 0, text.Length);
            return text.Substring(start, end - start);
        }

        public IReadOnlyList<TextRange> GetVisibleRanges() => new[] { DocumentRange };

        public TextRange RangeFromPoint(Point p)
        {
            var i = 0;

            if (Owner.GetVisualRoot() is Visual root &&
                root.TransformToVisual(Owner) is Matrix m)
            {
                i = Owner.TextLayout.HitTestPoint(p.Transform(m)).TextPosition;
            }

            return new TextRange(i, 1);
        }

        IReadOnlyList<TextRange> ITextProvider.GetSelection() => Array.Empty<TextRange>();
        void ITextProvider.ScrollIntoView(TextRange range) { }
        void ITextProvider.Select(TextRange range) { }

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;
        protected override string? GetNameCore() => Owner.Text;

        protected override bool IsControlElementCore()
        {
            // Return false if the control is part of a control template.
            return Owner.TemplatedParent is null && base.IsControlElementCore();
        }

        private void OnTextChanged(string? text) => TextChanged?.Invoke(this, EventArgs.Empty);

    }
}
