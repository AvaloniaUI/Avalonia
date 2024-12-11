using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class TextBoxAutomationPeer : ControlAutomationPeer, IValueProvider
    {
        private string? ownerText;

        public TextBoxAutomationPeer(TextBox owner)
            : base(owner)
        {
            owner.TextChanged += TextChanged;
            ownerText = owner.Text;
        }

        private void TextChanged(object? sender, TextChangedEventArgs e)
        {
            RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, ownerText, Owner.Text);
            ownerText = Owner.Text;
        }

        public new TextBox Owner => (TextBox)base.Owner;
        public bool IsReadOnly => Owner.IsReadOnly;
        public string? Value => Owner.Text;
        public void SetValue(string? value) => Owner.Text = value;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Edit;
        }

        protected override string? GetNameCore()
        {
            return Owner.Text;
        }
    }
}
