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
        _initiallyVisible = InputHelper.SubscribeVisibilityChange(visible =>
        {
            _initiallyVisible = null;
            var eventToTrigger = (visible ? Activated : Deactivated);
            Dispatcher.UIThread.Invoke(
                () => eventToTrigger?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background)),
                DispatcherPriority.Background);
        });
    }
    
    public AvaloniaView? View;
    private bool? _initiallyVisible;

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

    internal void CompleteSetup()
    {
        // Trigger Activated as an initial state, if web page is visible, and wasn't hidden during initialization.
        Dispatcher.UIThread.Invoke(() =>
        {
            if (_initiallyVisible == true)
            {
                Activated?.Invoke(this, new ActivatedEventArgs(ActivationKind.Background));
            }
        }, DispatcherPriority.Background);
    }
}
