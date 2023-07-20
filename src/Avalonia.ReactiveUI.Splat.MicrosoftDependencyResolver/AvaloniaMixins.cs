using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace Avalonia.ReactiveUI.Splat
{
    /// <summary>
    /// Avalonia Mixins.
    /// </summary>
    public static class AvaloniaMixins
    {
        /// <summary>
        /// Uses the splat with microsoft dependency resolver.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configure">The configure.</param>
        /// <param name="getServiceProvider">The get service provider.</param>
        /// <returns>An App Builder.</returns>
        public static AppBuilder UseSplatWithMicrosoftDependencyResolver(this AppBuilder builder, Action<IServiceCollection> configure, Action<IServiceProvider?>? getServiceProvider = null)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AfterPlatformServicesSetup(_ =>
            {
                if (Locator.CurrentMutable is null)
                {
                    return;
                }

                IServiceCollection services = new ServiceCollection();
                Locator.CurrentMutable.RegisterConstant(services, typeof(IServiceCollection));
                services.UseMicrosoftDependencyResolver();

                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

                configure(services);
                if (getServiceProvider is null)
                {
                    return;
                }

                var serviceProvider = services.BuildServiceProvider();
                serviceProvider.UseMicrosoftDependencyResolver();
                getServiceProvider(serviceProvider);
            });
        }
    }
}
