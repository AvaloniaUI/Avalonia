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
    class WindowHost : UserControl
    {
        public WindowHost()
        {
            AutoScroll = true;
            VerticalScroll.Enabled = true;
            HorizontalScroll.Enabled = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            Text = "ScrollableArea";
            Controls.Add(_windowHost);
            _windowHost.Anchor = AnchorStyles.None;
            _timer.Tick += delegate
            {
                ReloadSettings();
                FixWindow();
            };
        }

        private void ReloadSettings()
        {
            var bkg = Settings.Background;
            var color = System.Drawing.ColorTranslator.FromHtml(bkg);
            if (BackColor != color)
                BackColor = color;
        }

        private Control _windowHost = new Control() {Text = "WindowWrapper"};
        private Timer _timer = new Timer {Enabled = true, Interval = 50};
        private IntPtr _hWnd;
        private int _desiredWidth;
        private int _desiredHeight;

        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;

        protected override void WndProc(ref Message m)
        {
            if ((m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL)
                && (((int) m.WParam & 0xFFFF) == 5))
            {
                // Change SB_THUMBTRACK to SB_THUMBPOSITION
                m.WParam = (IntPtr) (((int) m.WParam & ~0xFFFF) | 4);
            }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var b = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(b, e.ClipRectangle);
        }

        void FixPosition()
        {
            var newScrollSize =  new Size(_desiredWidth, _desiredHeight);
            if (AutoScrollMinSize != newScrollSize)
                AutoScrollMinSize = newScrollSize;

            var width = Width - AutoScrollMargin.Width;
            var height = Height - AutoScrollMargin.Height;
            var x = Math.Max(0, (width - _windowHost.Width)/2);
            var y = Math.Max(0, (height - _windowHost.Height)/2);

            var newLoc = new Point(x - HorizontalScroll.Value, y - VerticalScroll.Value);
            if(_windowHost.Location != newLoc)
                _windowHost.Location = newLoc;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
            base.Dispose(disposing);
        }

        public void SetWindow(IntPtr hWnd)
        {
            if (_hWnd != IntPtr.Zero)
                WinApi.SendMessage(_hWnd, WinApi.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _hWnd = hWnd;
            if (_hWnd != IntPtr.Zero)
            {
                WinApi.SetParent(hWnd, _windowHost.Handle);
                FixWindow();
            }
        }
        
        void FixWindow()
        {
            if (_hWnd != IntPtr.Zero)
            {
                WinApi.RECT rc;
                WinApi.GetWindowRect(_hWnd, out rc);
                _desiredWidth = rc.Right - rc.Left;
                _desiredHeight = rc.Bottom - rc.Top;
                var pt = _windowHost.PointToClient(new Point(rc.Left, rc.Top));

                if (!(pt.Y == 0 && pt.X == 0 && _desiredWidth == _windowHost.Width && _desiredHeight == _windowHost.Height))
                {
                    _windowHost.Width = _desiredWidth;
                    _windowHost.Height = _desiredHeight;
                    WinApi.MoveWindow(_hWnd, 0, 0, _desiredWidth, _desiredHeight, true);
                }
                FixPosition();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            FixPosition();
            base.OnResize(e);
        }
    }
}
