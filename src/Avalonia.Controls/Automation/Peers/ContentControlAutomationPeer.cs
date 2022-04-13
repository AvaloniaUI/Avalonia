using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class ContentControlAutomationPeer : ControlAutomationPeer
    {
        protected ContentControlAutomationPeer(ContentControl owner)
            : base(owner) 
        { 
        }

        public new ContentControl Owner => (ContentControl)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        protected override string? GetNameCore()
        {
            var result = base.GetNameCore();

            if (result is null && Owner.Presenter?.Child is TextBlock text)
            {
                result = text.Text;
            }

            if (result is null)
            {
                result = Owner.Content?.ToString();
            }

            return result;
        }

        protected override bool IsContentElementCore() => false;
        protected override bool IsControlElementCore() => false;
    }
}
