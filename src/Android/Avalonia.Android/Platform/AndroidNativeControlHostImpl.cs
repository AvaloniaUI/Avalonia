#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using Android.Views;
using Android.Widget;

using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.Android.Platform
{
    internal class AndroidNativeControlHostImpl : INativeControlHostImpl
    {
        private readonly AvaloniaView _avaloniaView;

        public AndroidNativeControlHostImpl(AvaloniaView avaloniaView)
        {
            _avaloniaView = avaloniaView;
        }

        public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent)
        {
            return new AndroidViewControlHandle(new FrameLayout(_avaloniaView.Context!), false);
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            var holder = new AndroidViewControlHandle(_avaloniaView, false);
            AndroidNativeControlAttachment? attachment = null;
            try
            {
                var child = create(holder);
                // It has to be assigned to the variable before property setter is called so we dispose it on exception
#pragma warning disable IDE0017 // Simplify object initialization
                attachment = new AndroidNativeControlAttachment(child);
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
            return new AndroidNativeControlAttachment(handle)
            {
                AttachedTo = this
            };
        }

        public bool IsCompatibleWith(IPlatformHandle handle) => handle.HandleDescriptor == AndroidViewControlHandle.AndroidDescriptor;

        class AndroidNativeControlAttachment : INativeControlHostControlTopLevelAttachment
        {
            // ReSharper disable once NotAccessedField.Local (keep GC reference)
            private IPlatformHandle? _child;
            private View? _view;
            private AndroidNativeControlHostImpl? _attachedTo;

            public AndroidNativeControlAttachment(IPlatformHandle child)
            {
                _child = child;

                _view = (child as AndroidViewControlHandle)?.View
                    ?? Java.Lang.Object.GetObject<View>(child.Handle, global::Android.Runtime.JniHandleOwnership.DoNotTransfer);
            }

            [MemberNotNull(nameof(_view))]
            private void CheckDisposed()
            {
                if (_view == null)
                    throw new ObjectDisposedException(nameof(AndroidNativeControlAttachment));
            }

            public void Dispose()
            {
                if (_view != null && _attachedTo?._avaloniaView is ViewGroup parent)
                {
                    parent.RemoveView(_view);
                }
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

                    var oldAttachedTo = _attachedTo;
                    _attachedTo = (AndroidNativeControlHostImpl?)value;
                    if (_attachedTo == null)
                    {
                        oldAttachedTo?._avaloniaView.RemoveView(_view);
                    }
                    else
                    {
                        _attachedTo._avaloniaView.AddView(_view);
                    }
                }
            }

            public bool IsCompatibleWith(INativeControlHostImpl host) => host is AndroidNativeControlHostImpl;

            public void HideWithSize(Size size)
            {
                CheckDisposed();
                _view.Visibility = ViewStates.Gone;
                _view.LayoutParameters = new ViewGroup.LayoutParams(Math.Max(1, (int)size.Width), Math.Max(1, (int)size.Height));
                _view.RequestLayout();
            }

            public void ShowInBounds(Rect bounds)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    throw new InvalidOperationException("The control isn't currently attached to a toplevel");

                bounds *= _attachedTo._avaloniaView.TopLevelImpl.RenderScaling;
                _view.Visibility = ViewStates.Visible;
                _view.LayoutParameters = new ViewGroup.MarginLayoutParams(Math.Max(1, (int)bounds.Width), Math.Max(1, (int)bounds.Height))
                {
                    LeftMargin = (int)bounds.X,
                    TopMargin = (int)bounds.Y
                };
                _view.RequestLayout();
            }
        }
    }

    public class AndroidViewControlHandle : INativeControlHostDestroyableControlHandle, IDisposable
    {
        internal const string AndroidDescriptor = "JavaHandle";
        
        private View? _view;
        private bool _disposeView;
        
        public AndroidViewControlHandle(View view, bool disposeView)
        {
            _view = view;
            _disposeView = disposeView;
        }
        
        public View View => _view ?? throw new ObjectDisposedException(nameof(AndroidViewControlHandle));
        
        public string HandleDescriptor => AndroidDescriptor;

        IntPtr IPlatformHandle.Handle => _view?.Handle ?? default;

        public void Destroy()
        {
            Dispose(true);
        }
        
        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        
        ~AndroidViewControlHandle()
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
