using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using AvPeer = global::Avalonia.Automation.Peers.AutomationPeer;
using AvSelection = global::Avalonia.Automation.Provider.ISelectionProvider;
using AvSelectionItem = global::Avalonia.Automation.Provider.ISelectionItemProvider;

namespace Avalonia.WinUI.Automation;

internal sealed partial class AvaloniaToXamlPeerProxy : ISelectionProvider, ISelectionItemProvider
{
    bool ISelectionProvider.CanSelectMultiple => _peer.GetProvider<AvSelection>()?.CanSelectMultiple ?? false;
    bool ISelectionProvider.IsSelectionRequired => _peer.GetProvider<AvSelection>()?.IsSelectionRequired ?? false;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        var provider = _peer.GetProvider<AvSelection>();
        if (provider is null)
            return System.Array.Empty<IRawElementProviderSimple>();

        var list = new List<IRawElementProviderSimple>();
        foreach (var avChild in provider.GetSelection() ?? (IReadOnlyList<AvPeer>)System.Array.Empty<AvPeer>())
        {
            var proxy = GetOrCreate(avChild, _host);
            if (ProviderFromPeer(proxy) is IRawElementProviderSimple simple)
                list.Add(simple);
        }
        return list.ToArray();
    }

    bool ISelectionItemProvider.IsSelected => _peer.GetProvider<AvSelectionItem>()?.IsSelected ?? false;

    IRawElementProviderSimple? ISelectionItemProvider.SelectionContainer
    {
        get
        {
            var container = _peer.GetProvider<AvSelectionItem>()?.SelectionContainer;
            if (container is AvPeer containerPeer)
            {
                var proxy = GetOrCreate(containerPeer, _host);
                return ProviderFromPeer(proxy);
            }
            return null;
        }
    }

    void ISelectionItemProvider.AddToSelection() => _peer.GetProvider<AvSelectionItem>()?.AddToSelection();
    void ISelectionItemProvider.RemoveFromSelection() => _peer.GetProvider<AvSelectionItem>()?.RemoveFromSelection();
    void ISelectionItemProvider.Select() => _peer.GetProvider<AvSelectionItem>()?.Select();
}
