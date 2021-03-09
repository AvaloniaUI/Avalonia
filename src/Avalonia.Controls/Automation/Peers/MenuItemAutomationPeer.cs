using Avalonia.Automation.Platform;
using Avalonia.Controls;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class MenuItemAutomationPeer : ControlAutomationPeer
    {
        public MenuItemAutomationPeer(IAutomationNodeFactory factory, MenuItem owner)
            : base(factory, owner) 
        { 
        }

        public new MenuItem Owner => (MenuItem)base.Owner;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.MenuItem;
        }

        protected override string? GetNameCore()
        {
            var result = base.GetNameCore();

            if (result is null && Owner.HeaderPresenter.Child is TextBlock text)
            {
                result = text.Text;
            }

            if (result is null)
            {
                result = Owner.Header?.ToString();
            }

            return result;
        }
    }
}
