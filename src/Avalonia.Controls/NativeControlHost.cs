using Avalonia.Controls.Platform;
using Avalonia.LogicalTree;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    public class NativeControlHost : Control
    {
        private TopLevel _currentRoot;
        private INativeControlHostImpl _currentHost;
        private INativeControlHostControlTopLevelAttachment _attachment;
        private IPlatformHandle _nativeControlHandle;
        private bool _queuedForDestruction;
        static NativeControlHost()
        {
            IsVisibleProperty.Changed.AddClassHandler<NativeControlHost>(OnVisibleChanged);
            TransformedBoundsProperty.Changed.AddClassHandler<NativeControlHost>(OnBoundsChanged);
        }

        private static void OnBoundsChanged(NativeControlHost host, AvaloniaPropertyChangedEventArgs arg2) 
            => host.UpdateHost();

        private static void OnVisibleChanged(NativeControlHost host, AvaloniaPropertyChangedEventArgs arg2)
            => host.UpdateHost();

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _currentRoot = e.Root as TopLevel;
            UpdateHost();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _currentRoot = null;
            UpdateHost();
        }


        void UpdateHost()
        {
            _currentHost = (_currentRoot?.PlatformImpl as ITopLevelImplWithNativeControlHost)?.NativeControlHost;
            var needsAttachment = _currentHost != null;
            var needsShow = needsAttachment && IsEffectivelyVisible && TransformedBounds.HasValue;
            
            if (needsAttachment)
            {
                // If there is an existing attachment, ensure that we are attached to the proper host or destroy the attachment
                if (_attachment != null && _attachment.AttachedTo != _currentHost)
                {
                    if (_attachment != null)
                    {
                        if (_attachment.IsCompatibleWith(_currentHost))
                        {
                            _attachment.AttachedTo = _currentHost;
                        }
                        else
                        {
                            _attachment.Dispose();
                            _attachment = null;
                        }
                    }
                }

                // If there is no attachment, but the control exists,
                // attempt to attach to to the current toplevel or destroy the control if it's incompatible
                if (_attachment == null && _nativeControlHandle != null)
                {
                    if (_currentHost.IsCompatibleWith(_nativeControlHandle))
                        _attachment = _currentHost.CreateNewAttachment(_nativeControlHandle);
                    else
                        DestroyNativeControl();
                }

                // There is no control handle an no attachment, create both
                if (_nativeControlHandle == null)
                {
                    _attachment = _currentHost.CreateNewAttachment(parent =>
                        _nativeControlHandle = CreateNativeControlCore(parent));
                }
            }
            else
            {
                // Immediately detach the control from the current toplevel if there is an existing attachment
                if (_attachment != null)
                    _attachment.AttachedTo = null;
                
                // Don't destroy the control immediately, it might be just being reparented to another TopLevel
                if (_nativeControlHandle != null && !_queuedForDestruction)
                {
                    _queuedForDestruction = true;
                    Dispatcher.UIThread.Post(CheckDestruction, DispatcherPriority.Background);
                }
            }

            if (needsShow)
                _attachment?.ShowInBounds(TransformedBounds.Value);
            else if (needsAttachment)
                _attachment?.Hide();
        }

        public bool TryUpdateNativeControlPosition()
        {
            var needsShow = _currentHost != null && IsEffectivelyVisible && TransformedBounds.HasValue;

            if(needsShow)
                _attachment?.ShowInBounds(TransformedBounds.Value);
            return needsShow;
        }

        void CheckDestruction()
        {
            _queuedForDestruction = false;
            if (_currentRoot == null)
                DestroyNativeControl();
        }
        
        protected virtual IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            return _currentHost.CreateDefaultChild(parent);
        }

        void DestroyNativeControl()
        {
            if (_nativeControlHandle != null)
            {
                _attachment?.Dispose();
                _attachment = null;
                
                DestroyNativeControlCore(_nativeControlHandle);
                _nativeControlHandle = null;
            }
        }

        protected virtual void DestroyNativeControlCore(IPlatformHandle control)
        {
            ((INativeControlHostDestroyableControlHandle)control).Destroy();
        }
        
    }
}
