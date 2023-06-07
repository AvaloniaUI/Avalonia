using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers
{
    public class SliderAutomationPeer : RangeBaseAutomationPeer
    {
        public SliderAutomationPeer(Slider owner) : base(owner)
        {
        }

        override protected string GetClassNameCore()
        {
            return "Slider";
        }

        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Slider;
        }

    }
}
