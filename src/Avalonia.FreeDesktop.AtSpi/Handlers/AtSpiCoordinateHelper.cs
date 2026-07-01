namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Coordinate translation utilities between screen, window, and parent frames.
    /// </summary>
    internal static class AtSpiCoordinateHelper
    {
        public static Rect GetScreenExtents(AtSpiNode node)
        {
            var bounds = node.Peer.GetBoundingRectangle();
            if (node is RootAtSpiNode rootNode)
                return rootNode.ToScreen(bounds);

            var root = node.Peer.GetVisualRoot();

            if (root is null) 
                return bounds;

            if (node.Server.TryGetAttachedNode(root) is RootAtSpiNode rootNode2)
                return rootNode2.ToScreen(bounds);

            return bounds;
        }

        /// <summary>Converts a rect in top-level (visual-root) coordinates to screen coordinates.</summary>
        public static Rect ToScreenRect(AtSpiNode node, Rect topLevelRect)
        {
            var root = node.Peer.GetVisualRoot();
            if (root is null)
                return topLevelRect;

            return node.Server.TryGetAttachedNode(root) is RootAtSpiNode rootNode
                ? rootNode.ToScreen(topLevelRect)
                : topLevelRect;
        }

        /// <summary>
        /// Converts a point in the requested AT-SPI coordinate space to top-level (visual-root)
        /// coordinates - the frame GetPositionFromPoint expects. Inverse of ToScreenRect + TranslateRect.
        /// </summary>
        public static Point PointToTopLevel(AtSpiNode node, int x, int y, uint coordType)
        {
            // 1. Bring the point into screen pixels.
            switch ((AtSpiCoordType)coordType)
            {
                case AtSpiCoordType.Window:
                {
                    var windowRect = GetWindowRect(node);
                    x += (int)windowRect.X;
                    y += (int)windowRect.Y;
                    break;
                }
                case AtSpiCoordType.Parent:
                {
                    var parentRect = GetParentScreenRect(node);
                    x += (int)parentRect.X;
                    y += (int)parentRect.Y;
                    break;
                }
                // Screen: already in screen pixels.
            }

            // 2. Screen -> top-level via the root window's PointToClient (DPI-aware, not a bare subtract).
            var root = node.Peer.GetVisualRoot();
            if (root is not null && node.Server.TryGetAttachedNode(root) is RootAtSpiNode rootNode)
                return rootNode.PointToClient(new PixelPoint(x, y));

            return new Point(x, y);
        }

        public static Rect TranslateRect(AtSpiNode node, Rect screenRect, uint coordType)
        {
            var ct = (AtSpiCoordType)coordType;

            switch (ct)
            {
                case AtSpiCoordType.Screen:
                    return screenRect;
                case AtSpiCoordType.Window:
                {
                    var windowRect = GetWindowRect(node);
                    return new Rect(
                        screenRect.X - windowRect.X,
                        screenRect.Y - windowRect.Y,
                        screenRect.Width,
                        screenRect.Height);
                }
                case AtSpiCoordType.Parent:
                {
                    var parentRect = GetParentScreenRect(node);
                    return new Rect(
                        screenRect.X - parentRect.X,
                        screenRect.Y - parentRect.Y,
                        screenRect.Width,
                        screenRect.Height);
                }
                default:
                    return screenRect;
            }
        }

        public static Rect GetWindowRect(AtSpiNode node)
        {
            var root = node.Peer.GetVisualRoot();
            if (root is null) return default;

            if (node.Server.TryGetAttachedNode(root) is RootAtSpiNode rootNode)
                return rootNode.ToScreen(root.GetBoundingRectangle());

            return default;
        }

        public static Rect GetParentScreenRect(AtSpiNode node)
        {
            var parent = node.Peer.GetParent();
            if (parent is null)
                return default;
            var bounds = parent.GetBoundingRectangle();
            var root = parent.GetVisualRoot();
            if (root is null)
                return bounds;
            var rootNode = node.Server.TryGetAttachedNode(root) as RootAtSpiNode;
            return rootNode?.ToScreen(bounds) ?? bounds;
        }
    }
}
