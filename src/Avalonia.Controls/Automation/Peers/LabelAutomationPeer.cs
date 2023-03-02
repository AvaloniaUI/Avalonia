using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls.Automation.Peers
{
    public class LabelAutomationPeer : ControlAutomationPeer
    {
        public LabelAutomationPeer(Label owner) : base(owner)
        {
        }

        override protected string GetClassNameCore()
        {
            return "Text";
        }

        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Text;
        }

        override protected string GetNameCore()
        {
            return AccessText.RemoveAccessKeyMarker(((Label)Owner).Content as string) ?? string.Empty;
        }
    }
}
