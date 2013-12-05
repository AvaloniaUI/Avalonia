namespace Perspex.Windows
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using Perspex.Controls;
    using Perspex.Windows.Interop;
    using SharpDX.Direct2D1;
    using SharpDX.DXGI;

    public class Window : ContentControl
    {
        private UnmanagedMethods.WndProc wndProcDelegate;

        private string className;

        private Renderer renderer;

        public Window()
        {
            this.CreateWindow();

            Size clientSize = this.ClientSize;
            this.renderer = new Renderer(this.Handle, (int)clientSize.Width, (int)clientSize.Height);
        }

        public Size ClientSize
        {
            get
            {
                UnmanagedMethods.RECT rect;
                UnmanagedMethods.GetClientRect(this.Handle, out rect);
                return new Size(rect.right, rect.bottom);
            }
        }

        public IntPtr Handle
        {
            get;
            private set;
        }

        public void Show()
        {
            UnmanagedMethods.ShowWindow(this.Handle, 4);
            this.Measure(this.ClientSize);
            this.Arrange(new Rect(this.ClientSize));
            this.renderer.Render(this);
        }

        protected override Visual DefaultTemplate()
        {
            Border border = new Border();
            border.Background = new Perspex.Media.SolidColorBrush(0xff808080);
            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(ContentProperty));
            border.Content = contentPresenter;
            return border;
        }

        private void CreateWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            this.wndProcDelegate = new UnmanagedMethods.WndProc(this.WndProc);

            this.className = Guid.NewGuid().ToString();

            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(UnmanagedMethods.WNDCLASSEX)),
                style = 0,
                lpfnWndProc = this.wndProcDelegate,
                hInstance = Marshal.GetHINSTANCE(this.GetType().Module),
                hCursor = UnmanagedMethods.LoadCursor(IntPtr.Zero, (int)UnmanagedMethods.Cursor.IDC_ARROW),
                hbrBackground = (IntPtr)5,
                lpszClassName = this.className,
            };

            ushort atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            this.Handle = UnmanagedMethods.CreateWindowEx(
                0,
                atom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (this.Handle == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                ////case UnmanagedMethods.WindowsMessage.WM_DESTROY:
                ////    this.OnClosed();
                ////    break;

                ////case UnmanagedMethods.WindowsMessage.WM_KEYDOWN:
                ////    InputManager.Current.ProcessInput(
                ////        new RawKeyEventArgs(
                ////            keyboard,
                ////            RawKeyEventType.KeyDown,
                ////            KeyInterop.KeyFromVirtualKey((int)wParam)));
                ////    break;

                ////case UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN:
                ////    InputManager.Current.ProcessInput(new RawMouseEventArgs(mouse, RawMouseEventType.LeftButtonDown));
                ////    break;

                ////case UnmanagedMethods.WindowsMessage.WM_LBUTTONUP:
                ////    InputManager.Current.ProcessInput(new RawMouseEventArgs(mouse, RawMouseEventType.LeftButtonUp));
                ////    break;

                ////case UnmanagedMethods.WindowsMessage.WM_MOUSEMOVE:
                ////    InputManager.Current.ProcessInput(new RawMouseEventArgs(mouse, RawMouseEventType.Move));
                ////    break;

                ////case UnmanagedMethods.WindowsMessage.WM_SIZE:
                ////    if (this.renderTarget != null)
                ////    {
                ////        this.renderTarget.Resize(new SharpDX.DrawingSize((int)lParam & 0xffff, (int)lParam >> 16));
                ////    }

                ////    this.OnResized();
                ////    return IntPtr.Zero;
            }

            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
