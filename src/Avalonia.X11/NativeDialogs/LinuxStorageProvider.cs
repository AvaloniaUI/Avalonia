using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.FreeDesktop;
using Avalonia.Platform.Storage;

namespace Avalonia.X11.NativeDialogs;

internal class LinuxStorageProvider : IStorageProvider
{
    private readonly X11Window _window;
    public LinuxStorageProvider(X11Window window)
    {
        _window = window;
    }

    public bool CanOpen => true;
    public bool CanSave => true;
    public bool CanPickFolder => true;

    private async Task<IStorageProvider> EnsureStorageProvider()
    {
        var options = AvaloniaLocator.Current.GetService<X11PlatformOptions>() ?? new X11PlatformOptions();

        if (options.UseDBusFilePicker)
        {
            var dBusDialog = await DBusSystemDialog.TryCreate(_window.Handle);
            if (dBusDialog is not null)
            {
                return dBusDialog;
            }
        }
        
        var gtkDialog = await GtkSystemDialog.TryCreate(_window);
        if (gtkDialog is not null)
        {
            return gtkDialog;
        }

        throw new InvalidOperationException("Neither DBus nor GTK are available on the system");
    }

    public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
        var provider = await EnsureStorageProvider().ConfigureAwait(false);
        return await provider.OpenFilePickerAsync(options).ConfigureAwait(false);
    }

    public async Task<IStorageFile> SaveFilePickerAsync(FilePickerSaveOptions options)
    {
        var provider = await EnsureStorageProvider().ConfigureAwait(false);
        return await provider.SaveFilePickerAsync(options).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
    {
        var provider = await EnsureStorageProvider().ConfigureAwait(false);
        return await provider.OpenFolderPickerAsync(options).ConfigureAwait(false);
    }

    public async Task<IStorageBookmarkFile> OpenFileBookmarkAsync(string bookmark)
    {
        var provider = await EnsureStorageProvider().ConfigureAwait(false);
        return await provider.OpenFileBookmarkAsync(bookmark).ConfigureAwait(false);
    }

    public async Task<IStorageBookmarkFolder> OpenFolderBookmarkAsync(string bookmark)
    {
        var provider = await EnsureStorageProvider().ConfigureAwait(false);
        return await provider.OpenFolderBookmarkAsync(bookmark).ConfigureAwait(false);
    }
}
