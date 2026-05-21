using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using AvScroll = global::Avalonia.Automation.Provider.IScrollProvider;
using AvScrollAmount = global::Avalonia.Automation.Provider.ScrollAmount;

namespace Avalonia.WinUI.Automation;

internal sealed partial class AvaloniaToXamlPeerProxy : IScrollProvider, IScrollItemProvider
{
    bool IScrollProvider.HorizontallyScrollable => _peer.GetProvider<AvScroll>()?.HorizontallyScrollable ?? false;
    double IScrollProvider.HorizontalScrollPercent => _peer.GetProvider<AvScroll>()?.HorizontalScrollPercent ?? -1;
    double IScrollProvider.HorizontalViewSize => _peer.GetProvider<AvScroll>()?.HorizontalViewSize ?? 100;
    bool IScrollProvider.VerticallyScrollable => _peer.GetProvider<AvScroll>()?.VerticallyScrollable ?? false;
    double IScrollProvider.VerticalScrollPercent => _peer.GetProvider<AvScroll>()?.VerticalScrollPercent ?? -1;
    double IScrollProvider.VerticalViewSize => _peer.GetProvider<AvScroll>()?.VerticalViewSize ?? 100;

    void IScrollProvider.Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        => _peer.GetProvider<AvScroll>()?.Scroll(ToAvalonia(horizontalAmount), ToAvalonia(verticalAmount));

    void IScrollProvider.SetScrollPercent(double horizontalPercent, double verticalPercent)
        => _peer.GetProvider<AvScroll>()?.SetScrollPercent(horizontalPercent, verticalPercent);

    // IScrollItemProvider is satisfied by Avalonia's peer-level BringIntoView — Avalonia has
    // no separate IScrollItemProvider interface; every peer can be brought into view.
    void IScrollItemProvider.ScrollIntoView() => _peer.BringIntoView();

    private static AvScrollAmount ToAvalonia(ScrollAmount amount) => amount switch
    {
        ScrollAmount.LargeDecrement => AvScrollAmount.LargeDecrement,
        ScrollAmount.SmallDecrement => AvScrollAmount.SmallDecrement,
        ScrollAmount.LargeIncrement => AvScrollAmount.LargeIncrement,
        ScrollAmount.SmallIncrement => AvScrollAmount.SmallIncrement,
        _ => AvScrollAmount.NoAmount,
    };
}
