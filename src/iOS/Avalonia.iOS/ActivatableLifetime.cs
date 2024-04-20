using System;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.iOS;

internal class ActivatableLifetime : ActivatableLifetimeBase
{
    public ActivatableLifetime(IAvaloniaAppDelegate avaloniaAppDelegate)
    {
        avaloniaAppDelegate.Activated += (_, args) => OnActivated(args);
        avaloniaAppDelegate.Deactivated += (_, args) => OnDeactivated(args);
    }
}
