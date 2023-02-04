using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.Browser
{
    internal class BrowserNativeControlHost : INativeControlHostImpl
    {
        private readonly JSObject _hostElement;

        public BrowserNativeControlHost(JSObject element)
        {
            _hostElement = element;
        }

        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
        {
            var element = NativeControlHostHelper.CreateDefaultChild(null);
            return new JSObjectControlHandle(element);
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            Attachment? a = null;
            try
            {
                var child = create(new JSObjectControlHandle(_hostElement));
                var attachmenetReference = NativeControlHostHelper.CreateAttachment();
                // It has to be assigned to the variable before property setter is called so we dispose it on exception
#pragma warning disable IDE0017 // Simplify object initialization
                a = new Attachment(attachmenetReference, child);
#pragma warning restore IDE0017 // Simplify object initialization
                a.AttachedTo = this;
                return a;
            }
            catch
            {
                a?.Dispose();
                throw;
            }
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle)
        {
            var attachmenetReference = NativeControlHostHelper.CreateAttachment();
            var a = new Attachment(attachmenetReference, handle);
            a.AttachedTo = this;
            return a;
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle is JSObjectControlHandle;

        private class Attachment : INativeControlHostControlTopLevelAttachment
        {
            private JSObject? _native;
            private BrowserNativeControlHost? _attachedTo;

            public Attachment(JSObject native, IPlatformHandle handle)
            {
                _native = native;
                NativeControlHostHelper.InitializeWithChildHandle(_native, ((JSObjectControlHandle)handle).Object);
            }

            public void Dispose()
            {
                if (_native != null)
                {
                    NativeControlHostHelper.ReleaseChild(_native);
                    _native.Dispose();
                    _native = null;
                }
            }

            public INativeControlHostImpl? AttachedTo
            {
                get => _attachedTo!;
                set
                {
                    CheckDisposed();

                    var host = (BrowserNativeControlHost?)value;
                    if (host == null)
                    {
                        NativeControlHostHelper.AttachTo(_native, null);
                    }
                    else
                    {
                        NativeControlHostHelper.AttachTo(_native, host._hostElement);
                    }
                    _attachedTo = host;
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is BrowserNativeControlHost;

            public void HideWithSize(Size size)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    return;

                NativeControlHostHelper.HideWithSize(_native, Math.Max(1, size.Width), Math.Max(1, size.Height));
            }

            public void ShowInBounds(Rect bounds)
            {
                CheckDisposed();

                if (_attachedTo == null)
                    throw new InvalidOperationException("Native control isn't attached to a toplevel");

                bounds = new Rect(bounds.X, bounds.Y, Math.Max(1, bounds.Width),
                    Math.Max(1, bounds.Height));

                NativeControlHostHelper.ShowInBounds(_native, bounds.X, bounds.Y, bounds.Width, bounds.Height);
            }

            [MemberNotNull(nameof(_native))]
            private void CheckDisposed()
            {
                if (_native == null)
                    throw new ObjectDisposedException(nameof(Attachment));
            }
        }
    }
}
