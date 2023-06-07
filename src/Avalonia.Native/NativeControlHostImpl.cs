using System;
using Avalonia.Controls.Platform;
using Avalonia.MicroCom;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.VisualTree;
using MicroCom.Runtime;

namespace Avalonia.Native
{
    class NativeControlHostImpl : IDisposable, INativeControlHostImpl
    {
        private IAvnNativeControlHost _host;

        public NativeControlHostImpl(IAvnNativeControlHost host)
        {
            _host = host;
        }

        public void Dispose()
        {
            _host?.Dispose();
            _host = null;
        }

        class DestroyableNSView : INativeControlHostDestroyableControlHandle
        {
            private IAvnNativeControlHost _impl;
            private IntPtr _nsView;

            public DestroyableNSView(IAvnNativeControlHost impl)
            {
                _impl = MicroComRuntime.CloneReference(impl);
                _nsView = _impl.CreateDefaultChild(IntPtr.Zero);
            }

            public IntPtr Handle => _nsView;
            public string HandleDescriptor => "NSView";
            public void Destroy()
            {
                if (_impl != null)
                {
                    _impl.DestroyDefaultChild(_nsView);
                    _impl.Dispose();
                    _impl = null;
                    _nsView = IntPtr.Zero;
                }
            }
        }
        
        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent) 
            => new DestroyableNSView(_host);

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(
            Func<IPlatformHandle, IPlatformHandle> create)
        {
            var a = new Attachment(_host.CreateAttachment());
            try
            {
                var child = create(a.GetParentHandle());
                a.InitWithChild(child);
                a.AttachedTo = this;
                return a;
            }
            catch
            {
                a.Dispose();
                throw;
            }
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle)
        {
            var a = new Attachment(_host.CreateAttachment());
            a.InitWithChild(handle);
            a.AttachedTo = this;
            return a;
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle.HandleDescriptor == "NSView";

        class Attachment : INativeControlHostControlTopLevelAttachment
        {
            private IAvnNativeControlHostTopLevelAttachment _native;
            private NativeControlHostImpl _attachedTo;
            public IPlatformHandle GetParentHandle() => new PlatformHandle(_native.ParentHandle, "NSView");
            public Attachment(IAvnNativeControlHostTopLevelAttachment native)
            {
                _native = native;
            }
            
            public void Dispose()
            {
                if (_native != null)
                {
                    _native.ReleaseChild();
                    _native.Dispose();
                    _native = null;
                }
            }

            public INativeControlHostImpl AttachedTo
            {
                get => _attachedTo;
                set
                {
                    var host = (NativeControlHostImpl)value;
                    if(host == null)
                        _native.AttachTo(null);
                    else
                        _native.AttachTo(host._host);
                    _attachedTo = host;
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is NativeControlHostImpl;

            public void HideWithSize(Size size)
            {
                _native.HideWithSize(Math.Max(1, (float)size.Width), Math.Max(1, (float)size.Height));
            }
            
            public void ShowInBounds(Rect bounds)
            {
                if (_attachedTo == null)
                    throw new InvalidOperationException("Native control isn't attached to a toplevel");
                bounds = new Rect(bounds.X, bounds.Y, Math.Max(1, bounds.Width),
                    Math.Max(1, bounds.Height));
                _native.ShowInBounds((float) bounds.X, (float) bounds.Y, (float) bounds.Width, (float) bounds.Height);
            }

            public void InitWithChild(IPlatformHandle handle) 
                => _native.InitializeWithChildHandle(handle.Handle);
        }
    }
}
