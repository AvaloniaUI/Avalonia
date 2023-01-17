using System;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface ITopLevelWithSystemNavigationManager
    {
        ISystemNavigationManager SystemNavigationManager { get; }
    }

    [Unstable]
    public interface ISystemNavigationManager
    {
        public event EventHandler<RoutedEventArgs>? BackRequested;
    }
}
