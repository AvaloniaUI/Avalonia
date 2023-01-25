using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public class TextBlockAutomationPeer : ControlAutomationPeer, ITextProvider, ITextPeer
    {
        public TextBlockAutomationPeer(TextBlock owner)
            : base(owner)
        {
            owner.GetObservable(TextBox.TextProperty).Subscribe(OnTextChanged);
        }

        public new TextBlock Owner => (TextBlock)base.Owner;
        public ITextRangeProvider DocumentRange => new AutomationTextRange(this, 0, Owner.Text?.Length ?? 0);
        public bool IsReadOnly => true;
        public int LineCount => Owner.TextLayout.TextLines.Count;
        public string? PlaceholderText => null;
        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.None;
        public string Text => Owner.Text ?? string.Empty;

        public event EventHandler? TextChanged;
        event EventHandler? ITextProvider.SelectedRangesChanged { add { } remove { } }

        public IReadOnlyList<ITextRangeProvider> GetVisibleRanges() => new[] { DocumentRange };

        public ITextRangeProvider RangeFromPoint(Point p)
        {
            var i = 0;

            if (Owner.GetVisualRoot() is Visual root &&
                root.TransformToVisual(Owner) is Matrix m)
            {
                i = Owner.TextLayout.HitTestPoint(p.Transform(m)).TextPosition;
            }

            return new AutomationTextRange(this, i, i);
        }

        IReadOnlyList<Rect> ITextPeer.GetBounds(int start, int end)
        {
            if (Owner.GetVisualRoot() is Visual root &&
                Owner.TransformToVisual(root) is Matrix m)
            {
                var source = Owner.TextLayout.HitTestTextRange(start, end - start);
                var result = new List<Rect>();

                foreach (var rect in source)
                    result.Add(rect.TransformToAABB(m));

                return result;
            }

            return Array.Empty<Rect>();
        }

        int ITextPeer.LineFromChar(int charIndex) => Owner.TextLayout.GetLineIndexFromCharacterIndex(charIndex, false);
        int ITextPeer.LineIndex(int lineIndex) => Owner.TextLayout.TextLines[lineIndex].FirstTextSourceIndex;
        void ITextPeer.ScrollIntoView(int start, int end) { }
        void ITextPeer.Select(int start, int end) => throw new NotSupportedException();

        ITextRangeProvider? ITextProvider.GetCaretRange() => null;
        IReadOnlyList<ITextRangeProvider> ITextProvider.GetSelection() => Array.Empty<ITextRangeProvider>();
        ITextRangeProvider ITextProvider.RangeFromChild(AutomationPeer childElement) => throw new NotSupportedException();

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
