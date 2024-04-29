using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Logging;
using Avalonia.Native.Interop;
using Avalonia.Platform.Storage;
using Avalonia.Platform.Storage.FileIO;

namespace Avalonia.Native
{
    class ClipboardImpl : IClipboard, IDisposable
    {
        private IAvnClipboard _native;
        // TODO hide native types behind IAvnClipboard abstraction, so managed side won't depend on macOS.
        private const string NSPasteboardTypeString = "public.utf8-plain-text";
        private const string NSFilenamesPboardType = "NSFilenamesPboardType";

        public ClipboardImpl(IAvnClipboard native)
        {
            _native = native;
        }

        public Task ClearAsync()
        {
            _native.Clear();

            return Task.CompletedTask;
        }

        public Task<string> GetTextAsync()
        {
            using (var text = _native.GetText(NSPasteboardTypeString))
                return Task.FromResult(text.String);
        }

        public unsafe Task SetTextAsync(string text)
        {
            _native.Clear();

            if (text != null) 
                _native.SetText(NSPasteboardTypeString, text);

            return Task.CompletedTask;
        }

        public IEnumerable<string> GetFormats()
        {
            var rv = new HashSet<string>();
            using (var formats = _native.ObtainFormats())
            {
                var cnt = formats.Count;
                for (uint c = 0; c < cnt; c++)
                {
                    using (var fmt = formats.Get(c))
                    {
                         if(fmt.String == NSPasteboardTypeString)
                             rv.Add(DataFormats.Text);
                         if (fmt.String == NSFilenamesPboardType)
                         {
                             rv.Add(DataFormats.FileNames);
                             rv.Add(DataFormats.Files);
                         }
                         else
                             rv.Add(fmt.String);
                    }
                }
            }

            return rv;
        }
        
        public void Dispose()
        {
            _native?.Dispose();
            _native = null;
        }

        public IEnumerable<string> GetFileNames()
        {
            using (var strings = _native.GetStrings(NSFilenamesPboardType))
                return strings?.ToStringArray();
        }

        public IEnumerable<IStorageItem> GetFiles()
        {
            return GetFileNames()?.Select(f => StorageProviderHelpers.TryCreateBclStorageItem(f)!)
                .Where(f => f is not null);
        }

        public unsafe Task SetDataObjectAsync(IDataObject data)
        {
            _native.Clear();

            // If there is multiple values with the same "to" format, prefer these that were not mapped.
            var formats = data.GetDataFormats().Select(f =>
                {
                    string from, to;
                    bool mapped;
                    if (f == DataFormats.Text)
                        (from, to, mapped) = (f, NSPasteboardTypeString, true);
                    else if (f == DataFormats.Files || f == DataFormats.FileNames)
                        (from, to, mapped) = (f, NSFilenamesPboardType, true);
                    else (from, to, mapped) = (f, f, false);
                    return (from, to, mapped);
                })
                .GroupBy(p => p.to)
                .Select(g => g.OrderBy(f => f.mapped).First());
                
            
            foreach (var (fromFormat, toFormat, _) in formats)
            {
                var o = data.Get(fromFormat);
                switch (o)
                {
                    case string s:
                        _native.SetText(toFormat, s);
                        break;
                    case IEnumerable<IStorageItem> storageItems:
                        using (var strings = new AvnStringArray(storageItems
                                   .Select(s => s.TryGetLocalPath())
                                   .Where(p => p is not null)))
                        {
                            _native.SetStrings(toFormat, strings);
                        }
                        break;
                    case IEnumerable<string> managedStrings:
                        using (var strings = new AvnStringArray(managedStrings))
                        {
                            _native.SetStrings(toFormat, strings);
                        }
                        break;
                    case byte[] bytes:
                    {
                        fixed (byte* pbytes = bytes)
                            _native.SetBytes(toFormat, pbytes, bytes.Length);
                        break;
                    }
                    default:
                        Logger.TryGet(LogEventLevel.Warning, LogArea.macOSPlatform)?.Log(this,
                            "Unsupported IDataObject value type: {0}", o?.GetType().FullName ?? "(null)");
                        break;
                }
            }
            return Task.CompletedTask;
        }

        public Task<string[]> GetFormatsAsync()
        {
            return Task.FromResult(GetFormats().ToArray());
        }

        public async Task<object> GetDataAsync(string format)
        {
            if (format == DataFormats.Text || format == NSPasteboardTypeString)
                return await GetTextAsync();
            if (format == DataFormats.FileNames || format == NSFilenamesPboardType)
                return GetFileNames();
            if (format == DataFormats.Files)
                return GetFiles();
            using (var n = _native.GetBytes(format))
                return n.Bytes;
        }
    }
    
    class ClipboardDataObject : IDataObject, IDisposable
    {
        private ClipboardImpl _clipboard;
        private List<string> _formats;

        public ClipboardDataObject(IAvnClipboard clipboard)
        {
            _clipboard = new ClipboardImpl(clipboard);
        }

        public void Dispose()
        {
            _clipboard?.Dispose();
            _clipboard = null;
        }

        List<string> Formats => _formats ??= _clipboard.GetFormats().ToList();

        public IEnumerable<string> GetDataFormats() => Formats;

        public bool Contains(string dataFormat) => Formats.Contains(dataFormat);

        public object Get(string dataFormat) => _clipboard.GetDataAsync(dataFormat).GetAwaiter().GetResult();

        public Task SetFromDataObjectAsync(IDataObject dataObject) => _clipboard.SetDataObjectAsync(dataObject);
    }
}
