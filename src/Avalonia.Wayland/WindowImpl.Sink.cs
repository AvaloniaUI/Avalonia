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
        private new WindowImpl _p => (WindowImpl)base._p;
        private XdgConfigureBatch? _initialBatch;
        private WaylandSurfaceCreateResult<WXdgTopLevelProxy>? _handle;
        private WXdgTopLevelProxy? _surfaceProxy;

        public Sink(WindowImpl p, bool secondShow) : base(p)
        {
            _handle = _p._client.CreateTopLevelHandle(new WXdgTopLevelEventSinkProxy(this, WaylandMarshallers.UIThread));
            _surfaceProxy = _handle.Proxy;
            _p._handle = _handle;
            _p._surfaceProxy = _surfaceProxy;

            var initialBatch = _handle.BasicInitCompleted.GetAwaiter().GetResult();
            _initialBatch = initialBatch;

            _p.ApplyConfigureBatch(initialBatch);

            // If compositor didn't provide a size, auto-size
            if (!secondShow && initialBatch.Size is not { Width: > 0, Height: > 0 })
                _p.ClientSize = new Size(Math.Min(640, _p.MaxAutoSizeHint.Width),
                    Math.Min(480, _p.MaxAutoSizeHint.Height));

            // Ack the initial configure
            _surfaceProxy.SetPendingAckSerial(initialBatch.Serial);

            if (initialBatch.InitialDecorationMode is { } im)
                _p.ApplyDecorationMode(im);

            // Re-send stored shadow extents to the new surface
            if (_p._shadowExtents != default)
                _surfaceProxy.SetShadowExtents(_p._shadowExtents);

            // Re-send cached title to the new surface
            if (_p._title != null)
                _surfaceProxy.SetTitle(_p._title);

            // Re-apply cached min/max size constraints after a fresh worker
            // surface is created. null on both sides means SetMinMaxSize was
            // never called (or both bounds are unconstrained) — nothing to push.
            if (_p._minSize.HasValue || _p._maxSize.HasValue)
                _surfaceProxy.SetMinMaxSize(_p._minSize, _p._maxSize);

            // Re-apply cursor (defaults to Arrow on a fresh worker WSurface).
            if (_p._currentCursor != StandardCursorType.Arrow)
                _surfaceProxy.SetCursor(_p._currentCursor);

            // Re-register text-input sink on the freshly created worker
            // surface and re-apply current state, if a client is attached.
            _p._textInputMethod?.OnSurfaceCreated();

            // Refresh the storage provider's factory chain — the previous wayland
            // connection (and its xdg-foreign exporter, if any) is gone, and the new
            // compositor may expose different globals. Reset() drops any cached
            // provider so portal availability is re-evaluated against the fresh
            // connection on the next picker call.
            _p._storageProvider?.Reset(_p.BuildStorageFactories());
        }

        protected override void DisconnectFromSurface()
        {
            var proxy = _p._surfaceProxy;
            _p._handle = null;
            _p._surfaceProxy = null;
            proxy?.Disconnect();
        }

        public void OnConfigure(XdgConfigureBatch batch)
        {
            // Marshalled to UI thread by WSurfaceEventSinkProxy.
            if (_disposed)
                return;

            // Skip the initial batch — already processed synchronously in the constructor
            if (ReferenceEquals(batch, _initialBatch))
            {
                _initialBatch = null;
                return;
            }

            _p.ApplyConfigureBatch(batch);

            // Post the ack serial back to the wayland thread for next commit
            _surfaceProxy?.SetPendingAckSerial(batch.Serial);
        }

        public void OnDecorationModeChanged(DecorationMode mode)
        {
            if (_disposed || _p._csdSticky)
                return;
            _p.ApplyDecorationMode(mode);
        }

        public void OnClose()
        {
            // Marshalled to UI thread by WSurfaceEventSinkProxy.
            if (_disposed)
                return;

            if (_p.Closing?.Invoke(WindowCloseReason.WindowClosing) != true)
                Dispose();
        }

        protected override void OnInputWhileDisabled() => _p.GotInputWhenDisabled?.Invoke();

        // Title-bar move and edge-resize chrome dispatch is xdg_toplevel-specific.
        protected override bool HandleSurfaceSpecificDispatch(RawInputEventArgs args)
        {
            // TODO: We need to properly check if touch contact is primary, but it's tracked in TouchDevice,
            // so we might want to move that handling to x-plat part of the codebase
            // another point is that we generally need to cancel implicit capture if pointer gesture started
            // with right mouse button
            if (_inputRoot == null || !(args is RawPointerEventArgs
                {
                    Type: RawPointerEventType.LeftButtonDown or RawPointerEventType.TouchBegin
                } mouse))
                return false;

            var chromeRole = _inputRoot.HitTestChromeElement(mouse.Position);
            if (chromeRole == WindowDecorationsElementRole.TitleBar)
            {
                _surfaceProxy?.Move(mouse.PlatformInputEventCookie, WaylandDispatchPriority.Oob);
                return true;
            }

            if (_p._canResize && chromeRole is { } role)
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
