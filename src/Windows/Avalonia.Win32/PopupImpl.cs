using System;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class PopupImpl : WindowImpl, IPopupImpl
    {

        // This is needed because we are calling virtual methods from constructors
        // One fabulous design decision leads to another, I guess
        [ThreadStatic]
        private static IntPtr s_parentHandle;

        public override void Show()
        {
            UnmanagedMethods.ShowWindow(Handle.Handle, UnmanagedMethods.ShowWindowCommand.ShowNoActivate);
            var parent = UnmanagedMethods.GetParent(Handle.Handle);
            if (parent != IntPtr.Zero)
            {
                IntPtr nextParent = parent;
                while (nextParent != IntPtr.Zero)
                {
                    parent = nextParent;
                    nextParent = UnmanagedMethods.GetParent(parent);
                }

                UnmanagedMethods.SetFocus(parent);
            }
        }

        protected override bool ShouldTakeFocusOnClick => false;

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
                s_parentHandle,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);
            s_parentHandle = IntPtr.Zero;

            var classes = (int)UnmanagedMethods.GetClassLongPtr(result, (int)UnmanagedMethods.ClassLongIndex.GCL_STYLE);

            classes |= (int)UnmanagedMethods.ClassStyles.CS_DROPSHADOW;

            UnmanagedMethods.SetClassLong(result, UnmanagedMethods.ClassLongIndex.GCL_STYLE, new IntPtr(classes));

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

        // This is needed because we are calling virtual methods from constructors
        // One fabulous design decision leads to another, I guess
        static IWindowBaseImpl SaveParentHandle(IWindowBaseImpl parent)
        {
            s_parentHandle = parent.Handle.Handle;
            return parent;
        }

        // This is needed because we are calling virtual methods from constructors
        // One fabulous design decision leads to another, I guess
        public PopupImpl(IWindowBaseImpl parent) : this(SaveParentHandle(parent), false)
        {

        }

        private PopupImpl(IWindowBaseImpl parent, bool dummy) : base()
        {
            PopupPositioner = new ManagedPopupPositioner(new ManagedPopupPositionerPopupImplHelper(parent, MoveResize));
        }

        private void MoveResize(PixelPoint position, Size size, double scaling)
        {
            Move(position);
            Resize(size);
            //TODO: We ignore the scaling override for now
        }

        public IPopupPositioner PopupPositioner { get; }
    }
}
