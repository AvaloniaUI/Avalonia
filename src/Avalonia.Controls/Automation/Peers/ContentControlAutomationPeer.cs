using Avalonia.Controls;
using Avalonia.LogicalTree;

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
            if (!string.IsNullOrWhiteSpace(result))
            {
                return result;
            }
            else
            {
                Control? childControl = Owner.Presenter?.Child;
                AutomationPeer? childPeer = childControl is null ? null :
                    CreatePeerForElement(childControl);
                string? childName = (childControl as TextBlock)?.Text ?? childPeer?.GetName();
                if (!string.IsNullOrWhiteSpace(childName))
                {
                    return childName;
                }

                // A control's ToString is its type name, so use the first text it shows instead.
                return Owner.Content is Control content ?
                    GetFirstText(content) :
                    Owner.Content?.ToString();
            }
        }

        private static string? GetFirstText(Control content)
        {
            foreach (var logical in content.GetSelfAndLogicalDescendants())
            {
                if (logical is TextBlock { Text: { } text } && !string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            return null;
        }

        protected override string? GetHelpTextCore()
        {
            Control? childControl = Owner.Presenter?.Child;
            AutomationPeer? childPeer = childControl is null ? null :
                CreatePeerForElement(childControl);
            return base.GetHelpTextCore() ??
                childPeer?.GetHelpText();
        }

        protected override bool IsContentElementCore() => false;
        protected override bool IsControlElementCore() => false;
    }
}
