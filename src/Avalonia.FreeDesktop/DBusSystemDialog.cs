using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;
using Avalonia.Threading;
using Tmds.DBus.Protocol;
using Avalonia.FreeDesktop.DBus;

namespace Avalonia.FreeDesktop
{
    internal class DBusSystemDialog : BclStorageProvider
    {
        /// <summary>
        /// Creates a portal-backed storage provider if xdg-desktop-portal FileChooser is available.
        /// </summary>
        /// <param name="parentLeaseProvider">
        /// Callback invoked once per picker call to obtain the parent-window handle to pass to the
        /// portal. The returned <see cref="IPortalParentLease"/> is held until the picker call
        /// completes and then disposed. May return <c>null</c> to call the portal with an empty
        /// parent_window string (per portal spec: "no parent").
        /// </param>
        internal static async Task<IStorageProvider?> TryCreateAsync(Func<Task<IPortalParentLease?>>? parentLeaseProvider)
        {
            if (DBusHelper.DefaultConnection is not { } conn)
                return null;

            using var restoreContext = AvaloniaSynchronizationContext.Ensure(DispatcherPriority.Input);

            var dbusFileChooser = new FileChooser(conn, "org.freedesktop.portal.Desktop",
                "/org/freedesktop/portal/desktop");
            uint version;
            try
            {
                version = await dbusFileChooser.GetVersionAsync();
            }
            catch
            {
                return null;
            }

            return new DBusSystemDialog(conn, parentLeaseProvider, dbusFileChooser, version);
        }

        private readonly DBusConnection _connection;
        private readonly FileChooser _fileChooser;
        private readonly Func<Task<IPortalParentLease?>>? _parentLeaseProvider;
        private readonly uint _version;

        private DBusSystemDialog(DBusConnection connection, Func<Task<IPortalParentLease?>>? parentLeaseProvider,
            FileChooser fileChooser, uint version)
        {
            _connection = connection;
            _fileChooser = fileChooser;
            _parentLeaseProvider = parentLeaseProvider;
            _version = version;
        }

        private async Task<IPortalParentLease?> AcquireParentLeaseAsync()
            => _parentLeaseProvider is null ? null : await _parentLeaseProvider().ConfigureAwait(false);

        public override bool CanOpen => true;

        public override bool CanSave => true;

        public override bool CanPickFolder => _version >= 3;

        public override async Task<OpenFilePickerResult> OpenFilePickerWithResultAsync(FilePickerOpenOptions options)
        {
            var (paths, selectedType) = await ShowFilePickerAsync(
                options.SuggestedFileType,
                options.SuggestedFileName,
                options.SuggestedStartLocation,
                options.FileTypeFilter,
                options.Title,
                options.AllowMultiple,
                _fileChooser.OpenFileAsync)
                .ConfigureAwait(false);

            var files = paths.Select(path => new BclStorageFile(new FileInfo(path))).ToArray();

            return new OpenFilePickerResult { Files = files, SelectedFileType = selectedType };
        }

        public override async Task<SaveFilePickerResult> SaveFilePickerWithResultAsync(FilePickerSaveOptions options)
        {
            var (paths, selectedType) = await ShowFilePickerAsync(
                options.SuggestedFileType,
                options.SuggestedFileName,
                options.SuggestedStartLocation,
                options.FileTypeChoices,
                options.Title,
                null,
                _fileChooser.SaveFileAsync)
            .ConfigureAwait(false);

            var path = paths.FirstOrDefault();

            BclStorageFile? file;
            if (path is null)
                file = null;
            else
            {
                // WSL2 freedesktop automatically adds extension from selected file type, but we can't pass "default ext". So apply it manually.
                path = StorageProviderHelpers.NameWithExtension(path, options.DefaultExtension, selectedType);
                file = new BclStorageFile(new FileInfo(path));
            }

            return new SaveFilePickerResult { File = file, SelectedFileType = selectedType };
        }

        private async Task<(string[] paths, FilePickerFileType? selectedType)> ShowFilePickerAsync(
            FilePickerFileType? suggestedFileType,
            string? suggestedFileName,
            IStorageFolder? suggestedStartLocation,
            IReadOnlyList<FilePickerFileType>? fileTypes,
            string? title,
            bool? allowMultiple,
            Func<string, string, Dictionary<string, VariantValue>, Task<ObjectPath>> showAsync)
        {
            await using var parentLease = await AcquireParentLeaseAsync().ConfigureAwait(false);
            var parentWindow = parentLease?.Handle ?? string.Empty;
            var (expectedPath, token) = CreateRequestToken();
            var chooserOptions = new Dictionary<string, VariantValue>();
            if (TryParseFilters(fileTypes,suggestedFileType, out var filters,
                    out var currentFilter))
            {
                chooserOptions.Add("filters", filters);
                if (currentFilter is { } filter)
                    chooserOptions.Add("current_filter", filter);
            }

            if (suggestedFileName is not null)
                chooserOptions.Add("current_name", VariantValue.String(suggestedFileName));
            if (suggestedStartLocation?.TryGetLocalPath() is { } folderPath)
                chooserOptions.Add("current_folder", VariantValue.Array(Encoding.UTF8.GetBytes(folderPath + "\0")));
            if (allowMultiple.HasValue)
                chooserOptions.Add("multiple", VariantValue.Bool(allowMultiple.Value));
            chooserOptions.Add("handle_token", token);

            var request = new Request(_connection, "org.freedesktop.portal.Desktop", expectedPath);
            var tsc = new TaskCompletionSource<string[]?>();
            FilePickerFileType? selectedType = null;
            using var disposable = await request.WatchResponseAsync(notification =>
            {
                if (notification.IsCompletion)
                {
                    tsc.TrySetException(notification.Exception);
                }
                else
                {
                    var x = notification.Value;
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
                        selectedType = fileTypes?.FirstOrDefault(type => type.Name == name && (
                            (type.MimeTypes?.All(y => mimeTypes.Contains(y)) ?? false) ||
                            (type.Patterns?.All(y => patterns.Contains(y)) ?? false)))
                            ?? new FilePickerFileType(name) { MimeTypes = mimeTypes, Patterns = patterns };
                    }

                    tsc.TrySetResult(x.Results["uris"].GetArray<string>());
                }
            }, ObserverFlags.EmitAll).ConfigureAwait(false);

            var actualPath = await showAsync(parentWindow, title ?? string.Empty, chooserOptions)
                .ConfigureAwait(false);
            VerifyRequestPath(tsc, expectedPath, actualPath);

            var uris = await tsc.Task.ConfigureAwait(false) ?? [];
            var paths = uris.Select(uri => new Uri(uri).LocalPath).ToArray();
            return (paths, selectedType);
        }

        public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            if (_version < 3)
                return [];

            await using var parentLease = await AcquireParentLeaseAsync().ConfigureAwait(false);
            var parentWindow = parentLease?.Handle ?? string.Empty;
            var (expectedPath, token) = CreateRequestToken();
            var chooserOptions = new Dictionary<string, VariantValue>
            {
                { "directory", VariantValue.Bool(true) }, { "multiple", VariantValue.Bool(options.AllowMultiple) }
            };

            if (options.SuggestedFileName is { } currentName)
                chooserOptions.Add("current_name", VariantValue.String(currentName));
            if (options.SuggestedStartLocation?.TryGetLocalPath() is { } folderPath)
                chooserOptions.Add("current_folder", VariantValue.Array(Encoding.UTF8.GetBytes(folderPath + "\0")));
            chooserOptions.Add("handle_token", token);

            var request = new Request(_connection, "org.freedesktop.portal.Desktop", expectedPath);
            var tsc = new TaskCompletionSource<string[]?>();
            using var disposable = await request.WatchResponseAsync(notification =>
            {
                if (notification.IsCompletion)
                    tsc.TrySetException(notification.Exception);
                else
                    tsc.TrySetResult(notification.Value.Results["uris"].GetArray<string>());
            }, ObserverFlags.EmitAll);

            var actualPath = await _fileChooser.OpenFileAsync(parentWindow, options.Title ?? string.Empty, chooserOptions);
            VerifyRequestPath(tsc, expectedPath, actualPath);

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

        // https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.Request.html
        // Subscribe to the Response signal before making the portal call to avoid a race condition.
        private (ObjectPath ExpectedPath, string Token) CreateRequestToken()
        {
            string sender = (_connection.UniqueName ?? "").TrimStart(':').Replace(".", "_");
            string token = "Avalonia_" + Stopwatch.GetTimestamp().ToString();
            ObjectPath expectedPath = $"/org/freedesktop/portal/desktop/request/{sender}/{token}";
            return (expectedPath, token);
        }

        private static void VerifyRequestPath<T>(TaskCompletionSource<T> tsc, ObjectPath expectedPath, ObjectPath actualPath)
        {
            if (actualPath != expectedPath)
                tsc.TrySetException(new InvalidOperationException($"Portal returned unexpected request path '{actualPath}', expected '{expectedPath}'."));
        }
    }
}
