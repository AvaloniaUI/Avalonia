using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Platform;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class NativeControlHost : Control
    {
        private TopLevel? _currentRoot;
        private INativeControlHostImpl? _currentHost;
        private INativeControlHostControlTopLevelAttachment? _attachment;
        private IPlatformHandle? _nativeControlHandle;
        private bool _queuedForDestruction;
        private bool _queuedForMoveResize;
        private readonly List<Visual> _propertyChangedSubscriptions = new();

        static NativeControlHost()
        {
            FlowDirectionProperty.Changed.AddClassHandler<NativeControlHost>(OnFlowDirectionChanged);
        }

        internal IPlatformHandle? NativeControlHandle
        {
            get => _nativeControlHandle;
            set
            {
                if (_nativeControlHandle != value)
                {
                    _nativeControlHandle = value;
                    NativeControlHandleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        internal event EventHandler? NativeControlHandleChanged;

        /// <inheritdoc />
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _currentRoot = e.Root as TopLevel;
            var visual = (Visual)this;
            while (visual != null)
            {
                visual.PropertyChanged += PropertyChangedHandler;
                _propertyChangedSubscriptions.Add(visual);

                visual = visual.GetVisualParent();
            }

            UpdateHost();
        }
        
        private static void OnFlowDirectionChanged(NativeControlHost nativeControlHost,
            AvaloniaPropertyChangedEventArgs propertyChangedEventArgs)
        {
            nativeControlHost.TryUpdateNativeControlPosition();
        }

        private void PropertyChangedHandler(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.IsEffectiveValueChange && (e.Property == BoundsProperty || e.Property == IsVisibleProperty))
                EnqueueForMoveResize();
        }

        /// <inheritdoc />
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _currentRoot = null;
            foreach (var v in _propertyChangedSubscriptions)
                v.PropertyChanged -= PropertyChangedHandler;
            _propertyChangedSubscriptions.Clear();
            UpdateHost();
        }


        private void UpdateHost()
        {
            _queuedForMoveResize = false;
            _currentHost = _currentRoot?.PlatformImpl?.TryGetFeature<INativeControlHostImpl>();
            
            if (_currentHost != null)
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
                // attempt to attach to the current toplevel or destroy the control if it's incompatible
                if (_attachment == null && NativeControlHandle != null)
                {
                    if (_currentHost.IsCompatibleWith(NativeControlHandle))
                        _attachment = _currentHost.CreateNewAttachment(NativeControlHandle);
                    else
                        DestroyNativeControl();
                }

                // There is no control handle an no attachment, create both
                if (NativeControlHandle == null)
                {
                    _attachment = _currentHost.CreateNewAttachment(parent =>
                        NativeControlHandle = CreateNativeControlCore(parent));
                }
            }
            else
            {
                // Immediately detach the control from the current toplevel if there is an existing attachment
                if (_attachment != null)
                    _attachment.AttachedTo = null;
                
                // Don't destroy the control immediately, it might be just being reparented to another TopLevel
                if (NativeControlHandle != null && !_queuedForDestruction)
                {
                    _queuedForDestruction = true;
                    Dispatcher.UIThread.Post(CheckDestruction, DispatcherPriority.Background);
                }
            }

            if (_attachment?.AttachedTo != _currentHost)
                return;

            TryUpdateNativeControlPosition();
        }

        
        private Rect? GetAbsoluteBounds()
        {
            Debug.Assert(_currentRoot is not null);

            var bounds = Bounds;
            // Native window is not rendered by Avalonia
            var transformToVisual = this.TransformToVisual(_currentRoot);
            if (transformToVisual == null)
                return null;
            var position = new Rect(default, bounds.Size).TransformToAABB(transformToVisual.Value).Position;
            return new Rect(position, bounds.Size);
        }

        private void EnqueueForMoveResize()
        {
            if(_queuedForMoveResize)
                return;
            _queuedForMoveResize = true;
            Dispatcher.UIThread.Post(UpdateHost, DispatcherPriority.AfterRender);
        }

        public bool TryUpdateNativeControlPosition()
        {
            if (_currentHost == null)
                return false;
            
            var bounds = GetAbsoluteBounds();

            if (IsEffectivelyVisible && bounds.HasValue)
            {
                if (bounds.Value.Width == 0 && bounds.Value.Height == 0)
                    return false;
                _attachment?.ShowInBounds(bounds.Value);
            }
            else
                _attachment?.HideWithSize(Bounds.Size);
            return true;
        }

        private void CheckDestruction()
        {
            _queuedForDestruction = false;
            if (_currentRoot == null)
                DestroyNativeControl();
        }
        
        protected virtual IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (_currentHost == null)
                throw new InvalidOperationException();
            return _currentHost.CreateDefaultChild(parent);
        }

        private void DestroyNativeControl()
        {
            if (NativeControlHandle != null)
            {
                _attachment?.Dispose();
                _attachment = null;
                
                DestroyNativeControlCore(NativeControlHandle);
                NativeControlHandle = null;
            }
        }

        protected virtual void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (control is INativeControlHostDestroyableControlHandle nativeControlHostDestroyableControlHandle)
            {
                nativeControlHostDestroyableControlHandle.Destroy();
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new NativeControlHostPeer(this);
    }
}
