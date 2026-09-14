using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;
using NWayland.Protocols.XdgShell;

namespace Avalonia.Wayland;

partial class WindowImpl
{
    private new sealed class Sink : WindowBaseImpl.Sink, IWXdgTopLevelEventSink
    {
        private new WindowImpl Parent => (WindowImpl)base.Parent;
        private XdgConfigureBatch? _initialBatch;
        private WaylandSurfaceCreateResult<WXdgTopLevelProxy>? _handle;
        private WXdgTopLevelProxy? _surfaceProxy;

        public Sink(WindowImpl parent, bool secondShow) : base(parent)
        {
            _handle = Parent.Client.CreateTopLevelHandle(new WXdgTopLevelEventSinkProxy(this, WaylandMarshallers.UIThread));
            _surfaceProxy = _handle.Proxy;
            Parent._handle = _handle;
            Parent._surfaceProxy = _surfaceProxy;

            var initialBatch = _handle.BasicInitCompleted.GetAwaiter().GetResult();
            _initialBatch = initialBatch;

            Parent.ApplyConfigureBatch(initialBatch);

            // If compositor didn't provide a size, auto-size
            if (!secondShow && initialBatch.Size is not { Width: > 0, Height: > 0 })
                Parent.ClientSize = new Size(Math.Min(640, Parent.MaxAutoSizeHint.Width),
                    Math.Min(480, Parent.MaxAutoSizeHint.Height));

            // Ack the initial configure
            _surfaceProxy.SetPendingAckSerial(initialBatch.Serial);

            if (initialBatch.InitialDecorationMode is { } im)
                Parent.ApplyDecorationMode(im);

            // Re-send stored shadow extents to the new surface
            if (Parent._shadowExtents != default)
                _surfaceProxy.SetShadowExtents(Parent._shadowExtents);

            // Re-send cached title to the new surface
            if (Parent._title != null)
                _surfaceProxy.SetTitle(Parent._title);

            // Re-apply cached min/max size constraints after a fresh worker
            // surface is created. null on both sides means SetMinMaxSize was
            // never called (or both bounds are unconstrained) — nothing to push.
            if (Parent._minSize.HasValue || Parent._maxSize.HasValue)
                _surfaceProxy.SetMinMaxSize(Parent._minSize, Parent._maxSize);

            // Re-apply cursor (defaults to Arrow on a fresh worker WSurface).
            if (Parent.CurrentCursor is not null)
                Parent.ApplyCurrentCursor(_surfaceProxy);

            // Re-register text-input sink on the freshly created worker
            // surface and re-apply current state, if a client is attached.
            Parent._textInputMethod?.OnSurfaceCreated();

            // Refresh the storage provider's factory chain — the previous wayland
            // connection (and its xdg-foreign exporter, if any) is gone, and the new
            // compositor may expose different globals. Reset() drops any cached
            // provider so portal availability is re-evaluated against the fresh
            // connection on the next picker call.
            Parent._storageProvider?.Reset(Parent.BuildStorageFactories());
        }

        protected override void DisconnectFromSurface()
        {
            var proxy = Parent._surfaceProxy;
            Parent._handle = null;
            Parent._surfaceProxy = null;
            proxy?.Disconnect();
        }

        public void OnConfigure(XdgConfigureBatch batch)
        {
            // Marshalled to UI thread by WSurfaceEventSinkProxy.
            if (IsDisposed)
                return;

            // Skip the initial batch — already processed synchronously in the constructor
            if (ReferenceEquals(batch, _initialBatch))
            {
                _initialBatch = null;
                return;
            }

            Parent.ApplyConfigureBatch(batch);

            // Post the ack serial back to the wayland thread for next commit
            _surfaceProxy?.SetPendingAckSerial(batch.Serial);
        }

        public void OnDecorationModeChanged(DecorationMode mode)
        {
            if (IsDisposed || Parent._csdSticky)
                return;
            Parent.ApplyDecorationMode(mode);
        }

        public void OnClose()
        {
            // Marshalled to UI thread by WSurfaceEventSinkProxy.
            if (IsDisposed)
                return;

            if (Parent.Closing?.Invoke(WindowCloseReason.WindowClosing) != true)
                Dispose();
        }

        protected override void OnInputWhileDisabled() => Parent.GotInputWhenDisabled?.Invoke();

        // Title-bar move and edge-resize chrome dispatch is xdg_toplevel-specific.
        protected override bool HandleSurfaceSpecificDispatch(RawInputEventArgs args)
        {
            // TODO: We need to properly check if touch contact is primary, but it's tracked in TouchDevice,
            // so we might want to move that handling to x-plat part of the codebase
            // another point is that we generally need to cancel implicit capture if pointer gesture started
            // with right mouse button
            if (InputRoot == null || !(args is RawPointerEventArgs
                {
                    Type: RawPointerEventType.LeftButtonDown or RawPointerEventType.TouchBegin
                } mouse))
                return false;

            var chromeRole = InputRoot.HitTestChromeElement(mouse.Position);
            if (chromeRole == WindowDecorationsElementRole.TitleBar)
            {
                _surfaceProxy?.Move(mouse.PlatformInputEventCookie, WaylandDispatchPriority.Oob);
                return true;
            }

            if (Parent._canResize && chromeRole is { } role)
            {
                var resizeEdge = role switch
                {
                    WindowDecorationsElementRole.ResizeN => XdgToplevel.ResizeEdgeEnum.Top,
                    WindowDecorationsElementRole.ResizeS => XdgToplevel.ResizeEdgeEnum.Bottom,
                    WindowDecorationsElementRole.ResizeE => XdgToplevel.ResizeEdgeEnum.Right,
                    WindowDecorationsElementRole.ResizeW => XdgToplevel.ResizeEdgeEnum.Left,
                    WindowDecorationsElementRole.ResizeNE => XdgToplevel.ResizeEdgeEnum.TopRight,
                    WindowDecorationsElementRole.ResizeNW => XdgToplevel.ResizeEdgeEnum.TopLeft,
                    WindowDecorationsElementRole.ResizeSE => XdgToplevel.ResizeEdgeEnum.BottomRight,
                    WindowDecorationsElementRole.ResizeSW => XdgToplevel.ResizeEdgeEnum.BottomLeft,
                    _ => (XdgToplevel.ResizeEdgeEnum?)null
                };

                if (resizeEdge is { } edge)
                {
                    _surfaceProxy?.Resize(mouse.PlatformInputEventCookie, edge, WaylandDispatchPriority.Oob);
                    return true;
                }
            }

            return false;
        }
    }
}
