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
            BackColor = Color.AliceBlue;
            _textBox = new TextBox {Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Visible = false};
            Controls.Add(_textBox);
        }
        
        private IntPtr _hWnd;
        private TextBox _textBox;

        public string PlaceholderText
        {
            get { return _textBox.Text; }
            set
            {
                _textBox.Text = value;
                FixWindow();
            }
        }

        

        public void SetWindow(IntPtr hWnd)
        {
            if (_hWnd != IntPtr.Zero)
                WinApi.SendMessage(_hWnd, WinApi.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _hWnd = hWnd;
            if (_hWnd != IntPtr.Zero)
            {
                WinApi.SetParent(hWnd, Handle);
                FixWindow();
            }
        }

        void FixWindow()
        {
            if (_hWnd != IntPtr.Zero)
                WinApi.MoveWindow(_hWnd, 0, 0, Width, Height, true);
            _textBox.Visible = _hWnd == IntPtr.Zero && !string.IsNullOrWhiteSpace(_textBox.Text);
        }

        protected override void OnResize(EventArgs e)
        {
            FixWindow();
            base.OnResize(e);
        }
    }
}
