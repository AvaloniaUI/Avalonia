using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Avalonia.X11
{
    internal class X11DataObject : IDataObject, IDisposable
    {
        internal static readonly byte[] SerializedObjectGUID = new Guid("FD9EA796-3B13-4370-A679-56106BB288FB").ToByteArray();

        public const string c_mimeFiles = "text/uri-list";
        public const string c_mimeTextPlain = "text/plain";
        public const string c_mimeUTF8 = "UTF8_STRING";
        public const string c_mimeUTF8_alt = "text/plain;charset=utf-8";

        private readonly Dictionary<string, object?> _data = new();
        private readonly IList<string> _supportedTypes;
        private readonly List<string> _askedTypes = new();

        private X11DataReceiver _receiver;
        private bool _dropEnded = false;
        
        public X11DataObject(IList<string> supportedTypes, X11DataReceiver receiver) 
        {
            _supportedTypes = supportedTypes;
            _receiver = receiver;
        }

        public bool Contains(string dataFormat)
        {
            var mimeFormat = DataFormatToMimeFormat(dataFormat);

            if (_dropEnded)
            {
                return _data.ContainsKey(mimeFormat);
            }
            
            if(  _supportedTypes.Contains(mimeFormat) || _data.ContainsKey(mimeFormat))
            {
                if(!_askedTypes.Contains(mimeFormat)) _askedTypes.Add(mimeFormat);

                return true;
            }

            return false;
        }


        public object? Get(string dataFormat)
        {
            var mimeFormat = DataFormatToMimeFormat(dataFormat);
            if (_data.TryGetValue(mimeFormat, out var value)) return value;
            
            if(_dropEnded) return null;

            if(!_supportedTypes.Contains(mimeFormat)) return null;

            // Request the data synchronously from X11
            _receiver.RequestData(mimeFormat);

            if (_data.TryGetValue(mimeFormat, out var receivedValue)) return receivedValue;

            return null;

        }

        public IEnumerable<string> GetDataFormats()
        {
            return _dropEnded ?
                _data.Keys.Select(MimeFormatToDataFormat) :
                _supportedTypes.Select(MimeFormatToDataFormat);
        }

        public IList<string> GetSupportedTypes()
        {
            return _supportedTypes;
        }

        public IList<string> GetAskedTypes()
        {
            return _askedTypes;
        }

        public IList<string> GetLoadedSupportedTypes()
        {
            return _data.Keys.ToArray();
        }

        public void SetData(string type, object? data)
        {
            _data[DataFormatToMimeFormat(type)] = data;
        }

        public void DropEnded()
        {
            _dropEnded = true;
            _askedTypes.Clear();
        }

        public void Dispose()
        {
            _supportedTypes.Clear();
            _data.Clear();
        }    
        
        public bool AllDataLoaded => _data.Count == _supportedTypes.Count;

        public bool DataLoaded => _data.Count != 0;


        public static string DataFormatToMimeFormat(string format)
        {
            if (format == DataFormats.Text || format == c_mimeUTF8_alt || format == c_mimeTextPlain)
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
            if (format == c_mimeUTF8 || format == c_mimeUTF8_alt || format == c_mimeTextPlain)
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

            if (mime == c_mimeUTF8 || mime == c_mimeUTF8_alt)
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
                object netObject => SerializeObject(netObject),

            };

            return byteArray ?? Array.Empty<byte>();

        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for netstandart 2.0 dragndrop compatability")]
        private static byte[] SerializeObject(object data)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(SerializedObjectGUID, 0, SerializedObjectGUID.Length);
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                new BinaryFormatter().Serialize(ms, data);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                return ms.ToArray();
            }
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We still use BinaryFormatter for netstandart 2.0 dragndrop compatability")]
        [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "We still use BinaryFormatter for netstandart 2.0 dragndrop compatability")]
        public static object DeserializeObject(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                ms.Position = SerializedObjectGUID.Length;
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                return binaryFormatter.Deserialize(ms);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            }
        }
    }    
}
