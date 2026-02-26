using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.X11.Clipboard;

namespace Avalonia.X11
{
    internal class X11DataTransfer : IDataTransfer, IDisposable
    {
        private readonly IList<IntPtr> _supportedTypes;
        private readonly List<X11DataTransferItem> _items = new();
        private readonly X11Atoms _x11Atoms;
        private readonly DataFormat[] _dataFormats;
        private readonly IList<IntPtr> _textFormatAtoms;

        private X11DataReceiver _receiver;
        private bool _dropEnded = false;
        private DataFormat[]? _formats;

        public X11DataTransfer(IList<IntPtr> supportedTypes, X11DataReceiver receiver, X11Atoms x11Atoms)
        {
            _supportedTypes = supportedTypes.ToList();
            _receiver = receiver;
            InitializeItems();
            _x11Atoms = x11Atoms;

            (_dataFormats, _textFormatAtoms) = ClipboardDataFormatHelper.GetDataFormats(_supportedTypes, _x11Atoms);
        }

        private void InitializeItems()
        {
            _items.Clear();

            foreach (var mimeType in _supportedTypes)
            {
                var format = ClipboardDataFormatHelper.ToDataFormat(mimeType, _x11Atoms);
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
            if (_dropEnded)
            {
                var formats = new List<DataFormat>();

                foreach (var item in _items)
                {
                    foreach (var format in item.Formats)
                    {
                        formats.Add(format);
                    }
                }

                return formats.Distinct().ToArray();
            }
            else
            {
                return _dataFormats;
            }           
        }

        public IList<IntPtr> GetSupportedTypes()
        {
            return _supportedTypes;
        }


        public IList<IntPtr> GetLoadedSupportedTypes()
        {
            return _items
                .Where(i => i.IsDataLoaded)
                .Select(i => i.MimeType)
                .ToArray();
        }

        public IList<DataFormat> GetLoadedDataFormats()
        {
            return _items
                .Where(i => i.IsDataLoaded)
                .SelectMany(i => i.Formats)
                .ToArray();
        }

        private Task<object?> Load(DataFormat format, IntPtr formatAtom)
        {
            if (_dropEnded) return Task.FromResult<object?>(null);
            if (!_supportedTypes.Contains(formatAtom))
                return Task.FromResult<object?>(null);
            ;

            return _receiver.RequestData(format, formatAtom);
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
                item = new X11DataTransferItem(this, dataFormat, ToAtom(dataFormat), data);
                _items.Add(item);
                _formats = null;               
            }
        }

        public void DropEnded()
        {
            _dropEnded = true;
            _items.RemoveAll(i => !i.IsDataLoaded);
            _formats = null;
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

        public IntPtr ToAtom(DataFormat format)
        {
            return ClipboardDataFormatHelper.ToAtom(format, _textFormatAtoms.ToArray(), _x11Atoms, _dataFormats);
        }



        public static byte[] ToTransfer(IDataTransfer dataTransfer, DataFormat? format)
        {
            if(format == null)
                return Array.Empty<byte>();

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

            if (DataFormat.Text.Equals(dataFormat))
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
            if (DataFormat.File.Equals(dataFormat))
            {
                if (value is IEnumerable<IStorageItem> items)
                {
                    return ClipboardUriListHelper.FileUriListToUtf8Bytes(items);
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
            private readonly IntPtr _mimeType;
            private object? _data;
            private bool _dropEnded = false;
            private bool _isDataLoaded = false;
            private readonly DataFormat _format;

            public X11DataTransferItem(X11DataTransfer parent, DataFormat format, IntPtr mimeType, object? data = null)
            {
                _parent = parent;
                _format = format;
                _mimeType = mimeType;
                _data = data;
                _isDataLoaded = data != null;
            }

            public IReadOnlyList<DataFormat> Formats => new[] { _format };

            public IntPtr MimeType => _mimeType;
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

          

            public async Task<object?> TryGetRawAsync(DataFormat format)
            {
                if (_format != format)
                {
                    return Task.FromResult<object?>(null);
                }

                if (IsDataLoaded)
                {
                    return Task.FromResult<object?>(_data);
                }

                if (_dropEnded)
                {
                    return Task.FromResult<object?>(null);
                }

                try
                {
                    var res = await _parent.Load(format, _mimeType);
                    if (res != null)
                    {
                        SetData(res);
                        return Task.FromResult<object?>(res);
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }

                return Task.FromResult<object?>(null);
                ;
            }

            public object? TryGetRaw(DataFormat format)
            {
               return TryGetRawAsync(format).GetAwaiter().GetResult();
            }
        }
    }
}
