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
    using System.Reactive.Linq;
    using System.Runtime.InteropServices;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Diagnostics;
    using Perspex.Input;
    using Perspex.Input.Raw;
    using Perspex.Layout;
    using Perspex.Platform;
    using Perspex.Rendering;
    using Perspex.Threading;
    using Perspex.Windows.Input;
    using Perspex.Windows.Interop;
    using Perspex.Windows.Threading;
    using Splat;

    public class Window : ContentControl, ILayoutRoot, IRenderRoot, ICloseable
    {
        private UnmanagedMethods.WndProc wndProcDelegate;

        private string className;

        private IRenderer renderer;

        private IInputManager inputManager;

        public Window()
        {
            IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();

            this.CreateWindow();
            Size clientSize = this.ClientSize;
            this.LayoutManager = new LayoutManager(this);
            this.RenderManager = new RenderManager();
            this.renderer = factory.CreateRenderer(this.Handle, (int)clientSize.Width, (int)clientSize.Height);
            this.inputManager = Locator.Current.GetService<IInputManager>();
            this.Template = ControlTemplate.Create<Window>(this.DefaultTemplate);

            this.LayoutManager.LayoutNeeded.Subscribe(x => 
            {
                WindowsDispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Render, 
                    () =>
                    {
                        this.LayoutManager.ExecuteLayoutPass();
                        this.renderer.Render(this);
                        this.RenderManager.RenderFinished();
                    });
            });

            this.RenderManager.RenderNeeded
                .Where(_ => !this.LayoutManager.LayoutQueued)
                .Subscribe(x =>
            {
                WindowsDispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Render,
                    () =>
                    {
                        if (!this.LayoutManager.LayoutQueued)
                        {
                            this.renderer.Render(this);
                            this.RenderManager.RenderFinished();
                        }
                    });
            });
        }

        public event EventHandler Activated;

        public event EventHandler Closed;

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

        public IRenderManager RenderManager
        {
            get;
            private set;
        }

        public void Show()
        {
            UnmanagedMethods.ShowWindow(this.Handle, 1);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                Window window = new Window
                {
                    Content = new DevTools
                    {
                        Root = this,
                    },
                };

                window.Show();
            }
        }

        private Control DefaultTemplate(Window c)
        {
            Border border = new Border();
            border.Background = new Perspex.Media.SolidColorBrush(0xffffffff);
            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.Bind(
                ContentPresenter.ContentProperty, 
                this.GetObservable(Window.ContentProperty),
                BindingPriority.Style);
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

            System.Diagnostics.Debug.WriteLine("Registered class " + this.className);

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

        private void OnActivated()
        {
            WindowsKeyboardDevice.Instance.WindowActivated(this);

            if (this.Activated != null)
            {
                this.Activated(this, EventArgs.Empty);
            }
        }

        private void OnResized(int width, int height)
        {
            this.renderer.Resize(width, height);
            this.LayoutManager.ExecuteLayoutPass();
            this.renderer.Render(this);
            this.RenderManager.RenderFinished();
        }

        private void OnClosed()
        {
            if (this.Closed != null)
            {
                this.Closed(this, EventArgs.Empty);
            }
        }

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            RawInputEventArgs e = null;

            WindowsMouseDevice.Instance.CurrentWindow = this;

            switch ((UnmanagedMethods.WindowsMessage)msg)
            {
                case UnmanagedMethods.WindowsMessage.WM_ACTIVATE:
                    this.OnActivated();
                    break;

                case UnmanagedMethods.WindowsMessage.WM_DESTROY:
                    this.OnClosed();
                    break;

                case UnmanagedMethods.WindowsMessage.WM_KEYDOWN:
                    WindowsKeyboardDevice.Instance.UpdateKeyStates();
                    e = new RawKeyEventArgs(
                            WindowsKeyboardDevice.Instance,
                            RawKeyEventType.KeyDown,
                            KeyInterop.KeyFromVirtualKey((int)wParam),
                            WindowsKeyboardDevice.Instance.StringFromVirtualKey((uint)wParam));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONDOWN:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance, 
                        this, 
                        RawMouseEventType.LeftButtonDown,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_LBUTTONUP:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance, 
                        this, 
                        RawMouseEventType.LeftButtonUp,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_MOUSEMOVE:
                    e = new RawMouseEventArgs(
                        WindowsMouseDevice.Instance, 
                        this, 
                        RawMouseEventType.Move,
                        new Point((uint)lParam & 0xffff, (uint)lParam >> 16));
                    break;

                case UnmanagedMethods.WindowsMessage.WM_SIZE:
                    this.OnResized((int)lParam & 0xffff, (int)lParam >> 16);
                    return IntPtr.Zero;
            }

            if (e != null)
            {
                this.inputManager.Process(e);
            }

            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
