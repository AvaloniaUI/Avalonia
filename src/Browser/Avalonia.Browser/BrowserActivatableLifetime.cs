using System;
using Avalonia.Browser.Interop;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace Avalonia.Browser;

internal class BrowserActivatableLifetime : IActivatableLifetime
{
    public BrowserActivatableLifetime()
    {
        bool? initiallyVisible = InputHelper.SubscribeVisibilityChange(visible =>
        {
            initiallyVisible = null;
            (visible ? Activated : Deactivated)?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background));
        });
    
        // Trigger Activated as an initial state, if web page is visible, and wasn't hidden during initialization.
        if (initiallyVisible == true)
        {
            _ = Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (initiallyVisible == true)
                {
                    Activated?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background));
                }
            }, DispatcherPriority.Background);
        }
    }
    
    public event EventHandler<ActivatedEventArgs>? Activated;
    public event EventHandler<ActivatedEventArgs>? Deactivated;

    public bool TryLeaveBackground() => false;
    public bool TryEnterBackground() => false;
}
