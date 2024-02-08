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
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop
{
    internal class DBusSystemDialog : BclStorageProvider
    {
        internal static async Task<IStorageProvider?> TryCreateAsync(IPlatformHandle handle)
        {
            if (DBusHelper.Connection is null)
                return null;

            var dbusFileChooser = new OrgFreedesktopPortalFileChooser(DBusHelper.Connection, "org.freedesktop.portal.Desktop", "/org/freedesktop/portal/desktop");
            uint version;
            try
            {
                version = await dbusFileChooser.GetVersionPropertyAsync();
            }
            catch
            {
                return null;
            }

            return new DBusSystemDialog(DBusHelper.Connection, handle, dbusFileChooser, version);
        }

        private readonly Connection _connection;
        private readonly OrgFreedesktopPortalFileChooser _fileChooser;
        private readonly IPlatformHandle _handle;
        private readonly uint _version;

        private DBusSystemDialog(Connection connection, IPlatformHandle handle, OrgFreedesktopPortalFileChooser fileChooser, uint version)
        {
            _connection = connection;
            _fileChooser = fileChooser;
            _handle = handle;
            _version = version;
        }

        public override bool CanOpen => true;

        public override bool CanSave => true;

        public override bool CanPickFolder => _version >= 3;

        public override async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            var parentWindow = $"x11:{_handle.Handle:X}";
            ObjectPath objectPath;
            var chooserOptions = new Dictionary<string, DBusVariantItem>();
            var filters = ParseFilters(options.FileTypeFilter);
            if (filters is not null)
                chooserOptions.Add("filters", filters);

            if (options.SuggestedStartLocation?.TryGetLocalPath()  is { } folderPath)
                chooserOptions.Add("current_folder", new DBusVariantItem("ay", new DBusByteArrayItem(Encoding.UTF8.GetBytes(folderPath + "\0"))));
            chooserOptions.Add("multiple", new DBusVariantItem("b", new DBusBoolItem(options.AllowMultiple)));

            objectPath = await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);

            var request = new OrgFreedesktopPortalRequest(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                    tsc.TrySetException(e);
                else
                    tsc.TrySetResult((x.results["uris"].Value as DBusArrayItem)?.Select(static y => (y as DBusStringItem)!.Value).ToArray());
            });

            var uris = await tsc.Task ?? Array.Empty<string>();
            return uris.Select(static path => new BclStorageFile(new FileInfo(new Uri(path).LocalPath))).ToList();
        }

        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            var parentWindow = $"x11:{_handle.Handle:X}";
            ObjectPath objectPath;
            var chooserOptions = new Dictionary<string, DBusVariantItem>();
            var filters = ParseFilters(options.FileTypeChoices);
            if (filters is not null)
                chooserOptions.Add("filters", filters);

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", new DBusVariantItem("s", new DBusStringItem(currentName)));
            if (options.SuggestedStartLocation?.TryGetLocalPath()  is { } folderPath)
                chooserOptions.Add("current_folder", new DBusVariantItem("ay", new DBusByteArrayItem(Encoding.UTF8.GetBytes(folderPath + "\0"))));

            objectPath = await _fileChooser.SaveFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);
            var request = new OrgFreedesktopPortalRequest(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                    tsc.TrySetException(e);
                else
                    tsc.TrySetResult((x.results["uris"].Value as DBusArrayItem)?.Select(static y => (y as DBusStringItem)!.Value).ToArray());
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
            if (_version < 3)
                return Array.Empty<IStorageFolder>();

            var parentWindow = $"x11:{_handle.Handle:X}";
            var chooserOptions = new Dictionary<string, DBusVariantItem>
            {
                { "directory", new DBusVariantItem("b", new DBusBoolItem(true)) },
                { "multiple", new DBusVariantItem("b", new DBusBoolItem(options.AllowMultiple)) }
            };

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", new DBusVariantItem("s", new DBusStringItem(currentName)));
            if (options.SuggestedStartLocation?.TryGetLocalPath()  is { } folderPath)
                chooserOptions.Add("current_folder", new DBusVariantItem("ay", new DBusByteArrayItem(Encoding.UTF8.GetBytes(folderPath + "\0"))));

            var objectPath = await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);
            var request = new OrgFreedesktopPortalRequest(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                    tsc.TrySetException(e);
                else
                    tsc.TrySetResult((x.results["uris"].Value as DBusArrayItem)?.Select(static y => (y as DBusStringItem)!.Value).ToArray());
            });

            var uris = await tsc.Task ?? Array.Empty<string>();
            return uris
                .Select(static path => new Uri(path).LocalPath)
                // WSL2 freedesktop allows to select files as well in directory picker, filter it out.
                .Where(Directory.Exists)
                .Select(static path => new BclStorageFolder(new DirectoryInfo(path))).ToList();
        }

        private static DBusVariantItem? ParseFilters(IReadOnlyList<FilePickerFileType>? fileTypes)
        {
            const uint GlobStyle = 0u;
            const uint MimeStyle = 1u;

            // Example: [('Images', [(0, '*.ico'), (1, 'image/png')]), ('Text', [(0, '*.txt')])]
            if (fileTypes is null)
                return null;

            var filters = new List<DBusItem>();

            foreach (var fileType in fileTypes)
            {
                var extensions = new List<DBusItem>();
                if (fileType.Patterns?.Count > 0)
                    extensions.AddRange(
                        fileType.Patterns.Select(static pattern =>
                            new DBusStructItem(new DBusItem[] { new DBusUInt32Item(GlobStyle), new DBusStringItem(pattern) })));
                else if (fileType.MimeTypes?.Count > 0)
                    extensions.AddRange(
                        fileType.MimeTypes.Select(static mimeType =>
                            new DBusStructItem(new DBusItem[] { new DBusUInt32Item(MimeStyle), new DBusStringItem(mimeType) })));
                else
                    continue;

                filters.Add(new DBusStructItem(
                    new DBusItem[]
                    {
                        new DBusStringItem(fileType.Name),
                        new DBusArrayItem(DBusType.Struct, extensions)
                    }));
            }

            return filters.Count > 0 ? new DBusVariantItem("a(sa(us))", new DBusArrayItem(DBusType.Struct, filters)) : null;
        }
    }
}
