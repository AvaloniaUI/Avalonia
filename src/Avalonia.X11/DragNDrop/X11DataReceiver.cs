using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Input;
using System.Linq;
using Avalonia.X11.Clipboard;
using Avalonia.Media.Imaging;
using System.IO;
using Avalonia.Controls.Utils;
using System.Threading.Tasks;

namespace Avalonia.X11
{
    internal class X11DataReceiver: IDisposable
    {
        private readonly IntPtr _display;
        private readonly IntPtr _handle;
        private readonly X11Atoms _atoms;
        private readonly X11DataReader _reader;

        private IntPtr _uriList;
        private IntPtr _textPlain;
        private IntPtr _textPlainAlt;

         List<byte> _result = new();

        private Queue<IntPtr> _typesToLoad = new();
        private X11DataTransfer? _currentDrag;

        public X11DataReceiver(IntPtr handle, X11Info info, AvaloniaX11Platform platform) 
        {
            _handle = handle;
            _display = info.Display;
            _atoms = info.Atoms;
           


            InitializeTypeAtoms();
            _reader = new(info, platform);
        }
                
        public bool LoadDataOnDrop(X11DataTransfer? data)
        {
            if (data == null)
                return false;

            _result.Clear();
            _typesToLoad.Clear();
            _currentDrag = data;

            var supportedTypes = data.GetSupportedTypes();
            var loadedTypes = data.GetLoadedSupportedTypes();

             if (supportedTypes.Contains(_uriList) && !loadedTypes.Contains(_uriList))
            {
                _typesToLoad.Enqueue(_uriList);
            }

            if (!data.GetLoadedDataFormats().Contains(DataFormat.Text))
            {
                if (supportedTypes.Contains(_atoms.UTF8_STRING))
                {
                    _typesToLoad.Enqueue(_atoms.UTF8_STRING);
                }
                else if (supportedTypes.Contains(_textPlain))
                {
                    _typesToLoad.Enqueue(_textPlain);
                }
                else if (supportedTypes.Contains(_textPlainAlt))
                {
                    _typesToLoad.Enqueue(_textPlainAlt);
                }
                else if (supportedTypes.Contains(_atoms.UTF16_STRING))
                {
                    _typesToLoad.Enqueue(_atoms.UTF16_STRING);
                }
                else if (supportedTypes.Contains(_atoms.XA_STRING))
                {
                    _typesToLoad.Enqueue(_atoms.XA_STRING);
                }
                else if (supportedTypes.Contains(_atoms.OEMTEXT))
                {
                    _typesToLoad.Enqueue(_atoms.OEMTEXT);
                }
            }
                       
            
            if(loadedTypes.Count < 1)
            {
                var bestType = supportedTypes.FirstOrDefault(t => t != _textPlainAlt && t != _atoms.UTF8_STRING && t != _textPlainAlt && t != _uriList);
                if (bestType != IntPtr.Zero)
                { 
                    _typesToLoad.Enqueue(bestType); 
                }                
            }

            if (_typesToLoad.Count == 0)
            {
                return false;
            }

            ProcessQuery();

            return true;
        }

        public Task<object?> RequestData(DataFormat format, IntPtr typeAtom)
        {
            if (typeAtom == IntPtr.Zero)  return Task.FromResult<object?>(null);
            return _reader.TryGetAsync(format, typeAtom);
        }

        
        private void ProcessQuery()
        {
            if (_typesToLoad.Count > 0)
            {
                IntPtr typeAtom = _typesToLoad.Dequeue();
                DataFormat? format = ClipboardDataFormatHelper.ToDataFormat(typeAtom, _atoms);
                if (_currentDrag !=  null && !_currentDrag.GetLoadedSupportedTypes().Contains(typeAtom) && typeAtom != IntPtr.Zero && format != null)
                {
                    var res = _reader.TryGetAsync(format, typeAtom).GetAwaiter().GetResult();
                    if (res != null)
                    {
                        _currentDrag.SetData(format, res);
                    }
                }
                else
                {
                    ProcessQuery();
                }
            }
            else
            {
                DataReceived?.Invoke();
            }
        }


        public event Action? DataReceived;



        private void InitializeTypeAtoms()
        {
            _uriList = _atoms.GetAtom(ClipboardDataFormatHelper.MimeTypeTextUriList);
            _textPlain = _atoms.GetAtom(ClipboardDataFormatHelper.MimeTextPlain);
            _textPlainAlt = _atoms.GetAtom(ClipboardDataFormatHelper.MimeUTF8_alt);
        }

      


        public void Dispose()
        {
            _currentDrag = null;
            _result.Clear();
        }
    }
}
