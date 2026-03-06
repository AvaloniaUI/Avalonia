using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class CarouselPageAutomationPeer : ControlAutomationPeer
{
    public CarouselPageAutomationPeer(CarouselPage owner)
        : base(owner)
    {
    }

    public new CarouselPage Owner => (CarouselPage)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.List;

    protected override string? GetNameCore()
    {
        var result = base.GetNameCore();

        if (string.IsNullOrEmpty(result))
            result = Owner.Header?.ToString();

        return result;
    }
}
