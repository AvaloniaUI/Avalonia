using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public class MenuItemAutomationPeer : ControlAutomationPeer
    {
        public MenuItemAutomationPeer(MenuItem owner)
            : base(owner) 
        { 
        }

        public new MenuItem Owner => (MenuItem)base.Owner;

        protected override string? GetAccessKeyCore()
        {
            var result = base.GetAccessKeyCore();

            if (string.IsNullOrWhiteSpace(result))
            {
                if (Owner.HeaderPresenter?.Child is AccessText accessText)
                {
                    result = accessText.AccessKey.ToString();
                }
            }

            return result;
        }

        protected override string? GetAcceleratorKeyCore()
        {
            var result = base.GetAcceleratorKeyCore();

            if (string.IsNullOrWhiteSpace(result))
            {
                result = Owner.InputGesture?.ToString();
            }

            return result;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.MenuItem;
        }

        protected override string? GetNameCore()
        {
            var result = base.GetNameCore();

            if (result is null && Owner.Header is string header)
            {
                result = AccessText.RemoveAccessKeyMarker(header);
            }

            return result;
        }
    }
}
