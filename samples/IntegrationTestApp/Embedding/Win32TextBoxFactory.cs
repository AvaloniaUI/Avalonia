using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Platform;
using static IntegrationTestApp.Embedding.WinApi;

namespace IntegrationTestApp.Embedding;

internal class Win32TextBoxFactory : INativeTextBoxFactory
{
    public INativeTextBoxImpl CreateControl(IPlatformHandle parent)
    {
        return new Win32TextBox(parent);
    }

    private class Win32TextBox : INativeTextBoxImpl
    {
        private readonly IntPtr _oldWndProc;
        private readonly WndProcDelegate _wndProc;
        private TRACKMOUSEEVENT _trackMouseEvent;

        public Win32TextBox(IPlatformHandle parent)
        {
            var handle = CreateWindowEx(0, "EDIT",
                string.Empty,
                (uint)(WinApi.WindowStyles.WS_CHILD | WinApi.WindowStyles.WS_VISIBLE | WinApi.WindowStyles.WS_BORDER),
                0, 0, 1, 1,
                parent.Handle,
                IntPtr.Zero,
                GetModuleHandle(null),
                IntPtr.Zero);

            _wndProc = new(WndProc);
            _oldWndProc = SetWindowLongPtr(handle, WinApi.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProc));

            _trackMouseEvent.cbSize = Marshal.SizeOf<TRACKMOUSEEVENT>();
            _trackMouseEvent.dwFlags = TME_HOVER | TME_LEAVE;
            _trackMouseEvent.hwndTrack = handle;
            _trackMouseEvent.dwHoverTime = 400;

            Handle = new Win32WindowControlHandle(handle, "HWND");
        }

        public IPlatformHandle Handle { get; }

        public string Text
        {
            get
            {
                var sb = new StringBuilder(256);
                GetWindowText(Handle.Handle, sb, sb.Capacity);
                return sb.ToString();
            }
            set => SetWindowText(Handle.Handle, value);
        }

        public event EventHandler? ContextMenuRequested;
        public event EventHandler? Hovered;
        public event EventHandler? PointerExited;

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_CONTEXTMENU:
                    if (ContextMenuRequested is not null)
                    {
                        ContextMenuRequested?.Invoke(this, EventArgs.Empty);
                        return IntPtr.Zero;
                    }
                    break;
                case WM_MOUSELEAVE:
                    PointerExited?.Invoke(this, EventArgs.Empty);
                    break;
                case WM_MOUSEHOVER:
                    Hovered?.Invoke(this, EventArgs.Empty);
                    break;
                case WM_MOUSEMOVE:
                    TrackMouseEvent(ref _trackMouseEvent);
                    break;

            }

            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }
    }
}
