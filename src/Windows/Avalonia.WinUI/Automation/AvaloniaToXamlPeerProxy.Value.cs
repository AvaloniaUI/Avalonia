using Microsoft.UI.Xaml.Automation.Provider;
using AvValue = global::Avalonia.Automation.Provider.IValueProvider;

namespace Avalonia.WinUI.Automation;

internal sealed partial class AvaloniaToXamlPeerProxy : IValueProvider
{
    bool IValueProvider.IsReadOnly => _peer.GetProvider<AvValue>()?.IsReadOnly ?? true;

    string IValueProvider.Value => _peer.GetProvider<AvValue>()?.Value ?? string.Empty;

    void IValueProvider.SetValue(string value) => _peer.GetProvider<AvValue>()?.SetValue(value);
}
