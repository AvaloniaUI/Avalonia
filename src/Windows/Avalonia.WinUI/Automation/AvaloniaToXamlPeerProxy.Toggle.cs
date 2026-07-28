using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using AvToggle = global::Avalonia.Automation.Provider.IToggleProvider;
using AvToggleState = global::Avalonia.Automation.Provider.ToggleState;

namespace Avalonia.WinUI.Automation;

internal sealed partial class AvaloniaToXamlPeerProxy : IToggleProvider
{
    ToggleState IToggleProvider.ToggleState
    {
        get
        {
            var provider = _peer.GetProvider<AvToggle>();
            if (provider is null)
                return ToggleState.Indeterminate;
            return provider.ToggleState switch
            {
                AvToggleState.Off => ToggleState.Off,
                AvToggleState.On => ToggleState.On,
                _ => ToggleState.Indeterminate,
            };
        }
    }

    void IToggleProvider.Toggle() => _peer.GetProvider<AvToggle>()?.Toggle();
}
