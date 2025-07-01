using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Avalonia.X11
{
    internal class X11DataObject : IX11DataObject, IDataObject, IDisposable
    {
        private const string c_mimeFiles = "text/uri-list";
        private const string c_mimeTextPlain = "text/plain";
        private const string c_mimeUTF8 = "UTF8_STRING";

        private readonly IList<string> _supportedTypes;

        private string _bestType = string.Empty;

        private object? _data = null;
        private bool _dataSetted = false;
        
        public X11DataObject(IList<string> supportedTypes) 
        {
            _supportedTypes = supportedTypes;
        }

        public bool Contains(string dataFormat)
        {
            if (_dataSetted)
            {
                return _bestType == dataFormat;
            }

            return _supportedTypes.Contains(DataFormatToMimeFormat(dataFormat));
        }
               

        public object? Get(string dataFormat)
        {
            if(_dataSetted && dataFormat == _bestType)
            { 
                return _data; 
            }

            return null;
        }

        public IEnumerable<string> GetDataFormats()
        {
            if (_dataSetted)
            {
                return [MimeFormatToDataFormat(_bestType)];
            }
            else
            {               
                return _supportedTypes.Select(MimeFormatToDataFormat);
            }
        }

        public bool ReserveType(string typeName)
        {
            var mimeName = DataFormatToMimeFormat(typeName);

            if(_dataSetted)
            {
                return mimeName == _bestType;
            }
                        
            if(_supportedTypes.Contains(mimeName))
            {
                _bestType = typeName;
                return true;
            }
            else if (typeName == DataFormats.Text && _supportedTypes.Contains(c_mimeTextPlain))
            {
                _bestType = c_mimeTextPlain;
                return true;
            }

            return false;
        }

        public string GetBestType()
        {
            if(!string.IsNullOrEmpty(_bestType))
                return _bestType;

            if (_supportedTypes.Contains(c_mimeFiles))
                return c_mimeFiles;

            if (_supportedTypes.Contains(c_mimeUTF8))
                return c_mimeUTF8;

            if (_supportedTypes.Contains(c_mimeTextPlain))
                return c_mimeTextPlain;

            if(_supportedTypes.Any())
                return _supportedTypes.First();

            return string.Empty;
        }

        public void SetData(string type, object? data)
        {
            _dataSetted = true;
            _bestType = type;
            _data = data; 
        }

        public void Dispose()
        {
            _supportedTypes.Clear();
            _data = null;
        }    
        
        public static string DataFormatToMimeFormat(string format)
        {
            if (format == DataFormats.Text )
            {
                return c_mimeUTF8;
            }            
            else if (format == DataFormats.FileNames || format == DataFormats.Files)
            {
                return c_mimeFiles;
            }

            return format;
        }

        public static string MimeFormatToDataFormat(string format)
        {
            if (format == c_mimeUTF8 || format == c_mimeTextPlain)
            {
                return DataFormats.Text;
            }
            else if (format == c_mimeFiles)
            {
                return DataFormats.Files;
            }

            return format;
        }

        public static byte[] ToTransfer(IDataObject dataObject, string type)
        {
            object? obj = dataObject.Get(type);
            if (obj == null)
            {
                return Array.Empty<byte>();
            }

            string mime = DataFormatToMimeFormat(type);

            if (mime == c_mimeUTF8)
            {
                if (obj is IEnumerable<string> strings)
                {
                    return System.Text.Encoding.UTF8.GetBytes(string.Join("\0", strings) + "\n");
                }

                if (obj is not string str)
                {
                    return Array.Empty<byte>();
                }

                return System.Text.Encoding.UTF8.GetBytes(str);
            }
            else if (mime == c_mimeTextPlain)
            {
                Encoding ansiEncoding = Encoding.GetEncoding(0); //system ANSI

                if (obj is IEnumerable<string> strings)
                {
                    return ansiEncoding.GetBytes(string.Join("\0", strings) + "\n");
                }

                if (obj is not string str)
                {
                    return Array.Empty<byte>();
                }

                return ansiEncoding.GetBytes(str);
            }
            else if (mime == c_mimeFiles)
            {
                if (obj is string str)
                {
                    return System.Text.Encoding.UTF8.GetBytes(str);
                }

                if (obj is IEnumerable<string> strings)
                {
                    return System.Text.Encoding.UTF8.GetBytes(string.Join("\0", strings.Select(line => line!.StartsWith("file://") ? line : "file://" + line)) + "\n");
                }

                if (obj is not IEnumerable<IStorageItem> items)
                {
                    return Array.Empty<byte>();
                }

                var uris = items
                    .Select(f => f.TryGetLocalPath())
                    .Where(f => f is not null)!
                    .Select(line => line!.StartsWith("file://") ? line : "file://" + line);

                return System.Text.Encoding.UTF8.GetBytes(string.Join("\0", uris) + "\n");
            }


            byte[]? byteArray = obj switch
            {
                byte[] bytes => bytes,
                IEnumerable<byte> ebytes => ebytes.ToArray(),
                string str => Encoding.UTF8.GetBytes(str),
                MemoryStream stream => stream.ToArray(),
                int num => BitConverter.GetBytes(num),
                IConvertible convertible => BitConverter.GetBytes(convertible.ToInt32(null)),
                _ => null
            };

            return byteArray ?? Array.Empty<byte>();

        }
    }    
}
