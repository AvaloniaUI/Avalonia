using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.iOS;

internal class ActivatableLifetime : IActivatableLifetime
{
    public ActivatableLifetime(IAvaloniaAppDelegate avaloniaAppDelegate)
    {
        avaloniaAppDelegate.Activated += (_, args) => Activated?.Invoke(this, args);
        avaloniaAppDelegate.Deactivated += (_, args) => Deactivated?.Invoke(this, args);
    }

    public event EventHandler<ActivatedEventArgs>? Activated;
    public event EventHandler<ActivatedEventArgs>? Deactivated;
    public bool TryLeaveBackground() => false;
    public bool TryEnterBackground() => false;
}
