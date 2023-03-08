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

        override protected string? GetNameCore()
        {
            var content = ((Label)Owner).Content as string;

            if (string.IsNullOrEmpty(content))
            {
                return base.GetNameCore();
            }

            return AccessText.RemoveAccessKeyMarker(content) ?? string.Empty;
        }
    }
}
