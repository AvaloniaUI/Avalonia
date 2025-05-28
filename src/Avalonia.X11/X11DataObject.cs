using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

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

            if(dataFormat == DataFormats.Text)
            {
                return _supportedTypes.Contains(c_mimeTextPlain) || _supportedTypes.Contains(c_mimeUTF8);
            }

            if(dataFormat == DataFormats.Files || dataFormat == DataFormats.FileNames)
            {
                return _supportedTypes.Contains(c_mimeFiles);
            }

            return _supportedTypes.Contains(dataFormat);
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
                yield return _bestType;
            }
            else
            {
                if (_supportedTypes.Contains(c_mimeTextPlain) || _supportedTypes.Contains(c_mimeUTF8))
                {
                    yield return DataFormats.Text;
                }

                if (_supportedTypes.Contains(c_mimeFiles))
                {
                    yield return DataFormats.Files;
                    yield return DataFormats.FileNames;
                }

                foreach (var type in _supportedTypes)
                {
                    yield return type;
                }
            }
        }

        public bool ReserveType(string typeName)
        {
            if(_dataSetted)
            {
                return typeName == _bestType ||
                    (typeName == DataFormats.Text && (_bestType == c_mimeTextPlain || _bestType == c_mimeUTF8)) ||
                    ((typeName == DataFormats.FileNames || typeName == DataFormats.Files) && _bestType == c_mimeFiles);
            }


            if(_supportedTypes.Contains(typeName))
            {
                _bestType = typeName;
                return true;
            }
            else if(typeName == DataFormats.Text && _supportedTypes.Contains(c_mimeUTF8))
            {
                _bestType = c_mimeUTF8;
                return true;
            }
            else if (typeName == DataFormats.Text && _supportedTypes.Contains(c_mimeTextPlain))
            {
                _bestType = c_mimeTextPlain;
                return true;
            }
            else if ((typeName == DataFormats.FileNames || typeName == DataFormats.Files) && _supportedTypes.Contains(c_mimeFiles))
            {
                _bestType = c_mimeFiles;
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
    }
}
