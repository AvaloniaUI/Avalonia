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
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

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
                "/org/freedesktop/portal/desktop");
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

        private readonly Connection _connection;
        private readonly OrgFreedesktopPortalFileChooserProxy _fileChooser;
        private readonly IPlatformHandle _handle;
        private readonly uint _version;

        private DBusSystemDialog(Connection connection, IPlatformHandle handle,
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
            ObjectPath objectPath;
            var chooserOptions = new Dictionary<string, VariantValue>();

            if (TryParseFilters(options.FileTypeFilter, options.SuggestedFileType, out var filters,
                    out var currentFilter))
            {
                chooserOptions.Add("filters", filters);
                if (currentFilter is { } filter)
                    chooserOptions.Add("current_filter", filter);
            }

            if (options.SuggestedStartLocation?.TryGetLocalPath() is { } folderPath)
                chooserOptions.Add("current_folder", VariantValue.Array(Encoding.UTF8.GetBytes(folderPath + "\0")));

            chooserOptions.Add("multiple", VariantValue.Bool(options.AllowMultiple));

            objectPath = await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);

            var request =
                new OrgFreedesktopPortalRequestProxy(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                    tsc.TrySetException(e);
                else
                    tsc.TrySetResult(x.Results["uris"].GetArray<string>());
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
            ObjectPath objectPath;
            var chooserOptions = new Dictionary<string, VariantValue>();
            if (TryParseFilters(options.FileTypeChoices, options.SuggestedFileType, out var filters,
                    out var currentFilter))
            {
                chooserOptions.Add("filters", filters);
                if (currentFilter is { } filter)
                    chooserOptions.Add("current_filter", filter);
            }

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", VariantValue.String(currentName));
            if (options.SuggestedStartLocation?.TryGetLocalPath() is { } folderPath)
                chooserOptions.Add("current_folder", VariantValue.Array(Encoding.UTF8.GetBytes(folderPath + "\0")));

            objectPath = await _fileChooser.SaveFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions)
                .ConfigureAwait(false);
            var request =
                new OrgFreedesktopPortalRequestProxy(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            FilePickerFileType? selectedType = null;
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                {
                    tsc.TrySetException(e);
                }
                else
                {
                    if (x.Results.TryGetValue("current_filter", out var currentFilter))
                    {
                        var name = currentFilter.GetItem(0).GetString();
                        var patterns = new List<string>();
                        var mimeTypes = new List<string>();
                        var types = currentFilter.GetItem(1).GetArray<VariantValue>();
                        foreach (var t in types)
                        {
                            if (t.GetItem(0).GetUInt32() == 1)
                                mimeTypes.Add(t.GetItem(1).GetString());
                            else
                                patterns.Add(t.GetItem(1).GetString());
                        }
                        
                        // Reuse the file type objects from options
                        // so the consuming code can match exactly the
                        // file type selected instead of spawning one.
                        selectedType = options.FileTypeChoices?.FirstOrDefault(type => type.Name == name && (
                            (type.MimeTypes?.All(y => mimeTypes.Contains(y)) ?? false) ||
                            (type.Patterns?.All(y => patterns.Contains(y)) ?? false))) 
                            ?? new FilePickerFileType(name) { MimeTypes = mimeTypes, Patterns = patterns };
                    }

                    tsc.TrySetResult(x.Results["uris"].GetArray<string>());
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
            var chooserOptions = new Dictionary<string, VariantValue>
            {
                { "directory", VariantValue.Bool(true) }, { "multiple", VariantValue.Bool(options.AllowMultiple) }
            };

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", VariantValue.String(currentName));
            if (options.SuggestedStartLocation?.TryGetLocalPath() is { } folderPath)
                chooserOptions.Add("current_folder", VariantValue.Array(Encoding.UTF8.GetBytes(folderPath + "\0")));

            var objectPath =
                await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);
            var request =
                new OrgFreedesktopPortalRequestProxy(_connection, "org.freedesktop.portal.Desktop", objectPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync((e, x) =>
            {
                if (e is not null)
                    tsc.TrySetException(e);
                else
                    tsc.TrySetResult(x.Results["uris"].GetArray<string>());
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
            out VariantValue result,
            out VariantValue? currentFilter)
        {
            const uint GlobStyle = 0u;
            const uint MimeStyle = 1u;

            // Example: [('Images', [(0, '*.ico'), (1, 'image/png')]), ('Text', [(0, '*.txt')])]
            if (fileTypes is null)
            {
                result = default;
                currentFilter = null;
                return false;
            }

            var filters = new Array<Struct<string, Array<Struct<uint, string>>>>();
            currentFilter = null;

            foreach (var fileType in fileTypes)
            {
                var extensions = new List<Struct<uint, string>>();
                if (fileType.Patterns?.Count > 0)
                    extensions.AddRange(fileType.Patterns.Select(static pattern => Struct.Create(GlobStyle, pattern)));
                else if (fileType.MimeTypes?.Count > 0)
                    extensions.AddRange(
                        fileType.MimeTypes.Select(static mimeType => Struct.Create(MimeStyle, mimeType)));
                else
                    continue;

                var filterStruct = Struct.Create(fileType.Name, new Array<Struct<uint, string>>(extensions));
                filters.Add(filterStruct);

                if (suggestedFileType is not null && ReferenceEquals(fileType, suggestedFileType))
                {
                    currentFilter = VariantValue.Struct(
                        VariantValue.String(filterStruct.Item1),
                        filterStruct.Item2.AsVariantValue());
                }
            }

            result = filters.AsVariantValue();
            return true;
        }
    }
}
