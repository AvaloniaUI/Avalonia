using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Native;

#nullable enable

internal class MacOSClassicDesktopStyleApplicationLifetime : ClassicDesktopStyleApplicationLifetime,
    IActivatableApplicationLifetime
{
    /// <inheritdoc />
    public event EventHandler<ActivatedEventArgs>? Activated;
    
    /// <inheritdoc />
    public event EventHandler<ActivatedEventArgs>? Deactivated;

    internal void RaiseUrl(Uri uri)
    {
        Activated?.Invoke(this, new ProtocolActivatedEventArgs(ActivationKind.OpenUri, uri));
    }
    
    internal void RaiseActivated(ActivationKind kind)
    {
        Activated?.Invoke(this, new ActivatedEventArgs(kind));
    }

    internal void RaiseDeactivated(ActivationKind kind)
    {
        Deactivated?.Invoke(this, new ActivatedEventArgs(kind));
    }
}
