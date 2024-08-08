using System;
using Android.OS;
using Avalonia.Android.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace Avalonia.Android;

public class AvaloniaMainActivity : AvaloniaActivity
{
    private protected static SingleViewLifetime? Lifetime;

    private protected override void InitializeAvaloniaView(object? initialContent)
    {
        // Android can run OnCreate + InitializeAvaloniaView multiple times per process lifetime.
        // On each call we need to create new AvaloniaView, but we can't recreate Avalonia nor Avalonia controls.
        // So, if lifetime was already created previously - recreate AvaloniaView.
        // If not, initialize Avalonia, and create AvaloniaView inside of AfterSetup callback.
        // We need this AfterSetup callback to match iOS/Browser behavior and ensure that view/toplevel is available in custom AfterSetup calls.
        if (Lifetime is not null)
        {
            initialContent ??= Lifetime.MainView; 

            Lifetime.Activity = this;
            _view = new AvaloniaView(this) { Content = initialContent };
        }
        else
        {
            var builder = CreateAppBuilder();
            builder = CustomizeAppBuilder(builder);

            Lifetime = new SingleViewLifetime();
            Lifetime.Activity = this;
 
            builder
                .AfterApplicationSetup(_ =>
                {
                    _view = new AvaloniaView(this) { Content = initialContent };
                })
                .SetupWithLifetime(Lifetime);

            // AfterPlatformServicesSetup should always be called. If it wasn't, we have an unusual problem.
            if (_view is null)
                throw new InvalidOperationException("Unknown error: AvaloniaView initialization has failed.");
        }

        if (Avalonia.Application.Current?.TryGetFeature<IActivatableLifetime>()
            is AndroidActivatableLifetime activatableLifetime)
        {
            activatableLifetime.CurrentMainActivity = this;
        }
    }

    protected virtual AppBuilder CreateAppBuilder() => AppBuilder.Configure<Application>().UseAndroid();
    protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder;
}
