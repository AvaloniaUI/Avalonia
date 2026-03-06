using Avalonia.Browser.Interop;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Browser;

internal class BrowserActivatableLifetime : ActivatableLifetimeBase
{
    public void OnVisibilityStateChanged(string visibilityState)
    {
        var visible = visibilityState == "visible";
        if (visible)
        {
            OnActivated(ActivationKind.Background);
        }
        else
        {
            OnDeactivated(ActivationKind.Background);
        }
    }
}
