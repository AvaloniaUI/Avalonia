using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop
{
    internal class DBusSystemDialog : BclStorageProvider
    {
        internal static async Task<IStorageProvider?> TryCreateAsync(IPlatformHandle handle)
        {
            if (DBusHelper.Connection is null)
                return null;
            var services = await DBusHelper.Connection.ListServicesAsync();
            return services.Contains("org.freedesktop.portal.Desktop", StringComparer.Ordinal)
                ? new DBusSystemDialog(new DesktopService(DBusHelper.Connection, "org.freedesktop.portal.Desktop"), handle)
                : null;
        }

        private readonly DesktopService _desktopService;
        private readonly FileChooser _fileChooser;
        private readonly IPlatformHandle _handle;

        private DBusSystemDialog(DesktopService desktopService, IPlatformHandle handle)
        {
            _desktopService = desktopService;
            _fileChooser = desktopService.CreateFileChooser("/org/freedesktop/portal/desktop");
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
                chooserOptions.Add("filters", filters);

            chooserOptions.Add("multiple", options.AllowMultiple);

            objectPath = await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);

            var request = _desktopService.CreateRequest(objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                    tsc.TrySetException(e);
                else
                    tsc.TrySetResult(x.results["uris"] as string[]);
            });

            var uris = await tsc.Task ?? Array.Empty<string>();
            return uris.Select(static path => new BclStorageFile(new FileInfo(new Uri(path).LocalPath))).ToList();
        }

        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            var parentWindow = $"x11:{_handle.Handle:X}";
            ObjectPath objectPath;
            var chooserOptions = new Dictionary<string, object>();
            var filters = ParseFilters(options.FileTypeChoices);
            if (filters.Any())
                chooserOptions.Add("filters", filters);

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", currentName);
            if (options.SuggestedStartLocation?.TryGetUri(out var currentFolder) == true)
                chooserOptions.Add("current_folder", Encoding.UTF8.GetBytes(currentFolder.ToString()));

            objectPath = await _fileChooser.SaveFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);
            var request = _desktopService.CreateRequest(objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                    tsc.TrySetException(e);
                else
                    tsc.TrySetResult(x.results["uris"] as string[]);
            });

            var uris = await tsc.Task;
            var path = uris?.FirstOrDefault() is { } filePath ? new Uri(filePath).LocalPath : null;

            if (path is null)
                return null;

            // WSL2 freedesktop automatically adds extension from selected file type, but we can't pass "default ext". So apply it manually.
            path = StorageProviderHelpers.NameWithExtension(path, options.DefaultExtension, null);
            return new BclStorageFile(new FileInfo(path));
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
            var request = _desktopService.CreateRequest(objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                    tsc.TrySetException(e);
                else
                    tsc.TrySetResult(x.results["uris"] as string[]);
            });

            var uris = await tsc.Task ?? Array.Empty<string>();
            return uris
                .Select(static path => new Uri(path).LocalPath)
                // WSL2 freedesktop allows to select files as well in directory picker, filter it out.
                .Where(Directory.Exists)
                .Select(static path => new BclStorageFolder(new DirectoryInfo(path))).ToList();
        }

        private static (string name, (uint style, string extension)[])[] ParseFilters(IReadOnlyList<FilePickerFileType>? fileTypes)
        {
            // Example: [('Images', [(0, '*.ico'), (1, 'image/png')]), ('Text', [(0, '*.txt')])]
            if (fileTypes is null)
                return Array.Empty<(string name, (uint style, string extension)[])>();

            var filters = new List<(string name, (uint style, string extension)[])>();
            foreach (var fileType in fileTypes)
            {
                const uint GlobStyle = 0u;
                const uint MimeStyle = 1u;

                var extensions = Enumerable.Empty<(uint, string)>();

                if (fileType.Patterns is not null)
                    extensions = extensions.Concat(fileType.Patterns.Select(static x => (globStyle: GlobStyle, x)));
                else if (fileType.MimeTypes is not null)
                    extensions = extensions.Concat(fileType.MimeTypes.Select(static x => (mimeStyle: MimeStyle, x)));
                if (extensions.Any())
                    filters.Add((fileType.Name, extensions.ToArray()));
            }

            return filters.ToArray();
        }
    }
}
