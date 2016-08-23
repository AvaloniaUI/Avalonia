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
        private Avalonia.Controls.Control _content;

        public Avalonia.Controls.Control Content
        {
            get { return _content; }
            set
            {
                if (_host != null)
                    _host.Content = value;
                _content = value;
                
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
            _host = new WinFormsAvaloniaControlHost {Content = _content};
            UnmanagedMethods.SetParent(_host.Handle, hwndParent.Handle);
            return new HandleRef(this, _host.Handle);
        }

        protected override void DestroyWindowCore(HandleRef hwnd)
        {
            DestroyHost();
        }
    }
}
