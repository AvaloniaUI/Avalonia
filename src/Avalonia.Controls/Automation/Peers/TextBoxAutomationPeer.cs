using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class TextBoxAutomationPeer : ControlAutomationPeer, IValueProvider, ITextProvider
    {
        private TextBoxTextNavigation? _navigation;

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
            }
        }
    }
}
