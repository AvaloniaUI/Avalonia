using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public class ScrollBarAutomationPeer : RangeBaseAutomationPeer
    {
        public ScrollBarAutomationPeer(ScrollBar owner) : base(owner)
        {
        }

        override protected string GetClassNameCore()
        {
            return "ScrollBar";
        }

        override protected AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ScrollBar;
        }

        // AutomationControlType.ScrollBar must return IsContentElement false.
        // See http://msdn.microsoft.com/en-us/library/ms743712.aspx
        override protected bool IsContentElementCore()
        {
            return false;
        }

    }
}
