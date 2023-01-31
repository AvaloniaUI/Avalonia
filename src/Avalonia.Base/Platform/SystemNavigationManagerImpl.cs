using System;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface ISystemNavigationManagerImpl
    {
        public event EventHandler<RoutedEventArgs>? BackRequested;
    }
}
