using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

using Tmds.DBus;

namespace Avalonia.FreeDesktop
{
    internal class DBusSystemDialog : BclStorageProvider
    {
        private static readonly Lazy<IFileChooser?> s_fileChooser = new(() => DBusHelper.Connection?
            .CreateProxy<IFileChooser>("org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop"));

        internal static async Task<IStorageProvider?> TryCreate(IPlatformHandle handle)
        {
            if (handle.HandleDescriptor == "XID" && s_fileChooser.Value is { } fileChooser)
            {
                try
                {
                    await fileChooser.GetVersionAsync();
                    return new DBusSystemDialog(fileChooser, handle);
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, LogArea.X11Platform)?.Log(null, $"Unable to connect to org.freedesktop.portal.Desktop: {e.Message}");
                    return null;
                }
            }

            return null;
        }

        private readonly IFileChooser _fileChooser;
        private readonly IPlatformHandle _handle;

        private DBusSystemDialog(IFileChooser fileChooser, IPlatformHandle handle)
        {
            _fileChooser = fileChooser;
            _handle = handle;
        }

        public override bool CanOpen => true;

        public override bool CanSave => true;

        public override bool CanPickFolder => true;

        public override async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            var parentWindow = $"x11:{_handle.Handle:X}";
            ObjectPath objectPath;
            var chooserOptions = new Dictionary<string, object>();
            var filters = ParseFilters(options.FileTypeFilter);
            if (filters.Any())
            {
                chooserOptions.Add("filters", filters);
            }

            chooserOptions.Add("multiple", options.AllowMultiple);

            objectPath = await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);

            var request = DBusHelper.Connection!.CreateProxy<IRequest>("org.freedesktop.portal.Request", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync(x => tsc.SetResult(x.results["uris"] as string[]), tsc.SetException);
            var uris = await tsc.Task ?? Array.Empty<string>();

            return uris.Select(path => new BclStorageFile(new FileInfo(new Uri(path).LocalPath))).ToList();
        }

        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            var parentWindow = $"x11:{_handle.Handle:X}";
            ObjectPath objectPath;
            var chooserOptions = new Dictionary<string, object>();
            var filters = ParseFilters(options.FileTypeChoices);
            if (filters.Any())
            {
                chooserOptions.Add("filters", filters);
            }

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", currentName);
            if (options.SuggestedStartLocation?.TryGetUri(out var currentFolder) == true)
                chooserOptions.Add("current_folder", Encoding.UTF8.GetBytes(currentFolder.ToString()));
            objectPath = await _fileChooser.SaveFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);

            var request = DBusHelper.Connection!.CreateProxy<IRequest>("org.freedesktop.portal.Request", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync(x => tsc.SetResult(x.results["uris"] as string[]), tsc.SetException);
            var uris = await tsc.Task;
            var path = uris?.FirstOrDefault() is { } filePath ? new Uri(filePath).LocalPath : null;

            if (path is null)
            {
                return null;
            }
            else
            {
                // WSL2 freedesktop automatically adds extension from selected file type, but we can't pass "default ext". So apply it manually.
                path = StorageProviderHelpers.NameWithExtension(path, options.DefaultExtension, null);

                return new BclStorageFile(new FileInfo(path));
            }
        }

        public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            var parentWindow = $"x11:{_handle.Handle:X}";
            var chooserOptions = new Dictionary<string, object>
            {
                { "directory", true },
                { "multiple", options.AllowMultiple }
            };
            var objectPath = await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);
            var request = DBusHelper.Connection!.CreateProxy<IRequest>("org.freedesktop.portal.Request", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync(x => tsc.SetResult(x.results["uris"] as string[]), tsc.SetException);
            var uris = await tsc.Task ?? Array.Empty<string>();

            return uris
                .Select(path => new Uri(path).LocalPath)
                // WSL2 freedesktop allows to select files as well in directory picker, filter it out.
                .Where(Directory.Exists)
                .Select(path => new BclStorageFolder(new DirectoryInfo(path))).ToList();
        }

        private static (string name, (uint style, string extension)[])[] ParseFilters(IReadOnlyList<FilePickerFileType>? fileTypes)
        {
            // Example: [('Images', [(0, '*.ico'), (1, 'image/png')]), ('Text', [(0, '*.txt')])]

            if (fileTypes is null)
            {
                return Array.Empty<(string name, (uint style, string extension)[])>();
            }

            var filters = new List<(string name, (uint style, string extension)[])>();
            foreach (var fileType in fileTypes)
            {
                const uint globStyle = 0u;
                const uint mimeStyle = 1u;

                var extensions = Enumerable.Empty<(uint, string)>();

                if (fileType.Patterns is { } patterns)
                {
                    extensions = extensions.Concat(patterns.Select(static x => (globStyle, x)));
                }
                else if (fileType.MimeTypes is { } mimeTypes)
                {
                    extensions = extensions.Concat(mimeTypes.Select(static x => (mimeStyle, x)));
                }

                if (extensions.Any())
                {
                    filters.Add((fileType.Name, extensions.ToArray()));
                }
            }

            return filters.ToArray();
        }
    }
}
