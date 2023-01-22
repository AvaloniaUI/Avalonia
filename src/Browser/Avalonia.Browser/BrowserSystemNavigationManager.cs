using System;
using Avalonia.Browser.Interop;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace Avalonia.Browser
{
    internal class BrowserSystemNavigationManager : ISystemNavigationManager
    {
        public event EventHandler<RoutedEventArgs>? BackRequested;

        public BrowserSystemNavigationManager()
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
