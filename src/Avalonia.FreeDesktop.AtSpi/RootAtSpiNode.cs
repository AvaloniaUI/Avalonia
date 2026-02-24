using System;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Platform;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// AT-SPI node for a top-level window with coordinate translation support.
    /// </summary>
    internal sealed class RootAtSpiNode : AtSpiNode
    {
        public RootAtSpiNode(AutomationPeer peer, AtSpiServer server)
            : base(peer, server)
        {
            RootProvider = peer.GetProvider<IRootProvider>() ?? throw new InvalidOperationException(
                "Attempt to create RootAtSpiNode from peer which does not implement IRootProvider.");
            RootProvider.FocusChanged += OnRootFocusChanged;

            if (WindowImpl is not { } impl)
                return;

            impl.Activated += OnWindowActivated;
            impl.Deactivated += OnWindowDeactivated;
        }

        public IRootProvider RootProvider { get; }
        public IWindowBaseImpl? WindowImpl => RootProvider.PlatformImpl as IWindowBaseImpl;
        public ApplicationAtSpiNode? AppRoot { get; set; }

        public Rect ToScreen(Rect rect)
        {
            if (WindowImpl is null)
                return default;
            return new PixelRect(
                    WindowImpl.PointToScreen(rect.TopLeft),
                    WindowImpl.PointToScreen(rect.BottomRight))
                .ToRect(1);
        }

        public Point PointToClient(PixelPoint point)
        {
            if (WindowImpl is null)
                return default;
            return WindowImpl.PointToClient(point);
        }

        private void OnRootFocusChanged(object? sender, EventArgs e)
        {
            var focused = RootProvider.GetFocus();
            var focusedNode = Server.TryGetAttachedNode(focused);
            if (focusedNode is null)
            {
                // Focus can shift before children are queried;
                // refresh visible root children lazily.
                EnsureChildren();
                focusedNode = Server.TryGetAttachedNode(focused);
            }
            Server.EmitFocusChange(focusedNode);
        }

        private void OnWindowActivated()
        {
            Server.EmitWindowActivationChange(this, true);
        }

        private void OnWindowDeactivated()
        {
            Server.EmitWindowActivationChange(this, false);
        }

        public override void Detach()
        {
            if (_detached)
                return;

            RootProvider.FocusChanged -= OnRootFocusChanged;

            if (WindowImpl is { } impl)
            {
                impl.Activated -= OnWindowActivated;
                impl.Deactivated -= OnWindowDeactivated;
            }

            base.Detach();
        }
    }
}
