using System;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Platform;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal sealed class RootAtSpiNode : AtSpiNode
    {
        public RootAtSpiNode(AutomationPeer peer, AtSpiServer server)
            : base(peer, server, RootPath)
        {
            RootProvider = peer.GetProvider<IRootProvider>() ?? throw new InvalidOperationException(
                "Attempt to create RootAtSpiNode from peer which does not implement IRootProvider.");
            RootProvider.FocusChanged += OnRootFocusChanged;
        }

        public IRootProvider RootProvider { get; }
        public IWindowBaseImpl? WindowImpl => RootProvider.PlatformImpl as IWindowBaseImpl;

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
            var focused = InvokeSync(() => RootProvider.GetFocus());
            var focusedNode = focused is not null ? GetOrCreate(focused, Server) : null;
            Server.EmitFocusChange(focusedNode);
        }
    }
}
