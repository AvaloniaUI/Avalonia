using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.iOS;

internal class SingleViewLifetime : ISingleViewApplicationLifetime, IActivatableApplicationLifetime
{
    public SingleViewLifetime(IAvaloniaAppDelegate avaloniaAppDelegate)
    {
        avaloniaAppDelegate.Activated += (_, args) => Activated?.Invoke(this, args);
        avaloniaAppDelegate.Deactivated += (_, args) => Deactivated?.Invoke(this, args);
    }
            
    public AvaloniaView? View;

    public Control? MainView
    {
        get => View!.Content;
        set => View!.Content = value;
    }

    public event EventHandler<ActivatedEventArgs>? Activated;
    public event EventHandler<ActivatedEventArgs>? Deactivated;
    public bool TryLeaveBackground() => false;
    public bool TryEnterBackground() => false;
}
