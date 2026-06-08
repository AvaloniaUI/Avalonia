using System;
using Avalonia.Controls;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;

namespace Avalonia.Wayland;

partial class PopupImpl
{
    private new sealed class Sink : WindowBaseImpl.Sink, IWXdgPopupEventSink
    {
        private new PopupImpl _p => (PopupImpl)base._p;
        private WaylandSurfaceCreateResult<WXdgPopupProxy>? _handle;
        private WXdgPopupProxy? _surfaceProxy;

        public Sink(PopupImpl p) : base(p)
        {
            // Worker→UI sink calls (OnPointerEnter, OnPopupConfigure, etc.)
            // are invoked from the worker thread; wrap ourselves in a UI-
            // thread proxy so they marshal onto the dispatcher.
            var sinkProxy = new WXdgPopupEventSinkProxy(this, WaylandMarshallers.UIThread);

            // The xdg_popup parent is the direct parent surface (toplevel
            // or another popup). The compositor uses parent-relative
            // coordinates and is free to reposition the popup; chaining
            // through the real visual hierarchy lets it constrain each
            // link's anchor rect against its direct parent.
            var parentProxy = _p._parent.SurfaceProxy
                ?? throw new InvalidOperationException(
                    "Cannot create popup: parent surface has not been mapped yet.");

            _handle = _p._workerClient.CreatePopupHandle(sinkProxy, parentProxy);
            _surfaceProxy = _handle.Proxy;
            _p._handle = _handle;
            _p._surfaceProxy = _surfaceProxy;

            // Replay the cached positioner (if Show was called after
            // UpdatePositioner — common Avalonia ordering) so the worker
            // can attach the popup as soon as the parent is mapped.
            if (_p._lastPositioner is { } pos)
                _surfaceProxy.UpdatePositioner(pos);

            // Re-apply cursor (defaults to Arrow on a fresh worker WSurface).
            if (_p._currentCursor != Avalonia.Input.StandardCursorType.Arrow)
                _surfaceProxy.SetCursor(_p._currentCursor);
        }

        protected override void DisconnectFromSurface()
        {
            var proxy = _p._surfaceProxy;
            _p._handle = null;
            _p._surfaceProxy = null;
            proxy?.Disconnect();
        }

        public void OnPopupConfigure(XdgPopupConfigureBatch batch)
        {
            // Marshalled to UI thread by WXdgPopupEventSinkProxy.
            if (_disposed)
                return;

            // The compositor's suggested size is intentionally IGNORED
            // for now: per xdg_surface.configure, this is a *suggested*
            // surface change, and the framework's positioner-supplied
            // size is the authoritative one we use for layout. The
            // worker→UI plumbing is kept because we may want to revisit
            // this for compositor-driven shrink-to-fit behaviour.
            _ = batch.Width;
            _ = batch.Height;

            // Ack the configure on next commit.
            _surfaceProxy?.SetPendingAckSerial(batch.Serial);
        }

        public void OnPopupDone()
        {
            // Compositor dismissed the popup (click outside, parent
            // unmapped, etc.). xdg-shell forbids any further use of the
            // xdg_popup object after popup_done; treat this as a close
            // and let Dispose tear it down.
            if (_disposed)
                return;
            _p.Dispose();
        }
    }
}
