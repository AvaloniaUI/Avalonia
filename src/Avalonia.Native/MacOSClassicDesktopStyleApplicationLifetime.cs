using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Native;

#nullable enable

internal class MacOSClassicDesktopStyleApplicationLifetime : ClassicDesktopStyleApplicationLifetime,
    IActivatableApplicationLifetime
{
    public event EventHandler<ActivatedEventArgs>? Activated;
    
    public event EventHandler<ActivatedEventArgs>? Deactivated;

    internal void RaiseActivated(ActivationReason reason)
    {
        Activated?.Invoke(this, new ActivatedEventArgs(reason));
    }

    internal void RaiseDeactivated(ActivationReason reason)
    {
        Deactivated?.Invoke(this, new ActivatedEventArgs(reason));
    }
}
