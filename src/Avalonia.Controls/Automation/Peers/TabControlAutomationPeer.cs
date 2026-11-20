using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

/// <summary>
/// An automation peer for <see cref="TabControl"/>. It exposes the selection pattern, which UI Automation
/// requires for the Tab control type.
/// </summary>
public class TabControlAutomationPeer : SelectingItemsControlAutomationPeer
{
    public TabControlAutomationPeer(TabControl owner)
        : base(owner)
    {
    }
}
