using System;
using Avalonia.Browser.Interop;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace Avalonia.Browser
{
    internal class BrowserSystemNavigationManagerImpl : ISystemNavigationManagerImpl
    {
        public event EventHandler<RoutedEventArgs>? BackRequested;

        public BrowserSystemNavigationManagerImpl()
        {
            NavigationHelper.AddBackHandler(() =>
            {
                var routedEventArgs = new RoutedEventArgs();

                BackRequested?.Invoke(this, routedEventArgs);

                return routedEventArgs.Handled;
            });
        }
    }
}
