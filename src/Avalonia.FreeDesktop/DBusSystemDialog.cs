using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Threading;
using Avalonia.DBus;
using Avalonia.FreeDesktop.DBusXml;

namespace Avalonia.FreeDesktop
{
    internal class DBusSystemDialog : BclStorageProvider
    {
        internal static async Task<IStorageProvider?> TryCreateAsync(IPlatformHandle handle)
        {
            if (DBusHelper.DefaultConnection is not { } conn)
                return null;

            using var restoreContext = AvaloniaSynchronizationContext.Ensure(DispatcherPriority.Input);

            var dbusFileChooser = new OrgFreedesktopPortalFileChooserProxy(conn, "org.freedesktop.portal.Desktop",
                new DBusObjectPath("/org/freedesktop/portal/desktop"));
            uint version;
            try
            {
                version = await dbusFileChooser.GetVersionPropertyAsync();
            }
            catch
            {
                return null;
            }

            return new DBusSystemDialog(conn, handle, dbusFileChooser, version);
        }

        private readonly DBusConnection _connection;
        private readonly OrgFreedesktopPortalFileChooserProxy _fileChooser;
        private readonly IPlatformHandle _handle;
        private readonly uint _version;

        private DBusSystemDialog(DBusConnection connection, IPlatformHandle handle,
            OrgFreedesktopPortalFileChooserProxy fileChooser, uint version)
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
            DBusObjectPath objectPath;
            var chooserOptions = new Dictionary<string, DBusVariant>();

            if (TryParseFilters(options.FileTypeFilter, options.SuggestedFileType, out var filters,
                    out var currentFilter))
            {
                chooserOptions.Add("filters", filters);
                if (currentFilter is { } filter)
                    chooserOptions.Add("current_filter", filter);
            }

            if (options.SuggestedStartLocation?.TryGetLocalPath() is { } folderPath)
                chooserOptions.Add("current_folder", new DBusVariant(new List<byte>(Encoding.UTF8.GetBytes(folderPath + "\0"))));

            chooserOptions.Add("multiple", new DBusVariant(options.AllowMultiple));

            objectPath = await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);

            var request =
                new OrgFreedesktopPortalRequestProxy(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((response, results) =>
            {
                if (response != 0)
                    tsc.TrySetResult(null);
                else
                    tsc.TrySetResult(((List<string>)results["uris"].Value).ToArray());
            });

            var uris = await tsc.Task ?? [];
            return uris.Select(static path => new BclStorageFile(new FileInfo(new Uri(path).LocalPath))).ToList();
        }

        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            var (file, _) = await SaveFilePickerCoreAsync(options).ConfigureAwait(false);
            return file;
        }

        public override async Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
        {
            var (file, selectedType) = await SaveFilePickerCoreAsync(options).ConfigureAwait(false);
            return new SaveFilePickerResult { File = file, SelectedFileType = selectedType };
        }

        private async Task<(IStorageFile? file, FilePickerFileType? selectedType)> SaveFilePickerCoreAsync(
            FilePickerSaveOptions options)
        {
            var parentWindow = $"x11:{_handle.Handle:X}";
            DBusObjectPath objectPath;
            var chooserOptions = new Dictionary<string, DBusVariant>();
            if (TryParseFilters(options.FileTypeChoices, options.SuggestedFileType, out var filters,
                    out var currentFilter))
            {
                chooserOptions.Add("filters", filters);
                if (currentFilter is { } filter)
                    chooserOptions.Add("current_filter", filter);
            }

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", new DBusVariant(currentName));
            if (options.SuggestedStartLocation?.TryGetLocalPath() is { } folderPath)
                chooserOptions.Add("current_folder", new DBusVariant(new List<byte>(Encoding.UTF8.GetBytes(folderPath + "\0"))));

            objectPath = await _fileChooser.SaveFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions)
                .ConfigureAwait(false);
            var request =
                new OrgFreedesktopPortalRequestProxy(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            FilePickerFileType? selectedType = null;
            using var disposable = await request.WatchResponseAsync((response, results) =>
            {
                if (response != 0)
                {
                    tsc.TrySetResult(null);
                }
                else
                {
                    if (results.TryGetValue("current_filter", out var currentFilterResult))
                    {
                        var filterStruct = (DBusStruct)currentFilterResult.Value;
                        var name = (string)filterStruct[0];
                        var patterns = new List<string>();
                        var mimeTypes = new List<string>();
                        var types = (List<DBusStruct>)filterStruct[1];
                        foreach (var t in types)
                        {
                            if ((uint)t[0] == 1)
                                mimeTypes.Add((string)t[1]);
                            else
                                patterns.Add((string)t[1]);
                        }

                        // Reuse the file type objects from options
                        // so the consuming code can match exactly the
                        // file type selected instead of spawning one.
                        selectedType = options.FileTypeChoices?.FirstOrDefault(type => type.Name == name && (
                            (type.MimeTypes?.All(y => mimeTypes.Contains(y)) ?? false) ||
                            (type.Patterns?.All(y => patterns.Contains(y)) ?? false)))
                            ?? new FilePickerFileType(name) { MimeTypes = mimeTypes, Patterns = patterns };
                    }

                    tsc.TrySetResult(((List<string>)results["uris"].Value).ToArray());
                }
            }).ConfigureAwait(false);

            var uris = await tsc.Task.ConfigureAwait(false);
            var path = uris?.FirstOrDefault() is { } filePath ? new Uri(filePath).LocalPath : null;

            if (path is null)
                return (null, selectedType);

            // WSL2 freedesktop automatically adds extension from selected file type, but we can't pass "default ext". So apply it manually.
            path = StorageProviderHelpers.NameWithExtension(path, options.DefaultExtension, selectedType);
            return (new BclStorageFile(new FileInfo(path)), selectedType);
        }

        public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            if (_version < 3)
                return [];

            var parentWindow = $"x11:{_handle.Handle:X}";
            var chooserOptions = new Dictionary<string, DBusVariant>
            {
                { "directory", new DBusVariant(true) }, { "multiple", new DBusVariant(options.AllowMultiple) }
            };

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", new DBusVariant(currentName));
            if (options.SuggestedStartLocation?.TryGetLocalPath() is { } folderPath)
                chooserOptions.Add("current_folder", new DBusVariant(new List<byte>(Encoding.UTF8.GetBytes(folderPath + "\0"))));

            var objectPath =
                await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);
            var request =
                new OrgFreedesktopPortalRequestProxy(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((response, results) =>
            {
                if (response != 0)
                    tsc.TrySetResult(null);
                else
                    tsc.TrySetResult(((List<string>)results["uris"].Value).ToArray());
            });

            var uris = await tsc.Task ?? Array.Empty<string>();
            return uris
                .Select(static path => new Uri(path).LocalPath)
                // WSL2 freedesktop allows to select files as well in directory picker, filter it out.
                .Where(Directory.Exists)
                .Select(static path => new BclStorageFolder(new DirectoryInfo(path))).ToList();
        }

        private static bool TryParseFilters(IReadOnlyList<FilePickerFileType>? fileTypes,
            FilePickerFileType? suggestedFileType,
            out DBusVariant result,
            out DBusVariant? currentFilter)
        {
            const uint GlobStyle = 0u;
            const uint MimeStyle = 1u;

            // Example: [('Images', [(0, '*.ico'), (1, 'image/png')]), ('Text', [(0, '*.txt')])]
            if (fileTypes is null)
            {
                result = default!;
                currentFilter = null;
                return false;
            }

            var filters = new List<DBusStruct>();
            currentFilter = null;

            foreach (var fileType in fileTypes)
            {
                var extensions = new List<DBusStruct>();
                if (fileType.Patterns?.Count > 0)
                    extensions.AddRange(fileType.Patterns.Select(static pattern => new DBusStruct(GlobStyle, pattern)));
                else if (fileType.MimeTypes?.Count > 0)
                    extensions.AddRange(
                        fileType.MimeTypes.Select(static mimeType => new DBusStruct(MimeStyle, mimeType)));
                else
                    continue;

                var filterStruct = new DBusStruct(fileType.Name, extensions);
                filters.Add(filterStruct);

                if (suggestedFileType is not null && ReferenceEquals(fileType, suggestedFileType))
                {
                    currentFilter = new DBusVariant(new DBusStruct(fileType.Name, extensions));
                }
            }

            result = new DBusVariant(filters);
            return true;
        }
    }
}
