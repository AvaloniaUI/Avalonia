using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Input.TextInput;

namespace Avalonia.Automation.Peers
{
    public class TextBoxAutomationPeer : ControlAutomationPeer, IValueProvider, ITextProvider2
    {
        private readonly TextBoxTextNavigation _navigation;

        public TextBoxAutomationPeer(TextBox owner)
            : base(owner)
        {
            Owner.PropertyChanged += OwnerPropertyChanged;

            _navigation = new TextBoxTextNavigation(owner);
            _navigation.PlainTextChanged += (_, _) => RaiseTextChangedEvent();
            _navigation.SelectionChanged += (_, _) => RaiseTextSelectionChangedEvent();
        }

        public new TextBox Owner => (TextBox)base.Owner;
        public bool IsReadOnly => Owner.IsReadOnly;
        public string? Value => GetDisplayedText(Owner.Text, Owner.PasswordChar, Owner.RevealPassword);
        public void SetValue(string? value) => Owner.SetCurrentValue(TextBox.TextProperty, value);

        public SupportedTextSelection SupportedTextSelection => SupportedTextSelection.Single;

        public ITextRangeProvider DocumentRange
            => new AutomationTextRange(_navigation, _navigation.DocumentStart, _navigation.DocumentEnd);

        public IReadOnlyList<ITextRangeProvider> GetSelection()
        {
            var selection = _navigation.GetSelection();
            return new ITextRangeProvider[] { new AutomationTextRange(_navigation, selection.Start, selection.End) };
        }

        public ITextRangeProvider? RangeFromPoint(Point point)
        {
            var position = _navigation.GetPositionFromPoint(point);
            return position is null ? null : new AutomationTextRange(_navigation, position, position);
        }

        public IReadOnlyList<ITextRangeProvider> GetVisibleRanges()
        {
            var visible = _navigation.GetVisibleRange();
            return visible is null
                ? Array.Empty<ITextRangeProvider>()
                : new ITextRangeProvider[] { new AutomationTextRange(_navigation, visible.Start, visible.End) };
        }

        // A plain TextBox has uniform text with no embedded automation elements.
        public ITextRangeProvider? RangeFromChild(AutomationPeer childElement) => null;

        public ITextRangeProvider? GetCaretRange(out bool isActive)
        {
            isActive = HasKeyboardFocus();

            var caret = _navigation.GetCaret();
            return new AutomationTextRange(_navigation, caret.Start, caret.End);
        }

        // AT-SPI reads the control through IAccessibleText, which the navigation implements.
        protected override object? GetProviderCore(Type providerType)
            => providerType == typeof(IAccessibleText) ? _navigation : base.GetProviderCore(providerType);

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        protected override string? GetPlaceholderTextCore() => Owner.PlaceholderText;

        protected virtual void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if(e.Property == TextBox.TextProperty)
            {
                RaisePropertyChangedEvent(
                    ValuePatternIdentifiers.ValueProperty,
                    GetDisplayedText((string?)e.OldValue, Owner.PasswordChar, Owner.RevealPassword),
                    GetDisplayedText((string?)e.NewValue, Owner.PasswordChar, Owner.RevealPassword));
            }
            else if (e.Property == TextBox.PasswordCharProperty)
            {
                RaisePropertyChangedEvent(
                    ValuePatternIdentifiers.ValueProperty,
                    GetDisplayedText(Owner.Text, e.GetOldValue<char>(), Owner.RevealPassword),
                    Value);
            }
            else if (e.Property == TextBox.RevealPasswordProperty)
            {
                RaisePropertyChangedEvent(
                    ValuePatternIdentifiers.ValueProperty,
                    GetDisplayedText(Owner.Text, Owner.PasswordChar, e.GetOldValue<bool>()),
                    Value);
            }
        }

        // Expose the text as drawn so a masked password is not reported in clear.
        internal static string? GetDisplayedText(string? text, char passwordChar, bool revealPassword)
        {
            if (text is null || passwordChar == default(char) || revealPassword)
                return text;
            return new string(passwordChar, text.Length);
        }
    }
}
