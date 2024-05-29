using System;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace Avalonia.Browser;

internal class BrowserSystemNavigationManagerImpl : ISystemNavigationManagerImpl
{
    public event EventHandler<RoutedEventArgs>? BackRequested;

    public bool OnBackRequested()
    {
        var routedEventArgs = new RoutedEventArgs();

        BackRequested?.Invoke(this, routedEventArgs);

        return routedEventArgs.Handled;
    }
}
