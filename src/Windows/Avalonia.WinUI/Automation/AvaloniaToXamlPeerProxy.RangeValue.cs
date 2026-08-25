using Microsoft.UI.Xaml.Automation.Provider;
using AvRange = global::Avalonia.Automation.Provider.IRangeValueProvider;

namespace Avalonia.WinUI.Automation;

internal sealed partial class AvaloniaToXamlPeerProxy : IRangeValueProvider
{
    bool IRangeValueProvider.IsReadOnly => _peer.GetProvider<AvRange>()?.IsReadOnly ?? true;
    double IRangeValueProvider.Minimum => _peer.GetProvider<AvRange>()?.Minimum ?? 0;
    double IRangeValueProvider.Maximum => _peer.GetProvider<AvRange>()?.Maximum ?? 0;
    double IRangeValueProvider.Value => _peer.GetProvider<AvRange>()?.Value ?? 0;
    double IRangeValueProvider.LargeChange => _peer.GetProvider<AvRange>()?.LargeChange ?? 0;
    double IRangeValueProvider.SmallChange => _peer.GetProvider<AvRange>()?.SmallChange ?? 0;

    void IRangeValueProvider.SetValue(double value) => _peer.GetProvider<AvRange>()?.SetValue(value);
}
