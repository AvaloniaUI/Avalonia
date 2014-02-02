// -----------------------------------------------------------------------
// <copyright file="Window.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Windows
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using Perspex.Controls;
    using Perspex.Layout;
    using Perspex.Windows.Interop;
    using Perspex.Windows.Threading;

    public class Window : ContentControl, ILayoutRoot
    {
        public static readonly PerspexProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<Window>();

        private UnmanagedMethods.WndProc wndProcDelegate;

        private string className;

        private Renderer renderer;

        static Window()
        {
            FontSizeProperty.OverrideDefaultValue(typeof(Window), 18.0);
        }

        public Window()
        {
            this.CreateWindow();
            Size clientSize = this.ClientSize;
            this.LayoutManager = new LayoutManager();
            this.renderer = new Renderer(this.Handle, (int)clientSize.Width, (int)clientSize.Height);

            this.LayoutManager.LayoutNeeded.Subscribe(x => 
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Render, 
                    () =>
                    {
                        this.LayoutManager.ExecuteLayoutPass();
                        this.renderer.Render(this);
                    });
            });
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

        public ILayoutManager LayoutManager
        {
            get;
            private set;
        }

        public void Show()
        {
            UnmanagedMethods.ShowWindow(this.Handle, 4);
        }

        protected override Visual DefaultTemplate()
        {
            Border border = new Border();
            border.Background = new Perspex.Media.SolidColorBrush(0xffffffff);
            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.SetValue(ContentPresenter.ContentProperty, this.GetObservable(Window.ContentProperty));
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

        private void MouseMove(Visual visual, Point p)
        {
            Control control = visual as Control;

            if (control != null)
            {
                control.IsMouseOver = visual.Bounds.Contains(p);
            }

            foreach (Visual child in visual.VisualChildren)
            {
                this.MouseMove(child, p - visual.Bounds.Position);
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

                case UnmanagedMethods.WindowsMessage.WM_MOUSEMOVE:
                    this.MouseMove(this, new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_SIZE:
                    this.renderer.Resize((int)lParam & 0xffff, (int)lParam >> 16);
                    this.InvalidateMeasure();
                    return IntPtr.Zero;
            }

            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
