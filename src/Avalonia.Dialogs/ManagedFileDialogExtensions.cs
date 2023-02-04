#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Platform.Storage;

namespace Avalonia.Dialogs
{
    public static class ManagedFileDialogExtensions
    {
        internal class ManagedStorageProviderFactory<T> : IStorageProviderFactory where T : Window, new()
        {
            public IStorageProvider CreateProvider(TopLevel topLevel)
            {
                if (topLevel is Window window)
                {
                    var options = AvaloniaLocator.Current.GetService<ManagedFileDialogOptions>();
                    return new ManagedStorageProvider<T>(window, options);
                }
                throw new InvalidOperationException("Current platform doesn't support managed picker dialogs");
            }
        }

        public static AppBuilder UseManagedSystemDialogs(this AppBuilder builder)
        {
            builder.AfterSetup(_ =>
                AvaloniaLocator.CurrentMutable.Bind<IStorageProviderFactory>().ToSingleton<ManagedStorageProviderFactory<Window>>());
            return builder;
        }

        public static AppBuilder UseManagedSystemDialogs<TWindow>(this AppBuilder builder)
            where TWindow : Window, new()
        {
            builder.AfterSetup(_ =>
                AvaloniaLocator.CurrentMutable.Bind<IStorageProviderFactory>().ToSingleton<ManagedStorageProviderFactory<TWindow>>());
            return builder;
        }

        [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API")]
        public static Task<string[]> ShowManagedAsync(this OpenFileDialog dialog, Window parent,
            ManagedFileDialogOptions? options = null) => ShowManagedAsync<Window>(dialog, parent, options);

        [Obsolete("Use Window.StorageProvider API or TopLevel.StorageProvider API")]
        public static async Task<string[]> ShowManagedAsync<TWindow>(this OpenFileDialog dialog, Window parent,
            ManagedFileDialogOptions? options = null) where TWindow : Window, new()
        {
            var impl = new ManagedStorageProvider<TWindow>(parent, options);

            var files = await impl.OpenFilePickerAsync(dialog.ToFilePickerOpenOptions());
            return files
                .Select(file => file.TryGetFullPath() ?? file.Name)
                .ToArray();
        }
    }
}
