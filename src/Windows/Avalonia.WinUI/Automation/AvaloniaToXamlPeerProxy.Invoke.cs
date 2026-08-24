using Microsoft.UI.Xaml.Automation.Provider;
using AvInvoke = global::Avalonia.Automation.Provider.IInvokeProvider;

namespace Avalonia.WinUI.Automation;

internal sealed partial class AvaloniaToXamlPeerProxy : IInvokeProvider
{
    void IInvokeProvider.Invoke() => _peer.GetProvider<AvInvoke>()?.Invoke();
}
