using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform.Interop;

namespace Avalonia.Native
{
    class ClipboardImpl : IClipboard, IDisposable
    {
        private IAvnClipboard _native;
        private const string NSPasteboardTypeString = "public.utf8-plain-text";
        private const string NSFilenamesPboardType = "NSFilenamesPboardType";
        private const string NSPasteboardTypeFileUrl = "public.file-url";
        
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

        public Task SetTextAsync(string text)
        {
            _native.Clear();

            if (text != null)
            {
                using (var buffer = new Utf8Buffer(text))
                {
                    _native.SetText(NSPasteboardTypeString, buffer.DangerousGetHandle());
                }
            }

            return Task.CompletedTask;
        }

        public IEnumerable<string> GetFormats()
        {
            var rv = new List<string>();
            using (var formats = _native.ObtainFormats())
            {
                var cnt = formats.Count;
                for (uint c = 0; c < cnt; c++)
                {
                    using (var fmt = formats.Get(c))
                    {
                         if(fmt.String == NSPasteboardTypeString)
                             rv.Add(DataFormats.Text);
                         if(fmt.String == NSFilenamesPboardType)
                             rv.Add(DataFormats.FileNames);
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
                return strings.ToStringArray();
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

        public string GetText()
        {
            // bad idea in general, but API is synchronous anyway
            return _clipboard.GetTextAsync().Result;
        }

        public IEnumerable<string> GetFileNames() => _clipboard.GetFileNames();

        public object Get(string dataFormat)
        {
            if (dataFormat == DataFormats.Text)
                return GetText();
            if (dataFormat == DataFormats.FileNames)
                return GetFileNames();
            return null;
        }
    }
}
