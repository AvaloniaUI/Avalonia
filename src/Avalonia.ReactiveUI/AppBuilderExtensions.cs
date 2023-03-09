using Avalonia.Controls;
using Avalonia.Threading;
using ReactiveUI;
using Splat;

namespace Avalonia.ReactiveUI
{
    public static class AppBuilderExtensions
    {
        /// <summary>
        /// Initializes ReactiveUI framework to use with Avalonia. Registers Avalonia 
        /// scheduler, an activation for view fetcher, a template binding hook. Remember
        /// to call this method if you are using ReactiveUI in your application.
        /// </summary>
        public static AppBuilder UseReactiveUI(this AppBuilder builder) =>
            builder.AfterPlatformServicesSetup(_ => Locator.RegisterResolverCallbackChanged(() =>
            {
                if (Locator.CurrentMutable is null)
                {
                    return;
                }

                PlatformRegistrationManager.SetRegistrationNamespaces(RegistrationNamespace.Avalonia);
                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
                Locator.CurrentMutable.RegisterConstant(new AvaloniaActivationForViewFetcher(), typeof(IActivationForViewFetcher));
                Locator.CurrentMutable.RegisterConstant(new AutoDataTemplateBindingHook(), typeof(IPropertyBindingHook));
            }));
    }
}
