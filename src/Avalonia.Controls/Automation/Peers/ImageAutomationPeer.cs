using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers
{
    public class ImageAutomationPeer : ControlAutomationPeer
    {
        public ImageAutomationPeer(Control owner) : base(owner)
        {
        }

        override protected string GetClassNameCore()
        {
            return "Image";
        }

        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Image;
        }
    }
}
