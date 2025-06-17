#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Avalonia.Native.Interop;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Native
{
    internal sealed class ClipboardImpl : IOwnedClipboardImpl, IDisposable
    {
        private IAvnClipboard? _native;
        private long _lastClearChangeCount;

        // TODO hide native types behind IAvnClipboard abstraction, so managed side won't depend on macOS.
        private const string NSPasteboardTypeString = "public.utf8-plain-text";
        private const string NSFilenamesPboardType = "NSFilenamesPboardType";

        public ClipboardImpl(IAvnClipboard native)
        {
            _native = native;
        }

        private IAvnClipboard Native
            => _native ?? throw new ObjectDisposedException(nameof(ClipboardImpl));

        private void ClearCore()
        {
            _lastClearChangeCount = Native.Clear();
        }
        
        public Task ClearAsync()
        {
            ClearCore();
            return Task.CompletedTask;
        }

        public Task<DataFormat[]> GetDataFormatsAsync()
            => Task.FromResult(GetFormats());

        public DataFormat[] GetFormats()
        {
            using var formats = Native.ObtainFormats();

            var count = formats.Count;
            if (count == 0)
                return [];

            var results = new HashSet<DataFormat>();

            for (var c = 0u; c < count; c++)
            {
                using var format = formats.Get(c);
                switch (format.String)
                {
                    case NSPasteboardTypeString:
                        results.Add(DataFormat.Text);
                        break;
                    case NSFilenamesPboardType:
                        results.Add(DataFormat.File);
                        break;
                    case { } name:
                        results.Add(DataFormat.Parse(name));
                        break;
                }
            }

            return results.ToArray();
        }

        public void Dispose()
        {
            _native?.Dispose();
            _native = null;
        }

        private string? GetText()
        {
            using var text = Native.GetText(NSPasteboardTypeString);
            return text?.String;
        }

        private string[]? GetFileNames()
        {
            using var strings = Native.GetStrings(NSFilenamesPboardType);
            return strings?.ToStringArray();
        }

        private IStorageItem[]? GetFiles()
        {
            var storageApi = (StorageProviderApi)AvaloniaLocator.Current.GetRequiredService<IStorageProviderFactory>();
    
            // TODO: use non-deprecated AppKit API to get NSUri instead of file names.
            var fileNames = GetFileNames();
            if (fileNames is null)
                return null;

            return fileNames
                .Select(f => StorageProviderHelpers.TryGetUriFromFilePath(f, false) is { } uri
                    ? storageApi.TryGetStorageItem(uri)
                    : null)
                .Where(f => f is not null)
                .ToArray()!;
        }

        private byte[]? GetBytes(DataFormat format)
        {
            using var data = Native.GetBytes(format.SystemName);
            return data?.Bytes;
        }

        public object? TryGetData(DataFormat format)
        {
            if (DataFormat.Text.Equals(format) || format.SystemName == NSPasteboardTypeString)
                return GetText();

            if (DataFormat.FileNames.Equals(format) || format.SystemName == NSFilenamesPboardType)
                return GetFileNames();

            if (DataFormat.Files.Equals(format))
                return GetFiles();

            return GetBytes(format);
        }

        public Task<IDataTransfer?> TryGetDataAsync(IEnumerable<DataFormat> formats)
        {
            throw new NotImplementedException();
        }

        public Task SetDataAsync(IDataTransfer dataTransfer)
        {
            ClearCore();

            throw new NotImplementedException();

            // var formats = await dataTransfer.GetFormatsAsync();
            //
            // foreach (var format in formats)
            // {
            //     if (await dataTransfer.TryGetAsync(format) is { } value)
            //         SetData(value, format);
            // }
        }

        public void SetData(IDataTransfer dataTransfer)
        {
            ClearCore();

            throw new NotImplementedException();

            // var formats = dataTransfer.GetFormats();
            //
            // foreach (var format in formats)
            // {
            //     if (dataTransfer.TryGet(format) is { } value)
            //         SetData(value, format);
            // }
        }

        private void SetData(object data, DataFormat format)
        {
            if (DataFormat.Text.Equals(format))
            {
                SetString(NSPasteboardTypeString, Convert.ToString(data) ?? string.Empty);
                return;
            }

            if (DataFormat.FileNames.Equals(format))
            {
                var fileNames = GetTypedData<IEnumerable<string>>(data, format) ?? [];
                SetFileNames(NSFilenamesPboardType, fileNames);
                return;
            }

            if (DataFormat.Files.Equals(format))
            {
                var files = GetTypedData<IEnumerable<IStorageItem>>(data, format) ?? [];

                IEnumerable<string> fileNames = files
                    .Select(StorageProviderExtensions.TryGetLocalPath)
                    .Where(path => path is not null)!;

                SetFileNames(NSFilenamesPboardType, fileNames);
                return;
            }

            switch (data)
            {
                case byte[] bytes:
                {
                    SetBytes(format.SystemName, bytes.AsSpan());
                    return;
                }

                case Memory<byte> bytes:
                {
                    SetBytes(format.SystemName, bytes.Span);
                    return;
                }

                case string str:
                {
                    SetString(format.SystemName, str);
                    return;
                }

                case Stream stream:
                {
                    var length = (int)(stream.Length - stream.Position);
                    var buffer = ArrayPool<byte>.Shared.Rent(length);

                    try
                    {
                        stream.ReadExactly(buffer, 0, length);
                        SetBytes(format.SystemName, buffer);
                        return;
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }
                }

                default:
                {
                    Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)?.Log(
                        this,
                        "Unsupported value type {Type} for data format {Format}",
                        data.GetType(),
                        format);
                    return;
                }
            }

            static T? GetTypedData<T>(object? data, DataFormat format) where T : class
                => data switch
                {
                    null => null,
                    T value => value,
                    _ => throw new InvalidOperationException(
                        $"Expected a value of type {typeof(T)} for data format {format}, got {data.GetType()} instead.")
                };
        }

        private void SetString(string type, string value)
            => Native.SetText(type, value);

        private void SetFileNames(string type, IEnumerable<string> fileNames)
        {
            using var strings = new AvnStringArray(fileNames);
            Native.SetStrings(type, strings);
        }

        private unsafe void SetBytes(string type, ReadOnlySpan<byte> bytes)
        {
            fixed (byte* ptr = bytes)
                Native.SetBytes(type, ptr, bytes.Length);
        }

        public Task<bool> IsCurrentOwnerAsync()
            => Task.FromResult(Native.ChangeCount == _lastClearChangeCount);

    }
}
