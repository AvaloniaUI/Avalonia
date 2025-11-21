using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Avalonia.X11
{
    internal class X11DataTransfer : IDataTransfer, IDisposable
    {
        public const string c_mimeFiles = "text/uri-list";
        public const string c_mimeTextPlain = "text/plain";
        public const string c_mimeUTF8 = "UTF8_STRING";
        public const string c_mimeUTF8_alt = "text/plain;charset=utf-8";
        private const string c_appPrefix = "application/avn-fmt.";

        private readonly IList<string> _supportedTypes;
        private readonly List<X11DataTransferItem> _items = new();

        private X11DataReceiver _receiver;
        private bool _dropEnded = false;
        private DataFormat[]? _formats;

        public X11DataTransfer(IList<string> supportedTypes, X11DataReceiver receiver) 
        {
            _supportedTypes = supportedTypes.ToList();
            _receiver = receiver;
            InitializeItems();
        }

        private void InitializeItems()
        {
            _items.Clear();

            foreach (var mimeType in _supportedTypes)
            {
                var format = MimeFormatToDataFormat(mimeType);
                if (format != null && !_items.Any(it => it.Formats.Contains(format)))
                {
                    var item = new X11DataTransferItem(this, format, mimeType);
                    _items.Add(item);
                }
            }
        }

        public IReadOnlyList<DataFormat> Formats => _formats ??= GetFormatsCore();

        public IReadOnlyList<IDataTransferItem> Items => _items;

        private DataFormat[] GetFormatsCore()
        {
            var formats = new List<DataFormat>();

            if (_dropEnded)
            {
                foreach (var item in _items)
                {
                    foreach (var format in item.Formats)
                    {
                        formats.Add(format);
                    }
                }
            }
            else
            {
                foreach (var mimeType in _supportedTypes)
                {
                    var format = MimeFormatToDataFormat(mimeType);
                    if (format != null)
                        formats.Add(format);
                }
            }

            return formats.Distinct().ToArray();
        }

        public IList<string> GetSupportedTypes()
        {
            return _supportedTypes;
        }


        public IList<string> GetLoadedSupportedTypes()
        {
            return _items
                .Where(i => i.IsDataLoaded)
                .Select(i => i.MimeType)
                .ToArray();
        }

        private void Load(string mimeFormat)
        {
            if (_dropEnded) return;
            if (!_supportedTypes.Contains(mimeFormat)) return;

            // Request the data synchronously from X11
            _receiver.RequestData(mimeFormat);
        }

        private async Task LoadAsync(string mimeFormat)
        {
            if (_dropEnded)
            {
                return;
            }

            if (!_supportedTypes.Contains(mimeFormat))
            {
                return;
            }

            // Request the data synchronously from X11
            await _receiver.RequestDataAsync(mimeFormat);
        }

        public void SetData(DataFormat dataFormat, object? data)
        {
            var item = _items.FirstOrDefault(i => i.Formats.Contains(dataFormat));
            if (item != null)
            {
                item.SetData(data);
            }
            else
            {
                item = new X11DataTransferItem(this, dataFormat, DataFormatToMimeFormat(dataFormat), data);
                _items.Add(item);
                _formats = null;               
            }
        }

        public void SetData(string mimeType, object? data)
        {
            var item = _items.FirstOrDefault(i => i.MimeType == mimeType);
            if (item != null)
            {
                item.SetData(data);
            }
            else
            {
                var format = MimeFormatToDataFormat(mimeType);
                if (format != null)
                {
                    item = new X11DataTransferItem(this, format, mimeType, data);
                    _items.Add(item);
                    _supportedTypes.Add(mimeType);
                    _formats = null;
                }
            }

        }

        public void DropEnded()
        {
            _dropEnded = true;
            _items.RemoveAll(i => !i.IsDataLoaded);
            foreach (var item in _items)
            {
                item.DropEnded = true;
            }
        }

        public void Dispose()
        {
            _supportedTypes.Clear();
            _items.Clear();
        }    
        
        public bool AllDataLoaded => _items.All(i => i.IsDataLoaded);

        public bool DataLoaded => _items.Any(i => i.IsDataLoaded);


        public static string DataFormatToMimeFormat(DataFormat format)
        {
            if (format == DataFormat.Text)
            {
                return c_mimeUTF8;
            }            
            else if (format == DataFormat.File)
            {
                return c_mimeFiles;
            }

            return format.Identifier;
        }

        public static DataFormat MimeFormatToDataFormat(string format)
        {
            if (format == c_mimeUTF8 || format == c_mimeUTF8_alt || format == c_mimeTextPlain)
            {
                return DataFormat.Text;
            }
            else if (format == c_mimeFiles)
            {
                return DataFormat.File;
            }

            return DataFormat.FromSystemName<byte[]>(format, c_appPrefix);
            
        }

        public static byte[] ToTransfer(IDataTransfer dataTransfer, DataFormat format)
        {
            foreach (var item in dataTransfer.Items)
            {
                var itemFormat = item.Formats.FirstOrDefault(f => f == format);
                if (itemFormat != null)
                {
                    var value = item.TryGetRaw(itemFormat);
                    if (value != null)
                    {
                        return ConvertToBytes(value, format);
                    }
                }
            }

            return Array.Empty<byte>();
        }

        private static byte[] ConvertToBytes(object value, DataFormat dataFormat)
        {
            string mime = DataFormatToMimeFormat(dataFormat);

            if (mime == c_mimeUTF8 || mime == c_mimeUTF8_alt)
            {
                if (value is string str)
                {
                    return Encoding.UTF8.GetBytes(str);
                }
                if (value is IEnumerable<string> strings)
                {
                    return Encoding.UTF8.GetBytes(string.Join("\0", strings) + "\n");
                }
            }
            else if (mime == c_mimeTextPlain)
            {
                Encoding ansiEncoding = Encoding.GetEncoding(0);
                if (value is string str)
                {
                    return ansiEncoding.GetBytes(str);
                }
                if (value is IEnumerable<string> strings)
                {
                    return ansiEncoding.GetBytes(string.Join("\0", strings) + "\n");
                }
            }
            else if (mime == c_mimeFiles)
            {
                if (value is IEnumerable<IStorageItem> items)
                {
                    var uris = items
                        .Select(f => f.TryGetLocalPath())
                        .Where(f => f != null)
                        .Select(line => line!.StartsWith("file://") ? line : "file://" + line);

                    return Encoding.UTF8.GetBytes(string.Join("\0", uris) + "\n");
                }
            }

            return value switch
            {
                byte[] bytes => bytes,
                IEnumerable<byte> ebytes => ebytes.ToArray(),
                MemoryStream stream => stream.ToArray(),
                _ => Encoding.UTF8.GetBytes(value?.ToString() ?? string.Empty)
            };
        }


        private class X11DataTransferItem : IDataTransferItem, IAsyncDataTransferItem
        {
            private readonly X11DataTransfer _parent;
            private readonly string _mimeType;
            private object? _data;
            private bool _dropEnded = false;
            private bool _isDataLoaded = false;
            private readonly DataFormat _format;

            public X11DataTransferItem(X11DataTransfer parent, DataFormat format, string mimeType, object? data = null)
            {
                _parent = parent;
                _format = format;
                _mimeType = mimeType;
                _data = data;
                _isDataLoaded = data != null;
            }

            public IReadOnlyList<DataFormat> Formats => new[] { _format };

            public string MimeType => _mimeType;
            public bool DropEnded
            {
                get => _dropEnded;
                set { _data = value; }
            }

            public bool IsDataLoaded => _isDataLoaded;

            public void SetData(object? data)
            {
                _data = data;
                _isDataLoaded = true;
            }

            public object? TryGetRaw(DataFormat format)
            {
                if(_format != format)
                {
                    return null;
                }

                if(IsDataLoaded)
                {
                    return _data;
                }

                if(_dropEnded)
                {
                    return null;
                }

                try
                {
                    _parent.Load(_mimeType);
                    if (IsDataLoaded)
                    {
                        return _data;
                    }

                }
                catch (Exception ex)
                {
                    return Task.FromException<object?>(ex);
                }

                return null;
            }

            public async Task<object?> TryGetRawAsync(DataFormat format)
            {
                if (_format != format)
                {
                    return  Task.FromResult<object?>(null);
                }

                if (IsDataLoaded)
                {
                    return Task.FromResult(_data);
                }

                if (_dropEnded)
                {
                    return Task.FromResult<object?>(null);
                }

                try
                {
                    await _parent.LoadAsync(_mimeType);
                    
                    if (IsDataLoaded)
                    {
                        return Task.FromResult(_data);
                    }
                }
                catch (Exception ex)
                {
                    return Task.FromException<object?>(ex);
                }

                return Task.FromResult<object?>(null);
            }
        }


    }
}
