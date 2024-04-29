using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Tizen.NUI.BaseComponents;

namespace Avalonia.Tizen;

internal class NuiNativeControlHostImpl : INativeControlHostImpl
{
    private readonly NuiAvaloniaView _avaloniaView;

    public NuiNativeControlHostImpl(NuiAvaloniaView avaloniaView)
    {
        _avaloniaView = avaloniaView;
    }

    public INativeControlHostDestroyableControlHandle CreateDefaultChild(IPlatformHandle parent) =>
        new NuiViewControlHandle(new View());

    public INativeControlHostControlTopLevelAttachment CreateNewAttachment(Func<IPlatformHandle, IPlatformHandle> create)
    {
        var parent = new NuiViewControlHandle(_avaloniaView);
        NativeControlAttachment? attachment = null;
        try
        {
            var child = create(parent);
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
            throw;
        }
    }

    public INativeControlHostControlTopLevelAttachment CreateNewAttachment(IPlatformHandle handle) =>
        new NativeControlAttachment(handle);

    public bool IsCompatibleWith(IPlatformHandle handle) =>
        handle.HandleDescriptor == NuiViewControlHandle.ViewDescriptor;

    private class NativeControlAttachment : INativeControlHostControlTopLevelAttachment
    {
        private IPlatformHandle? _child;
        private View? _view;
        private NuiNativeControlHostImpl? _attachedTo;

        public NativeControlAttachment(IPlatformHandle child)
        {
            _child = child;
            _view = (child as NuiViewControlHandle)?.View;
        }

        [MemberNotNull(nameof(_view))]
        private void CheckDisposed()
        {
            if (_view == null)
                throw new ObjectDisposedException(nameof(NativeControlAttachment));
        }

        public void Dispose()
        {
            _view?.Unparent();
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

                _attachedTo = (NuiNativeControlHostImpl?)value;
                if (_attachedTo == null)
                {
                    _view.Unparent();
                }
                else
                {
                    _attachedTo._avaloniaView.Add(_view);
                }
            }
        }

        public bool IsCompatibleWith(INativeControlHostImpl host) => host is NuiNativeControlHostImpl;

        public void HideWithSize(Size size)
        {
            CheckDisposed();
            if (_attachedTo == null)
                return;

            _view.Hide();
            _view.Size = new global::Tizen.NUI.Size(MathF.Max(1f, (float)size.Width), Math.Max(1f, (float)size.Height));
        }

        public void ShowInBounds(Rect bounds)
        {
            CheckDisposed();
            if (_attachedTo == null)
                throw new InvalidOperationException("The control isn't currently attached to a toplevel");

            _view.Size = new global::Tizen.NUI.Size(MathF.Max(1f, (float)bounds.Width), Math.Max(1f, (float)bounds.Height));
            _view.Position = new global::Tizen.NUI.Position((float)bounds.X, (float)bounds.Y);
            _view.Show();
        }
    }
}
