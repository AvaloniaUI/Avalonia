#nullable enable

namespace Avalonia.Controls.Automation.Peers
{
    public abstract class ContentControlAutomationPeer : ControlAutomationPeer
    {
        protected ContentControlAutomationPeer(Control owner) : base(owner) { }

        protected override string? GetNameCore()
        {
            var result = base.GetNameCore();

            if (result is null && Owner is ContentControl cc && cc.Presenter?.Child is TextBlock text)
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
