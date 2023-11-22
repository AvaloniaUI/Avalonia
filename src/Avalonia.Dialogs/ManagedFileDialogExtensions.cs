using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
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
            private readonly Func<ContentControl>? _customFactory;

            public ManagedStorageProviderFactory(Func<ContentControl>? customFactory)
            {
                _customFactory = customFactory;
            }
            
            public IStorageProvider CreateProvider(TopLevel topLevel)
            {
                var options = AvaloniaLocator.Current.GetService<ManagedFileDialogOptions>();
                return new ManagedStorageProvider(topLevel, _customFactory, options);
            }
        }

        public static AppBuilder UseManagedSystemDialogs(this AppBuilder builder)
        {
            builder.AfterSetup(_ =>
                AvaloniaLocator.CurrentMutable.Bind<IStorageProviderFactory>().ToConstant(new ManagedStorageProviderFactory(null)));
            return builder;
        }

        public static AppBuilder UseManagedSystemDialogs<TWindow>(this AppBuilder builder)
            where TWindow : Window, new()
        {
            builder.AfterSetup(_ =>
                AvaloniaLocator.CurrentMutable.Bind<IStorageProviderFactory>().ToConstant(new ManagedStorageProviderFactory(() => new TWindow())));
            return builder;
        }

        [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<string[]> ShowManagedAsync(this OpenFileDialog dialog, Window parent,
            ManagedFileDialogOptions? options = null) => ShowManagedAsync<Window>(dialog, parent, options);

        [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API"), EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<string[]> ShowManagedAsync<TWindow>(this OpenFileDialog dialog, Window parent,
            ManagedFileDialogOptions? options = null) where TWindow : Window, new()
        {
            var impl = new ManagedStorageProvider(parent, () => new TWindow(), options);

            var files = await impl.OpenFilePickerAsync(dialog.ToFilePickerOpenOptions());
            return files
                .Select(file => file.TryGetLocalPath() ?? file.Name)
                .ToArray();
        }
    }
}
