using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class TextBoxAutomationPeer : ControlAutomationPeer, IValueProvider, ITextProvider2
    {
        private TextBoxTextNavigation? _navigation;
        private (int Start, int End, int Caret)? _lastReportedSelection;

        public TextBoxAutomationPeer(TextBox owner)
            : base(owner)
        {
            Owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new TextBox Owner => (TextBox)base.Owner;
        public bool IsReadOnly => Owner.IsReadOnly;
        public string? Value => Owner.Text;
        public void SetValue(string? value) => Owner.Text = value;

        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.Single;

        public ITextRangeProvider DocumentRange
        {
            get
            {
                var navigation = Navigation;
                return new AutomationTextRange(navigation, navigation.DocumentStart, navigation.DocumentEnd);
            }
        }

        public IReadOnlyList<ITextRangeProvider> GetSelection()
        {
            var navigation = Navigation;
            var start = Math.Min(Owner.SelectionStart, Owner.SelectionEnd);
            var end = Math.Max(Owner.SelectionStart, Owner.SelectionEnd);

            var range = new AutomationTextRange(
                navigation,
                navigation.GetPosition(navigation.DocumentStart, start),
                navigation.GetPosition(navigation.DocumentStart, end));

            return new ITextRangeProvider[] { range };
        }

        public ITextRangeProvider? RangeFromPoint(Point point)
        {
            var navigation = Navigation;
            var position = navigation.GetPositionFromPoint(point);
            return position is null ? null : new AutomationTextRange(navigation, position, position);
        }

        public IReadOnlyList<ITextRangeProvider> GetVisibleRanges()
        {
            var navigation = Navigation;
            var visible = navigation.GetVisibleRange();
            return visible is null
                ? Array.Empty<ITextRangeProvider>()
                : new ITextRangeProvider[] { new AutomationTextRange(navigation, visible.Start, visible.End) };
        }

        // A plain TextBox has uniform text with no embedded automation elements.
        public ITextRangeProvider? RangeFromChild(AutomationPeer childElement) => null;

        public ITextRangeProvider? GetCaretRange(out bool isActive)
        {
            isActive = HasKeyboardFocus();

            var navigation = Navigation;
            var caret = navigation.GetPosition(navigation.DocumentStart, Owner.CaretIndex);
            return new AutomationTextRange(navigation, caret, caret);
        }

        private TextBoxTextNavigation Navigation => _navigation ??= new TextBoxTextNavigation(Owner);

        // Expose the text navigation as the cross-platform IAccessibleText provider (used by AT-SPI).
        protected override object? GetProviderCore(Type providerType)
            => providerType == typeof(IAccessibleText) ? Navigation : base.GetProviderCore(providerType);

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        protected override string? GetPlaceholderTextCore() => Owner.PlaceholderText;

        protected virtual void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property == TextBox.TextProperty)
            {
                RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, e.OldValue, e.NewValue);
                RaiseTextChanged(e.OldValue as string ?? string.Empty, e.NewValue as string ?? string.Empty);
            }
            else if (e.Property == TextBox.SelectionStartProperty ||
                     e.Property == TextBox.SelectionEndProperty ||
                     e.Property == TextBox.CaretIndexProperty)
            {
                RaiseTextSelectionChanged();
            }
        }

        private void RaiseTextChanged(string oldText, string newText)
        {
            var (offset, oldLength, newLength) = TextSegmentation.ComputeChange(oldText, newText);
            if (oldLength == 0 && newLength == 0)
            {
                return;
            }

            RaiseTextChangedEvent(
                offset,
                oldLength > 0 ? oldText.Substring(offset, oldLength) : string.Empty,
                newLength > 0 ? newText.Substring(offset, newLength) : string.Empty);
        }

        // A caret or selection move updates SelectionStart, SelectionEnd and CaretIndex as separate
        // properties; collapsing runs of identical states keeps one user action from raising several
        // identical events.
        private void RaiseTextSelectionChanged()
        {
            var start = Math.Min(Owner.SelectionStart, Owner.SelectionEnd);
            var end = Math.Max(Owner.SelectionStart, Owner.SelectionEnd);
            var current = (start, end, Owner.CaretIndex);

            if (current == _lastReportedSelection)
            {
                return;
            }

            _lastReportedSelection = current;
            RaiseTextSelectionChangedEvent(start, end, current.CaretIndex);
        }
    }
}
