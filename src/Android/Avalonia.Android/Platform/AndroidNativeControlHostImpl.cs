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
            return new AndroidViewControlHandle(new FrameLayout(_avaloniaView.Context!));
        }

        public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
        {
            var parent = new AndroidViewControlHandle(_avaloniaView);
            AndroidNativeControlAttachment? attachment = null;
            try
            {
                var child = create(parent);
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

        private class AndroidNativeControlAttachment : INativeControlHostControlTopLevelAttachment
        {
            private View? _view;
            private AndroidNativeControlHostImpl? _attachedTo;

            public AndroidNativeControlAttachment(IPlatformHandle child)
            {
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
                if (_attachedTo == null)
                    return;

                size *= _attachedTo._avaloniaView.TopLevelImpl.RenderScaling;
                _view.Visibility = ViewStates.Gone;
                _view.LayoutParameters = new FrameLayout.LayoutParams(Math.Max(1, (int)size.Width), Math.Max(1, (int)size.Height));
                _view.RequestLayout();
            }

            public void ShowInBounds(Rect bounds)
            {
                CheckDisposed();
                if (_attachedTo == null)
                    throw new InvalidOperationException("The control isn't currently attached to a toplevel");

                bounds *= _attachedTo._avaloniaView.TopLevelImpl.RenderScaling;
                _view.Visibility = ViewStates.Visible;
                _view.LayoutParameters = new FrameLayout.LayoutParams(Math.Max(1, (int)bounds.Width), Math.Max(1, (int)bounds.Height))
                {
                    LeftMargin = (int)bounds.X,
                    TopMargin = (int)bounds.Y
                };
                _view.RequestLayout();
            }
        }
    }
}
