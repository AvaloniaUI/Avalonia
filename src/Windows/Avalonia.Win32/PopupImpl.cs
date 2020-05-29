using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class PopupImpl : WindowImpl, IPopupImpl
    {
        private bool _hasBoxShadow = true;

        public override void Show()
        {
            UnmanagedMethods.ShowWindow(Handle.Handle, UnmanagedMethods.ShowWindowCommand.ShowNoActivate);
        }

        protected override IntPtr CreateWindowOverride(ushort atom)
        {
            UnmanagedMethods.WindowStyles style =
                UnmanagedMethods.WindowStyles.WS_POPUP |
                UnmanagedMethods.WindowStyles.WS_CLIPSIBLINGS;

            UnmanagedMethods.WindowStyles exStyle =
                UnmanagedMethods.WindowStyles.WS_EX_TOOLWINDOW |
                UnmanagedMethods.WindowStyles.WS_EX_TOPMOST;

            var result = UnmanagedMethods.CreateWindowEx(
                (int)exStyle,
                atom,
                null,
                (uint)style,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            EnableBoxShadow(result, _hasBoxShadow);

            return result;
        }

        protected override IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_MOUSEACTIVATE:
                    return (IntPtr)UnmanagedMethods.MouseActivate.MA_NOACTIVATE;
                default:
                    return base.WndProc(hWnd, msg, wParam, lParam);
            }
        }

        public PopupImpl(IWindowBaseImpl parent)
        {
            PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(parent, MoveResize));
        }

        private void MoveResize(PixelPoint position, Size size, double scaling)
        {
            Move(position);
            Resize(size);
            //TODO: We ignore the scaling override for now
        }

        private void EnableBoxShadow (IntPtr hwnd, bool enabled)
        {
            var classes = (int)UnmanagedMethods.GetClassLongPtr(hwnd, (int)UnmanagedMethods.ClassLongIndex.GCL_STYLE);

            if (enabled)
            {
                classes |= (int)UnmanagedMethods.ClassStyles.CS_DROPSHADOW;
            }
            else
            {
                classes &= ~(int)UnmanagedMethods.ClassStyles.CS_DROPSHADOW;
            }

            UnmanagedMethods.SetClassLong(hwnd, UnmanagedMethods.ClassLongIndex.GCL_STYLE, new IntPtr(classes));
        }

        public void SetWindowManagerAddShadowHint(bool enabled)
        {
            _hasBoxShadow = enabled;

            if (Handle != null)
            {
                EnableBoxShadow(Handle.Handle, enabled);
            }
        }

        public IPopupPositioner PopupPositioner { get; }
    }
}
