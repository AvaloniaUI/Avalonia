using System;
using Android.OS;
using Avalonia.Android.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace Avalonia.Android;

public class AvaloniaMainActivity : AvaloniaActivity
{
    private protected override void InitializeAvaloniaView(object? initialContent)
    {
        if (Application is IAndroidApplication application && application.Lifetime is { } lifetime)
        {
            initialContent ??= lifetime.MainViewFactory?.Invoke();

            _view = new AvaloniaView(this);

            Content = initialContent;
        }

        if (_view is null)
            throw new InvalidOperationException("Unknown error: AvaloniaView initialization has failed.");
    }

    protected override void OnResume()
    {
        base.OnResume();

        if (Avalonia.Application.Current?.TryGetFeature<IActivatableLifetime>()
            is AndroidActivatableLifetime activatableLifetime)
        {
            activatableLifetime.CurrentMainActivity = this;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Avalonia.Application.Current?.TryGetFeature<IActivatableLifetime>()
            is AndroidActivatableLifetime activatableLifetime && activatableLifetime.CurrentMainActivity == this)
        {
            activatableLifetime.CurrentMainActivity = null;
        }
    }

    protected virtual AppBuilder CreateAppBuilder() => AppBuilder.Configure<Application>().UseAndroid();
    protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;
}
