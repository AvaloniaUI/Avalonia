using System;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;

namespace Avalonia.Native;

#nullable enable

internal class MacOSClassicDesktopStyleApplicationLifetime : ClassicDesktopStyleApplicationLifetime,
    IActivatableApplicationLifetime
{
    /// <inheritdoc />
    public event EventHandler<ActivatedEventArgs>? Activated;
    
    /// <inheritdoc />
    public event EventHandler<ActivatedEventArgs>? Deactivated;

    /// <inheritdoc />
    public bool TryLeaveBackground()
    {
        var nativeApplicationCommands = AvaloniaLocator.Current.GetService<INativeApplicationCommands>();
        nativeApplicationCommands?.ShowApp();

        return true;
    }

    /// <inheritdoc />
    public bool TryEnterBackground()
    {
        var nativeApplicationCommands = AvaloniaLocator.Current.GetService<INativeApplicationCommands>();
        nativeApplicationCommands?.HideApp();

        return true;
    }

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
