using Avalonia.Browser.Interop;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Browser;

internal class BrowserActivatableLifetime : ActivatableLifetimeBase
{
    public BrowserActivatableLifetime()
    {
        OnVisibilityStateChanged(DomHelper.GetCurrentDocumentVisibility(), true);
    }

    public void OnVisibilityStateChanged(string visibilityState, bool initial = false)
    {
        var visible = visibilityState == "visible";
        if (visible)
        {
            OnActivated(ActivationKind.Background);
        }
        else if (!initial)
        {
            OnDeactivated(ActivationKind.Background);
        }
    }
}
