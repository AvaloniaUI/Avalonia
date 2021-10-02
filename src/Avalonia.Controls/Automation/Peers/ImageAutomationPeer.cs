using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ImageAutomationPeer : ControlAutomationPeer
    {
        public ImageAutomationPeer(Control owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Image;
        }
    }
}
