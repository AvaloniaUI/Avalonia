using System;
using System.ComponentModel;
using System.Windows.Forms;
using Avalonia.Controls;
using Avalonia.Win32.Interop;
using Control = System.Windows.Forms.Control;

namespace Avalonia.Win32.Embedding
{
    [ToolboxItem(true)]
    public class WinFormsAvaloniaControlHost : Control
    {
        private EmbeddableControl _child;

        public EmbeddableControl Child
        {
            get { return _child; }
            set
            {
                if (value !=null && value.PlatformImpl.Handle.HandleDescriptor != PlatformConstants.WindowHandleType)
                    throw new ArgumentException("Invalid widget for embedding, don't know what to do with " +
                                                value.PlatformImpl.Handle.HandleDescriptor);
                if (_child != null)
                {
                    _child.PlatformImpl.Hide();
                    UnmanagedMethods.SetParent(_child.PlatformImpl.Handle.Handle, EmbeddedWindowImpl.DefaultParentWindow);
                }
                _child = value;
                if (_child != null)
                {
                    UnmanagedMethods.SetParent(_child.PlatformImpl.Handle.Handle, Handle);
                    _child.Prepare();
                    FixPosition();
                    
                }
            }
        }


        void FixPosition()
        {
            if (_child != null && Width > 0 && Height > 0)
                UnmanagedMethods.MoveWindow(_child.PlatformImpl.Handle.Handle, 0, 0, Width, Height, true);
        }

        public WinFormsAvaloniaControlHost()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnResize(EventArgs e)
        {
            FixPosition();
            base.OnResize(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Child == null)
                base.OnPaint(e);
        }
    }
}
