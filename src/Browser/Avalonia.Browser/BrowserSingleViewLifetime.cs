using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System.Runtime.Versioning;
using Avalonia.Browser;
using Avalonia.Browser.Interop;
using Avalonia.Threading;

namespace Avalonia;

internal class BrowserSingleViewLifetime : ISingleViewApplicationLifetime, IActivatableApplicationLifetime
{
    public BrowserSingleViewLifetime()
    {
        bool? initiallyVisible = InputHelper.SubscribeVisibilityChange(visible =>
        {
            initiallyVisible = null;
            (visible ? Activated : Deactivated)?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background));
        });
    }
    
    public AvaloniaView? View;

    public Control? MainView
    {
        get
        {
            EnsureView();
            return View.Content;
        }
        set
        {
            EnsureView();
            View.Content = value;
        }
    }

    [MemberNotNull(nameof(View))]
    private void EnsureView()
    {
        if (View is null)
        {
            throw new InvalidOperationException("Browser lifetime was not initialized. Make sure AppBuilder.StartBrowserApp was called.");
        }
    }

    public event EventHandler<ActivatedEventArgs>? Activated;
    public event EventHandler<ActivatedEventArgs>? Deactivated;

    public bool TryLeaveBackground() => false;
    public bool TryEnterBackground() => false;
}
