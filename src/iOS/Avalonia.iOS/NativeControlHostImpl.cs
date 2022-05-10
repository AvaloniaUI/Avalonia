#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using CoreGraphics;
using ObjCRuntime;
using UIKit;

namespace Avalonia.iOS
{
    internal class NativeControlHostImpl : INativeControlHostImpl
    {
        private readonly AvaloniaView _avaloniaView;

        public NativeControlHostImpl(AvaloniaView avaloniaView)
        {
            _avaloniaView = avaloniaView;
        }

        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
        {
            return new UIViewControlHandle(new UIView(), true);
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            var holder = new UIViewControlHandle(_avaloniaView, false);
            NativeControlAttachment? attachment = null;
            try
            {
                var child = create(holder);
                // It has to be assigned to the variable before property setter is called so we dispose it on exception
#pragma warning disable IDE0017 // Simplify object initialization
                attachment = new NativeControlAttachment(child);
#pragma warning restore IDE0017 // Simplify object initialization
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
            return new NativeControlAttachment(handle)
            {
                AttachedTo = this
            };
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle.HandleDescriptor == UIViewControlHandle.UIViewDescriptor;

        private class ViewHolder : UIView
        {
            public ViewHolder(IntPtr handle) : base(new NativeHandle(handle))
            {
                
            }
        }
        
        private class NativeControlAttachment : INativeControlHostControlTopLevelAttachment
        {
            // ReSharper disable once NotAccessedField.Local (keep GC reference)
            private IPlatformHandle? _child;
            private UIView? _view;
            private NativeControlHostImpl? _attachedTo;

            public NativeControlAttachment(IPlatformHandle child)
            {
                _child = child;
                
                _view = (child as UIViewControlHandle)?.View ?? new ViewHolder(child.Handle);
            }

            [MemberNotNull(nameof(_view))]
            private void CheckDisposed()
            {
                if (_view == null)
                    throw new ObjectDisposedException(nameof(NativeControlAttachment));
            }

            public void Dispose()
            {
                _view?.RemoveFromSuperview();
                _child = null;
                _attachedTo = null;
                _view?.Dispose();
                _view = null;
            }

            public INativeControlHostImpl? AttachedTo
            {
                get => _attachedTo;
                set
                {
                    CheckDisposed();
                    
                    _attachedTo = (NativeControlHostImpl?)value;
                    if (_attachedTo == null)
                    {
                        _view.RemoveFromSuperview();
                    }
                    else
                    {
                        _attachedTo._avaloniaView.AddSubview(_view);
                    }
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is NativeControlHostImpl;

            public void HideWithSize(Size size)
            {
                CheckDisposed();

                _view.Hidden = true;
                _view.Frame = new CGRect(0d, 0d, Math.Max(1d, size.Width), Math.Max(1d, size.Height));
            }

            public void ShowInBounds(Rect bounds)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    throw new InvalidOperationException("The control isn't currently attached to a toplevel");

                _view.Frame = new CGRect(bounds.X, bounds.Y, Math.Max(1d, bounds.Width), Math.Max(1d, bounds.Height));
                _view.Hidden = false;
            }
        }
    }
    
    public class UIViewControlHandle : INativeControlHostDestroyableControlHandle, IDisposable
    {
        internal const string UIViewDescriptor = "UIView";
        
        private UIView? _view;
        private bool _disposeView;
        
        public UIViewControlHandle(UIView view, bool disposeView)
        {
            _view = view;
            _disposeView = disposeView;
        }
        
        public UIView View => _view ?? throw new ObjectDisposedException(nameof(UIViewControlHandle));
        
        public string HandleDescriptor => UIViewDescriptor;

        IntPtr IPlatformHandle.Handle => _view?.Handle.Handle ?? default;

        public void Destroy()
        {
            Dispose(true);
        }
        
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        
        ~UIViewControlHandle()
        {
            Dispose(false);
        }
        
        private void Dispose(bool disposing)
        {
            if (_disposeView)
            {
                _view?.Dispose();
            }

            _view = null;
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
