using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class CheckBoxAutomationPeer : ToggleButtonAutomationPeer
    {
        public CheckBoxAutomationPeer(CheckBox owner)
            : base(owner) 
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.CheckBox;
        }
    }
}
