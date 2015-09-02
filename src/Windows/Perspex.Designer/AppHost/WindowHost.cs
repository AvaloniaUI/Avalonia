using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Perspex.Designer.AppHost
{
    class WindowHost : Control
    {
        public WindowHost()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.FillRectangle(Brushes.AliceBlue, ClientRectangle);
        }

        private IntPtr _hWnd;

        public void SetWindow(IntPtr hWnd)
        {
            _hWnd = hWnd;
            WinApi.SetParent(hWnd, Handle);
            FixWindow();
        }

        void FixWindow()
        {
            WinApi.MoveWindow(_hWnd, 0, 0, Width, Height, true);
        }

        protected override void OnResize(EventArgs e)
        {
            FixWindow();
            base.OnResize(e);
        }
    }
}
