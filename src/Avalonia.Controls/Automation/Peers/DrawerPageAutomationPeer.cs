using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class DrawerPageAutomationPeer : ControlAutomationPeer,
    IExpandCollapseProvider
{
    public DrawerPageAutomationPeer(DrawerPage owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    public new DrawerPage Owner => (DrawerPage)base.Owner;

    public ExpandCollapseState ExpandCollapseState => ToState(Owner.IsOpen);
    public bool ShowsMenu => false;
    public void Collapse() => Owner.IsOpen = false;
    public void Expand() => Owner.IsOpen = true;

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Pane;

    protected override string? GetNameCore()
    {
        var result = base.GetNameCore();

        if (string.IsNullOrEmpty(result))
            result = Owner.Header?.ToString();

        return result;
    }

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == DrawerPage.IsOpenProperty)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                ToState(e.GetOldValue<bool>()),
                ToState(e.GetNewValue<bool>()));
        }
    }

    private static ExpandCollapseState ToState(bool value)
    {
        return value ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
    }
}
