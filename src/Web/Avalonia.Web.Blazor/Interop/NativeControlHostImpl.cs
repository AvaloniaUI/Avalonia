using System.Diagnostics.CodeAnalysis;

using Avalonia.Controls.Platform;
using Avalonia.Platform;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Avalonia.Web.Blazor.Interop
{

    internal class NativeControlHostInterop : INativeControlHostImpl
    {
        private const string CreateDefaultChildSymbol = "NativeControlHost.CreateDefaultChild";
        private const string CreateAttachmentSymbol = "NativeControlHost.CreateAttachment";
        private const string GetReferenceSymbol = "NativeControlHost.GetReference";

        private readonly AvaloniaModule _module;
        private readonly ElementReference _hostElement;

        public NativeControlHostInterop(AvaloniaModule module, ElementReference element)
        {
            _module = module;
            _hostElement = element;
        }

        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
        {
            var element = _module.Invoke<IJSInProcessObjectReference>(CreateDefaultChildSymbol);
            return new JSObjectControlHandle(element);
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            Attachment? a = null;
            try
            {
                using var hostElementJsReference = _module.Invoke<IJSInProcessObjectReference>(GetReferenceSymbol, _hostElement);                
                var child = create(new JSObjectControlHandle(hostElementJsReference));
                var attachmenetReference = _module.Invoke<IJSInProcessObjectReference>(CreateAttachmentSymbol);
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
            var attachmenetReference = _module.Invoke<IJSInProcessObjectReference>(CreateAttachmentSymbol);
            var a = new Attachment(attachmenetReference, handle);
            a.AttachedTo = this;
            return a;
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle is JSObjectControlHandle;

        class Attachment : INativeControlHostControlTopLevelAttachment
        {
            private const string InitializeWithChildHandleSymbol = "InitializeWithChildHandle";
            private const string AttachToSymbol = "AttachTo";
            private const string ShowInBoundsSymbol = "ShowInBounds";
            private const string HideWithSizeSymbol = "HideWithSize";
            private const string ReleaseChildSymbol = "ReleaseChild";

            private IJSInProcessObjectReference? _native;
            private NativeControlHostInterop? _attachedTo;

            public Attachment(IJSInProcessObjectReference native, IPlatformHandle handle)
            {
                _native = native;
                _native.InvokeVoid(InitializeWithChildHandleSymbol, ((JSObjectControlHandle)handle).Object);
            }

            public void Dispose()
            {
                if (_native != null)
                {
                    _native.InvokeVoid(ReleaseChildSymbol);
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

                    var host = (NativeControlHostInterop?)value;
                    if (host == null)
                    {
                        _native.InvokeVoid(AttachToSymbol);
                    }
                    else
                    {
                        _native.InvokeVoid(AttachToSymbol, host._hostElement);
                    }
                    _attachedTo = host;
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is NativeControlHostInterop;

            public void HideWithSize(Size size)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    return;

                _native.InvokeVoid(HideWithSizeSymbol, Math.Max(1, (float)size.Width), Math.Max(1, (float)size.Height));
            }

            public void ShowInBounds(Rect bounds)
            {
                CheckDisposed();

                if (_attachedTo == null)
                    throw new InvalidOperationException("Native control isn't attached to a toplevel");

                bounds = new Rect(bounds.X, bounds.Y, Math.Max(1, bounds.Width),
                    Math.Max(1, bounds.Height));

                _native.InvokeVoid(ShowInBoundsSymbol, (float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height);
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
