using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Interop;
using Avalonia.Controls;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.Embedding
{
    public class WpfAvaloniaControlHost : HwndHost
    {
        private WinFormsAvaloniaControlHost _host;
        private EmbeddableControl _child;

        public EmbeddableControl Child
        {
            get { return _child; }
            set
            {
                if (_host != null)
                    _host.Child = value;
                _child = value;
                
            }
        }

        void DestroyHost()
        {
            _host?.Dispose();
            _host = null;
        }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            DestroyHost();
            _host = new WinFormsAvaloniaControlHost {Child = _child};
            UnmanagedMethods.SetParent(_host.Handle, hwndParent.Handle);
            return new HandleRef(this, _host.Handle);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyHost();
        }
    }
}
