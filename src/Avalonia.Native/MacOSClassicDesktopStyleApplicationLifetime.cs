using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Native;

#nullable enable

internal class MacOSClassicDesktopStyleApplicationLifetime : ClassicDesktopStyleApplicationLifetime,
    IActivatableApplicationLifetime
{
    public event EventHandler<ActivatedEventArgs>? Activated;
    
    public event EventHandler<ActivatedEventArgs>? Deactivated;

    {
        Activated?.Invoke(this, new ActivatedEventArgs(reason));
    
    internal void RaiseActivated(ActivationKind kind)
    {
        Activated?.Invoke(this, new ActivatedEventArgs(kind));
    }

    internal void RaiseDeactivated(ActivationKind kind)
    {
        Deactivated?.Invoke(this, new ActivatedEventArgs(kind));
    }
}
