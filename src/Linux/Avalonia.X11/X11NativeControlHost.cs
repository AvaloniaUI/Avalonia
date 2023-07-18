using System;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.VisualTree;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    // TODO: Actually implement XEmbed instead of simply using XReparentWindow
    internal class X11NativeControlHost : INativeControlHostImpl
    {
        private readonly AvaloniaX11Platform _platform;
        public X11Window Window { get; }

        public X11NativeControlHost(AvaloniaX11Platform platform, X11Window window)
        {
            _platform = platform;
            Window = window;
        }

        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
        {
            var ch = new DumbWindow(_platform.Info);
            XSync(_platform.Display, false);
            return ch;
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            var holder = new DumbWindow(_platform.Info, Window.Handle.Handle);
            Attachment attachment = null;
            try
            {
                var child = create(holder);
                // ReSharper disable once UseObjectOrCollectionInitializer
                // It has to be assigned to the variable before property setter is called so we dispose it on exception
                attachment = new Attachment(_platform.Display, holder, _platform.OrphanedWindow, child);
                attachment.AttachedTo = this;
                return attachment;
            }
            catch
            {
                attachment?.Dispose();
                holder?.Destroy();
                throw;
            }
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle)
        {
            if (!IsCompatibleWith(handle))
                throw new ArgumentException(handle.HandleDescriptor + " is not compatible with the current window",
                    nameof(handle));
            var attachment = new Attachment(_platform.Display, new DumbWindow(_platform.Info, Window.Handle.Handle),
                _platform.OrphanedWindow, handle) { AttachedTo = this };
            return attachment;
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle.HandleDescriptor == "XID";

        private class DumbWindow : INativeControlHostDestroyableControlHandle
        {
            private readonly IntPtr _display;

            public DumbWindow(X11Info x11, IntPtr? parent = null)
            {
                _display = x11.Display;
                /*Handle = XCreateSimpleWindow(x11.Display, XLib.XDefaultRootWindow(_display),
                    0, 0, 1, 1, 0, IntPtr.Zero, IntPtr.Zero);*/
                var attr = new XSetWindowAttributes
                {
                    backing_store = 1,
                    bit_gravity = Gravity.NorthWestGravity,
                    win_gravity = Gravity.NorthWestGravity,
                    
                };

                parent = parent ?? XDefaultRootWindow(x11.Display);

                Handle = XCreateWindow(_display, parent.Value, 0, 0,
                    1,1, 0, 0,
                    (int)CreateWindowArgs.InputOutput,
                    IntPtr.Zero, 
                    new UIntPtr((uint)(SetWindowValuemask.BorderPixel | SetWindowValuemask.BitGravity |
                                       SetWindowValuemask.BackPixel |
                                       SetWindowValuemask.WinGravity | SetWindowValuemask.BackingStore)), ref attr);
            }

            public IntPtr Handle { get; private set; }
            public string HandleDescriptor => "XID";
            public void Destroy()
            {
                if (Handle != IntPtr.Zero)
                {
                    XDestroyWindow(_display, Handle);
                    Handle = IntPtr.Zero;
                }
            }
        }

        private class Attachment : INativeControlHostControlTopLevelAttachment
        {
            private readonly IntPtr _display;
            private readonly IntPtr _orphanedWindow;
            private DumbWindow _holder;
            private IPlatformHandle _child;
            private X11NativeControlHost _attachedTo;
            private bool _mapped;
            
            public Attachment(IntPtr display, DumbWindow holder, IntPtr orphanedWindow, IPlatformHandle child)
            {
                _display = display;
                _orphanedWindow = orphanedWindow;
                _holder = holder;
                _child = child;
                XReparentWindow(_display, child.Handle, holder.Handle, 0, 0);
                XMapWindow(_display, child.Handle);
            }
            
            public void Dispose()
            {
                if (_child != null)
                {
                    XReparentWindow(_display, _child.Handle, _orphanedWindow, 0, 0);
                    _child = null;
                }

                _holder?.Destroy();
                _holder = null;
                _attachedTo = null;
            }

            private void CheckDisposed()
            {
                if (_child == null)
                    throw new ObjectDisposedException("X11 INativeControlHostControlTopLevelAttachment");
            }

            public INativeControlHostImpl AttachedTo
            {
                get => _attachedTo;
                set
                {
                    CheckDisposed();
                    _attachedTo = (X11NativeControlHost)value;
                    if (value == null)
                    {
                        _mapped = false;
                        XUnmapWindow(_display, _holder.Handle);
                        XReparentWindow(_display, _holder.Handle, _orphanedWindow, 0, 0);
                    }
                    else
                    {
                        XReparentWindow(_display, _holder.Handle, _attachedTo.Window.Handle.Handle, 0, 0);
                    }
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is X11NativeControlHost;

            public void HideWithSize(Size size)
            {
                if(_attachedTo == null || _child == null)
                    return;
                if (_mapped)
                {
                    _mapped = false;
                    XUnmapWindow(_display, _holder.Handle);
                }

                size *= _attachedTo.Window.RenderScaling;
                XResizeWindow(_display, _child.Handle,
                    Math.Max(1, (int)size.Width), Math.Max(1, (int)size.Height));
            }
            
            
            
            public void ShowInBounds(Rect bounds)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    throw new InvalidOperationException("The control isn't currently attached to a toplevel");
                bounds *= _attachedTo.Window.RenderScaling;
                
                var pixelRect = new PixelRect((int)bounds.X, (int)bounds.Y, Math.Max(1, (int)bounds.Width),
                    Math.Max(1, (int)bounds.Height));
                XMoveResizeWindow(_display, _child.Handle, 0, 0, pixelRect.Width, pixelRect.Height);
                XMoveResizeWindow(_display, _holder.Handle, pixelRect.X, pixelRect.Y, pixelRect.Width,
                    pixelRect.Height);
                if (!_mapped)
                {
                    XMapWindow(_display, _holder.Handle);
                    XRaiseWindow(_display, _holder.Handle);
                    _mapped = true;
                }
            }
        }
    }
}
