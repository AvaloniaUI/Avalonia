using System.Data;
using System.Linq;
using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers
{
    public class DataValidationErrorsAutomationPeer(DataValidationErrors owner) : ContentControlAutomationPeer(owner)
    {
        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;

        protected override string? GetNameCore()
        {
            return owner.Owner?.Name;
        }

        protected override string? GetHelpTextCore()
        {
            if (owner.Owner is not null)
            {
                var errors = DataValidationErrors.GetErrors(owner.Owner);
                var errorsStringList = errors?.Select(x => x.ToString());
                return errorsStringList != null ? string.Join("\n", errorsStringList.ToArray()) : string.Empty;
            }

            return string.Empty;
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }
    }
}
