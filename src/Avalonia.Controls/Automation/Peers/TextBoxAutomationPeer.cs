using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class TextBoxAutomationPeer : ControlAutomationPeer, IValueProvider
    {
        public TextBoxAutomationPeer(TextBox owner)
            : base(owner)
        {
            Owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new TextBox Owner => (TextBox)base.Owner;
        public bool IsReadOnly => Owner.IsReadOnly;
        public string? Value => GetDisplayedText(Owner.Text, Owner.PasswordChar, Owner.RevealPassword);
        public void SetValue(string? value) => Owner.Text = value;

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
        private static string? GetDisplayedText(string? text, char passwordChar, bool revealPassword)
        {
            if (text is null || passwordChar == default(char) || revealPassword)
                return text;
            return new string(passwordChar, text.Length);
        }
    }
}
