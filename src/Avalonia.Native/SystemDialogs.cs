#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Native.Interop;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Native
{
    internal class SystemDialogs : BclStorageProvider
    {
        private readonly WindowBaseImpl _window;
        private readonly IAvnSystemDialogs _native;

        public SystemDialogs(WindowBaseImpl window, IAvnSystemDialogs native)
        {
            _window = window;
            _native = native;
        }

        public override bool CanOpen => true;

        public override bool CanSave => true;

        public override bool CanPickFolder => true;

        public override async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            using var events = new SystemDialogEvents();

            var suggestedDirectory = options.SuggestedStartLocation?.TryGetUri(out var suggestedDirectoryTmp) == true
                ? suggestedDirectoryTmp.LocalPath : string.Empty;

            _native.OpenFileDialog((IAvnWindow)_window.Native,
                                    events,
                                    options.AllowMultiple.AsComBool(),
                                    options.Title ?? string.Empty,
                                    suggestedDirectory,
                                    string.Empty,
                                    PrepareFilterParameter(options.FileTypeFilter));

            var result = await events.Task.ConfigureAwait(false);

            return result?.Select(f => new BclStorageFile(new FileInfo(f))).ToArray()
                   ?? Array.Empty<IStorageFile>();
        }

        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            using var events = new SystemDialogEvents();

            var suggestedDirectory = options.SuggestedStartLocation?.TryGetUri(out var suggestedDirectoryTmp) == true
                ? suggestedDirectoryTmp.LocalPath : string.Empty;

            _native.SaveFileDialog((IAvnWindow)_window.Native,
                        events,
                        options.Title ?? string.Empty,
                        suggestedDirectory,
                        options.SuggestedFileName ?? string.Empty,
                        PrepareFilterParameter(options.FileTypeChoices));

            var result = await events.Task.ConfigureAwait(false);
            return result.FirstOrDefault() is string file
                ? new BclStorageFile(new FileInfo(file))
                : null;
        }

        public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            using var events = new SystemDialogEvents();

            var suggestedDirectory = options.SuggestedStartLocation?.TryGetUri(out var suggestedDirectoryTmp) == true
                ? suggestedDirectoryTmp.LocalPath : string.Empty;

            _native.SelectFolderDialog((IAvnWindow)_window.Native, events, options.AllowMultiple.AsComBool(), options.Title ?? "", suggestedDirectory);

            var result = await events.Task.ConfigureAwait(false);
            return result?.Select(f => new BclStorageFolder(new DirectoryInfo(f))).ToArray()
                   ?? Array.Empty<IStorageFolder>();
        }

        private static string PrepareFilterParameter(IReadOnlyList<FilePickerFileType>? fileTypes)
        {
            return string.Join(";",
                fileTypes?.SelectMany(f =>
                {
                    // On the native side we will try to parse identifiers or mimetypes.
                    if (f.AppleUniformTypeIdentifiers?.Any() == true)
                    {
                        return f.AppleUniformTypeIdentifiers;
                    }
                    else if (f.MimeTypes?.Any() == true)
                    {
                        // MacOS doesn't accept "all" type, so it's pointless to pass it.
                        return f.MimeTypes.Where(t => t != "*/*");
                    }

                    return Array.Empty<string>();
                }) ??
                Array.Empty<string>());
        }
    }

    internal unsafe class SystemDialogEvents : NativeCallbackBase, IAvnSystemDialogEvents
    {
        private readonly TaskCompletionSource<string[]> _tcs;

        public SystemDialogEvents()
        {
            _tcs = new TaskCompletionSource<string[]>();
        }

        public Task<string[]> Task => _tcs.Task;

        public void OnCompleted(int numResults, void* trFirstResultRef)
        {
            string[] results = new string[numResults];

            unsafe
            {
                var ptr = (IntPtr*)trFirstResultRef;

                for (int i = 0; i < numResults; i++)
                {
                    results[i] = Marshal.PtrToStringAnsi(*ptr) ?? string.Empty;

                    ptr++;
                }
            }

            _tcs.SetResult(results);
        }
    }
}
