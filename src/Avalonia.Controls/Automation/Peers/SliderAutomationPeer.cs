using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class SliderAutomationPeer : RangeBaseAutomationPeer
    {
        public SliderAutomationPeer(Slider owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Slider;
        }
    }
}
