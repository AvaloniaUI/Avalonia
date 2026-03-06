using System;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform.Storage;

namespace Avalonia.Dialogs
{
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows"), SupportedOSPlatform("macos"), SupportedOSPlatform("linux")]
#endif
    public static class ManagedFileDialogExtensions
    {
        internal class ManagedStorageProviderFactory : IStorageProviderFactory
        {
            private readonly ManagedFileDialogOptions? _options;

            public ManagedStorageProviderFactory(ManagedFileDialogOptions? options)
            {
                _options = options;
            }
            
            public IStorageProvider CreateProvider(TopLevel topLevel)
            {
                return new ManagedStorageProvider(topLevel, _options);
            }
        }
        
        public static AppBuilder UseManagedSystemDialogs(this AppBuilder builder)
        {
            return builder.UseManagedSystemDialogs(null);
        }

        public static AppBuilder UseManagedSystemDialogs<TWindow>(this AppBuilder builder)
            where TWindow : Window, new()
        {
            return builder.UseManagedSystemDialogs(() => new TWindow());
        }

        private static ManagedFileDialogOptions? PrepareOptions(
            ManagedFileDialogOptions? optionsOverride = null,
            Func<ContentControl>? customRootFactory = null)
        {
            var options = optionsOverride ?? AvaloniaLocator.Current.GetService<ManagedFileDialogOptions>();
            if (options is not null && customRootFactory is not null)
            {
                options = options with { ContentRootFactory = customRootFactory };
            }

            return options;
        }

        private static AppBuilder UseManagedSystemDialogs(this AppBuilder builder, Func<ContentControl>? customFactory)
        {
            builder.AfterSetup(_ =>
            {
                var options = PrepareOptions(null, customFactory);
                AvaloniaLocator.CurrentMutable.Bind<IStorageProviderFactory>()
                    .ToConstant(new ManagedStorageProviderFactory(options));
                if (options?.CustomVolumeInfoProvider is not null)
                {
                    AvaloniaLocator.CurrentMutable.Bind<IMountedVolumeInfoProvider>()
                        .ToConstant(options.CustomVolumeInfoProvider);
                }
            });
            return builder;
        }
    }
}
