using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using AvExpand = global::Avalonia.Automation.Provider.IExpandCollapseProvider;
using AvExpandState = global::Avalonia.Automation.ExpandCollapseState;

namespace Avalonia.WinUI.Automation;

internal sealed partial class AvaloniaToXamlPeerProxy : IExpandCollapseProvider
{
    ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState
    {
        get
        {
            var provider = _peer.GetProvider<AvExpand>();
            if (provider is null)
                return ExpandCollapseState.LeafNode;
            return provider.ExpandCollapseState switch
            {
                AvExpandState.Collapsed => ExpandCollapseState.Collapsed,
                AvExpandState.Expanded => ExpandCollapseState.Expanded,
                AvExpandState.PartiallyExpanded => ExpandCollapseState.PartiallyExpanded,
                _ => ExpandCollapseState.LeafNode,
            };
        }
    }

    void IExpandCollapseProvider.Expand() => _peer.GetProvider<AvExpand>()?.Expand();
    void IExpandCollapseProvider.Collapse() => _peer.GetProvider<AvExpand>()?.Collapse();
}
