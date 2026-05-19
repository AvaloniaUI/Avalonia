using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class NativeMenuBarAutomationPeer(NativeMenuBar owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.MenuBar;
        }
    }
}
