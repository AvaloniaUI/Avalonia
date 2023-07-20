using DryIoc;
using ReactiveUI;
using Splat;
using Splat.DryIoc;

namespace Avalonia.ReactiveUI.Splat
{
    /// <summary>
    /// Avalonia Mixins.
    /// </summary>
    public static class AvaloniaMixins
    {
        /// <summary>
        /// Uses the splat with dry ioc.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="configure">The configure.</param>
        /// <returns>An App Builder.</returns>
        public static AppBuilder UseSplatWithDryIoc(this AppBuilder builder, Action<Container> configure)
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

                var container = new Container();
                Locator.CurrentMutable.RegisterConstant(container, typeof(Container));
                container.UseDryIocDependencyResolver();

                RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;

                configure(container);
            });
        }
    }
}
