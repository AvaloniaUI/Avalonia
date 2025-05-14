using Avalonia.Controls.ApplicationLifetimes;
using UIKit;

namespace Avalonia.iOS;

/// <inheritdoc cref="IPlatformSingleViewApplicationLifetime"/>
public interface IUIViewControllerApplicationLifetime : IPlatformSingleViewApplicationLifetime<UIViewController>;

internal class UIViewControllerLifetime : IUIViewControllerApplicationLifetime
{
    public required UIWindow Window { get; init; }

    public UIViewController? PlatformView
    { 
        get => Window.RootViewController; 
        set => Window.RootViewController = value; 
    }
}
