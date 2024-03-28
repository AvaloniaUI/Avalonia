#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Android.OS;
using Avalonia.Android.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace Avalonia.Android;

public abstract class AvaloniaMainActivity : AvaloniaActivity
{
    private protected static SingleViewLifetime? Lifetime;

    public override void OnCreate(Bundle? savedInstanceState, PersistableBundle? persistentState)
    {
        // Global IActivatableLifetime expects a main activity, so we need to replace it on each OnCreate.
        if (Avalonia.Application.Current?.TryGetFeature<IActivatableLifetime>()
            is AndroidActivatableLifetime activatableLifetime)
        {
            activatableLifetime.Activity = this;
        }

        base.OnCreate(savedInstanceState, persistentState);
    }

    private protected override AvaloniaView CreateAvaloniaView()
    {
        // Android can run OnCreate + CreateAvaloniaView multiple times per process lifetime.
        // On each call we need to create new AvaloniaView, but we can't recreate Avalonia nor Avalonia controls.
        // So, if lifetime was already created previously - recreate AvaloniaView.
        // If not, initialize Avalonia, and create AvaloniaView inside of AfterSetup callback.
        // We need this AfterSetup callback to match iOS/Browser behavior and ensure that view/toplevel is available in custom AfterSetup calls.
        if (Lifetime is not null)
        {
            Lifetime.Activity = this;
            return new AvaloniaView(this);
        }
        else
        {
            var builder = CreateAppBuilder();

            Lifetime = new SingleViewLifetime();
            Lifetime.Activity = this;

            AvaloniaView? view = null; 
            builder
                .AfterSetup(_ =>
                {
                    view = new AvaloniaView(this);
                })
                .SetupWithLifetime(Lifetime);

            // AfterSetup should always be called. If it wasn't, we have an unusual problem.
            return view ?? throw new InvalidOperationException("Unknown error: AvaloniaView initialization failed.");
        }
    }

    protected abstract AppBuilder CreateAppBuilder();
}
