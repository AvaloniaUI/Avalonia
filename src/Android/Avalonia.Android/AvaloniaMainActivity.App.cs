#nullable enable

using Avalonia.Android.Platform;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace Avalonia.Android
{
    partial class AvaloniaMainActivity<TApp> where TApp : Application, new()
    {
        protected virtual AppBuilder CustomizeAppBuilder(AppBuilder builder) => builder.UseAndroid();

        protected AppBuilder CreateAppBuilder()
        {
            var builder = AppBuilder.Configure<TApp>();

            return CustomizeAppBuilder(builder);
        }

        private void InitializeApp()
        {
            // Android can run OnCreate + InitializeApp multiple times per process lifetime.
            // On each call we need to create new AvaloniaView, but we can't recreate Avalonia nor Avalonia controls.
            // So, if lifetime was already created previously - recreate AvaloniaView.
            // If not, initialize Avalonia, and create AvaloniaView inside of AfterSetup callback.
            // We need this AfterSetup callback to match iOS/Browser behavior and ensure that view/toplevel is available in custom AfterSetup calls.
            if (Lifetime is not null)
            {
                Lifetime.View = View = new AvaloniaView(this);
            }
            else
            {
                var builder = CreateAppBuilder();

                var lifetime = new SingleViewLifetime();
                builder
                    .AfterSetup(_ =>
                    {
                        lifetime.View = View = new AvaloniaView(this);
                    })
                    .SetupWithLifetime(lifetime);

                Lifetime = lifetime;
            }

            if (Avalonia.Application.Current?.TryGetFeature<IActivatableLifetime>()
                is AndroidActivatableLifetime activatableLifetime)
            {
                activatableLifetime.Activity = this;
            }
        }
    }
}
