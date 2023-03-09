using System;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace Avalonia.Android.Platform
{
    internal class AndroidSystemNavigationManagerImpl : ISystemNavigationManagerImpl
    {
        public event EventHandler<RoutedEventArgs> BackRequested;

        public AndroidSystemNavigationManagerImpl(IActivityNavigationService? navigationService)
        {
            if(navigationService != null)
            {
                navigationService.BackRequested += OnBackRequested;
            }
        }

        private void OnBackRequested(object sender, AndroidBackRequestedEventArgs e)
        {
            var routedEventArgs = new RoutedEventArgs();

            BackRequested?.Invoke(this, routedEventArgs);

            e.Handled = routedEventArgs.Handled;
        }
    }
}
