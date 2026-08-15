using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public partial class TextBoxAutomationPeer : ControlAutomationPeer, IValueProvider, ITextProvider
    {
        private bool _selectionChangeRaisePending;

        public TextBoxAutomationPeer(TextBox owner)
            : base(owner)
        {
            Owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new TextBox Owner => (TextBox)base.Owner;
        public bool IsReadOnly => Owner.IsReadOnly;
        public string? Value => Owner.Text;
        public void SetValue(string? value) => Owner.Text = value;

        public IReadOnlyList<ITextRangeProvider> GetSelection()
        {
            var start = Math.Min(Owner.SelectionStart, Owner.SelectionEnd);
            var end = Math.Max(Owner.SelectionStart, Owner.SelectionEnd);
            return new ITextRangeProvider[] { new TextRange(this, start, end) };
        }

        public IReadOnlyList<ITextRangeProvider> GetVisibleRanges() => new ITextRangeProvider[] { DocumentRange };

        public ITextRangeProvider? RangeFromChild(AutomationPeer childElement) => null;

        public ITextRangeProvider RangeFromPoint(Point point)
        {
            var presenter = Owner.Presenter;
            if (presenter is null)
                return DocumentRange;

            var local = TranslateFromOwner(point, presenter);
            var hit = presenter.TextLayout.HitTestPoint(local);
            var index = hit.CharacterHit.FirstCharacterIndex + hit.CharacterHit.TrailingLength;
            return new TextRange(this, index, index);
        }

        public ITextRangeProvider DocumentRange => new TextRange(this, 0, Owner.Text?.Length ?? 0);

        public SupportedTextSelection SupportedTextSelection => Provider.SupportedTextSelection.Single;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        protected override string? GetPlaceholderTextCore() => Owner.PlaceholderText;

        protected virtual void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.TextProperty)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, e.OldValue, e.NewValue);
                RaiseTextChangedEvent();
            }
            else if (e.Property == TextBox.CaretIndexProperty ||
                     e.Property == TextBox.SelectionStartProperty ||
                     e.Property == TextBox.SelectionEndProperty)
            {
                ScheduleSelectionChangedRaise();
            }
        }

        private void ScheduleSelectionChangedRaise()
        {
            if (_selectionChangeRaisePending)
                return;

            _selectionChangeRaisePending = true;

            Dispatcher.UIThread.Post(() =>
            {
                _selectionChangeRaisePending = false;
                RaiseTextSelectionChangedEvent();
            }, DispatcherPriority.Normal);
        }

        /// <summary>
        /// Converts a point in the peer's own top-level coordinate space (the space
        /// <see cref="RangeFromPoint"/> receives points in, matching
        /// <see cref="AutomationPeer.GetBoundingRectangle"/>'s convention) into the given
        /// presenter's local coordinate space.
        /// </summary>
        private static Point TranslateFromOwner(Point point, Visual presenter)
        {
            var root = presenter.GetVisualRoot() as Visual;
            if (root is null)
                return point;

            var toPresenter = root.TransformToVisual(presenter);
            return toPresenter.HasValue ? point.Transform(toPresenter.Value) : point;
        }
    }
}
