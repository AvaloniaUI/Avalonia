using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
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

        [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<string[]> ShowManagedAsync(this OpenFileDialog dialog, Window parent,
            ManagedFileDialogOptions? options = null) => ShowManagedAsync<Window>(dialog, parent, options);

        [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<string[]> ShowManagedAsync<TWindow>(this OpenFileDialog dialog, Window parent,
            ManagedFileDialogOptions? options = null) where TWindow : Window, new()
        {
            var impl = new ManagedStorageProvider(parent, PrepareOptions(options, () => new TWindow()));

            var files = await impl.OpenFilePickerAsync(dialog.ToFilePickerOpenOptions());
            return files
                .Select(file => file.TryGetLocalPath() ?? file.Name)
                .ToArray();
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
