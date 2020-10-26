using System.Collections.Generic;

#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public class MenuItemAutomationPeer : ControlAutomationPeer
    {
        public MenuItemAutomationPeer(Control owner) : base(owner) { }

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;

        protected override string? GetNameCore()
        {
            var result = base.GetNameCore();

            if (result is null && Owner is MenuItem m && m.HeaderPresenter.Child is TextBlock text)
            {
                result = text.Text;
            }

            if (result is null)
            {
                result = Owner.GetValue(ContentControl.ContentProperty)?.ToString();
            }

            return result;
        }
    }
}
