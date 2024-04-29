using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class Win32NativeControlHost : INativeControlHostImpl
    {
        private readonly bool _useLayeredWindow;
        public WindowImpl Window { get; }

        public Win32NativeControlHost(WindowImpl window, bool useLayeredWindow)
        {
            _useLayeredWindow = useLayeredWindow;
            Window = window;
        }

        private void AssertCompatible(IPlatformHandle handle)
        {
            if (!IsCompatibleWith(handle))
                throw new ArgumentException($"Don't know what to do with {handle.HandleDescriptor}");
        }

        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
        {
            AssertCompatible(parent);
            return new DumbWindow(false, parent.Handle);
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            var holder = new DumbWindow(_useLayeredWindow, Window.Handle.Handle);
            Win32NativeControlAttachment? attachment = null;
            try
            {
                var child = create(holder);
                // ReSharper disable once UseObjectOrCollectionInitializer
                // It has to be assigned to the variable before property setter is called so we dispose it on exception
                attachment = new Win32NativeControlAttachment(holder, child);
                attachment.AttachedTo = this;
                return attachment;
            }
            catch
            {
                attachment?.Dispose();
                holder.Destroy();
                throw;
            }
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle)
        {
            AssertCompatible(handle);
            return new Win32NativeControlAttachment(new DumbWindow(_useLayeredWindow, Window.Handle.Handle),
                handle) { AttachedTo = this };
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle.HandleDescriptor == "HWND";

        private class DumbWindow : IDisposable, INativeControlHostDestroyableControlHandle
        {
            public IntPtr Handle { get;}
            public string HandleDescriptor => "HWND";
            public void Destroy() => Dispose();

            private readonly UnmanagedMethods.WndProc _wndProcDelegate;
            private readonly string _className;

            public DumbWindow(bool layered = false, IntPtr? parent = null)
            {
                _wndProcDelegate = WndProc;
                var wndClassEx = new UnmanagedMethods.WNDCLASSEX
                {
                    cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
                    hInstance = UnmanagedMethods.GetModuleHandle(null),
                    lpfnWndProc = _wndProcDelegate,
                    lpszClassName = _className = "AvaloniaDumbWindow-" + Guid.NewGuid(),
                };

                var atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);
                Handle = UnmanagedMethods.CreateWindowEx(
                    layered ? (int)UnmanagedMethods.WindowStyles.WS_EX_LAYERED : 0,
                    atom,
                    null,
                    (int)UnmanagedMethods.WindowStyles.WS_CHILD,
                    0,
                    0,
                    640,
                    480,
                    parent ?? OffscreenParentWindow.Handle,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero);

                if (Handle == IntPtr.Zero)
                    throw new InvalidOperationException("Unable to create child window for native control host. Application manifest with supported OS list might be required.");

                if (layered)
                    UnmanagedMethods.SetLayeredWindowAttributes(Handle, 0, 255,
                        UnmanagedMethods.LayeredWindowFlags.LWA_ALPHA);
            }

            private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
            {
                return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
            }

            private void ReleaseUnmanagedResources()
            {
                UnmanagedMethods.DestroyWindow(Handle);
                UnmanagedMethods.UnregisterClass(_className, UnmanagedMethods.GetModuleHandle(null));
            }

            public void Dispose()
            {
                ReleaseUnmanagedResources();
                GC.SuppressFinalize(this);
            }

            ~DumbWindow()
            {
                ReleaseUnmanagedResources();
            }
        }

        private class Win32NativeControlAttachment : INativeControlHostControlTopLevelAttachment
        {
            private DumbWindow? _holder;
            private IPlatformHandle? _child;
            private Win32NativeControlHost? _attachedTo;

            public Win32NativeControlAttachment(DumbWindow holder, IPlatformHandle child)
            {
                _holder = holder;
                _child = child;
                UnmanagedMethods.SetParent(child.Handle, _holder.Handle);
                UnmanagedMethods.ShowWindow(child.Handle, UnmanagedMethods.ShowWindowCommand.ShowNoActivate);
            }

            [MemberNotNull(nameof(_holder))]
            private void CheckDisposed()
            {
                if (_holder == null)
                    throw new ObjectDisposedException(nameof(Win32NativeControlAttachment));
            }

            public void Dispose()
            {
                if (_child != null)
                    UnmanagedMethods.SetParent(_child.Handle, OffscreenParentWindow.Handle);
                _holder?.Dispose();
                _holder = null;
                _child = null;
                _attachedTo = null;
            }

            public INativeControlHostImpl? AttachedTo
            {
                get => _attachedTo;
                set
                {
                    CheckDisposed();
                    _attachedTo = value as Win32NativeControlHost;
                    if (_attachedTo == null)
                    {
                        UnmanagedMethods.ShowWindow(_holder.Handle, UnmanagedMethods.ShowWindowCommand.Hide);
                        UnmanagedMethods.SetParent(_holder.Handle, OffscreenParentWindow.Handle);
                    }
                    else
                        UnmanagedMethods.SetParent(_holder.Handle, _attachedTo.Window.Handle.Handle);
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is Win32NativeControlHost;

            public void HideWithSize(Size size)
            {
                CheckDisposed();
                UnmanagedMethods.SetWindowPos(_holder.Handle, IntPtr.Zero,
                    -100, -100, 1, 1,
                    UnmanagedMethods.SetWindowPosFlags.SWP_HIDEWINDOW |
                    UnmanagedMethods.SetWindowPosFlags.SWP_NOACTIVATE);
                if (_attachedTo == null || _child == null)
                    return;
                size *= _attachedTo.Window.RenderScaling;
                UnmanagedMethods.MoveWindow(_child.Handle, 0, 0,
                    Math.Max(1, (int)size.Width), Math.Max(1, (int)size.Height), false);
            }
            
            public unsafe void ShowInBounds(Rect bounds)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    throw new InvalidOperationException("The control isn't currently attached to a toplevel");
                bounds *= _attachedTo.Window.RenderScaling;
                var pixelRect = new PixelRect((int)bounds.X, (int)bounds.Y, Math.Max(1, (int)bounds.Width),
                    Math.Max(1, (int)bounds.Height));

                if (_child is not null)
                {
                    UnmanagedMethods.MoveWindow(_child.Handle, 0, 0, pixelRect.Width, pixelRect.Height, true);
                }

                UnmanagedMethods.SetWindowPos(_holder.Handle, IntPtr.Zero, pixelRect.X, pixelRect.Y, pixelRect.Width,
                    pixelRect.Height,
                    UnmanagedMethods.SetWindowPosFlags.SWP_SHOWWINDOW
                    | UnmanagedMethods.SetWindowPosFlags.SWP_NOZORDER
                    | UnmanagedMethods.SetWindowPosFlags.SWP_NOACTIVATE);
                
                UnmanagedMethods.InvalidateRect(_attachedTo.Window.Handle.Handle, null, false);
            }
        }

    }
}
