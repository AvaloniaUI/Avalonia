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
            using var fileTypes = new FilePickerFileTypesWrapper(options.FileTypeFilter, null);

            var suggestedDirectory = options.SuggestedStartLocation?.TryGetLocalPath() ?? string.Empty;

            _native.OpenFileDialog((IAvnWindow)_window.Native,
                                    events,
                                    options.AllowMultiple.AsComBool(),
                                    options.Title ?? string.Empty,
                                    suggestedDirectory,
                                    options.SuggestedFileName ?? string.Empty,
                                    fileTypes);

            var result = await events.Task.ConfigureAwait(false);

            return result?.Select(f => new BclStorageFile(new FileInfo(f))).ToArray()
                   ?? Array.Empty<IStorageFile>();
        }

        public override async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            using var events = new SystemDialogEvents();
            using var fileTypes = new FilePickerFileTypesWrapper(options.FileTypeChoices, options.DefaultExtension);

            var suggestedDirectory = options.SuggestedStartLocation?.TryGetLocalPath() ?? string.Empty;

            _native.SaveFileDialog((IAvnWindow)_window.Native,
                        events,
                        options.Title ?? string.Empty,
                        suggestedDirectory,
                        options.SuggestedFileName ?? string.Empty,
                        fileTypes);

            var result = await events.Task.ConfigureAwait(false);
            return result.FirstOrDefault() is string file
                ? new BclStorageFile(new FileInfo(file))
                : null;
        }

        public override async Task<IReadOnlyList<IStorageFolder>> OpenFolderPickerAsync(FolderPickerOpenOptions options)
        {
            using var events = new SystemDialogEvents();

            var suggestedDirectory = options.SuggestedStartLocation?.TryGetLocalPath() ?? string.Empty;

            _native.SelectFolderDialog((IAvnWindow)_window.Native, events, options.AllowMultiple.AsComBool(), options.Title ?? "", suggestedDirectory);

            var result = await events.Task.ConfigureAwait(false);
            return result?.Select(f => new BclStorageFolder(new DirectoryInfo(f))).ToArray()
                   ?? Array.Empty<IStorageFolder>();
        }
    }

    internal class FilePickerFileTypesWrapper : NativeCallbackBase, IAvnFilePickerFileTypes
    {
        private readonly IReadOnlyList<FilePickerFileType>? _types;
        private readonly string? _defaultExtension;
        private readonly List<IDisposable> _disposables;

        public FilePickerFileTypesWrapper(
            IReadOnlyList<FilePickerFileType>? types,
            string? defaultExtension)
        {
            _types = types;
            _defaultExtension = defaultExtension;
            _disposables = new List<IDisposable>();
        }

        public int Count => _types?.Count ?? 0;

        public int IsDefaultType(int index) => (_defaultExtension is not null &&
            _types![index].TryGetExtensions()?.Any(ext => _defaultExtension.EndsWith(ext)) == true).AsComBool();

        public int IsAnyType(int index) =>
            (_types![index].Patterns?.Contains("*.*") == true || _types[index].MimeTypes?.Contains("*.*") == true)
            .AsComBool();

        public IAvnString GetName(int index)
        {
            return EnsureDisposable(_types![index].Name.ToAvnString());
        }

        public IAvnStringArray GetPatterns(int index)
        {
            return EnsureDisposable(new AvnStringArray(_types![index].Patterns ?? Array.Empty<string>()));
        }

        public IAvnStringArray GetExtensions(int index)
        {
            return EnsureDisposable(new AvnStringArray(_types![index].TryGetExtensions() ?? Array.Empty<string>()));
        }

        public IAvnStringArray GetMimeTypes(int index)
        {
            return EnsureDisposable(new AvnStringArray(_types![index].MimeTypes ?? Array.Empty<string>()));
        }

        public IAvnStringArray GetAppleUniformTypeIdentifiers(int index)
        {
            return EnsureDisposable(new AvnStringArray(_types![index].AppleUniformTypeIdentifiers ?? Array.Empty<string>()));
        }

        protected override void Destroyed()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }

        private T EnsureDisposable<T>(T input) where T : IDisposable
        {
            _disposables.Add(input);
            return input;
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
