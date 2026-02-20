using Avalonia.Automation.Peers;

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

            if (AtSpiNode.TryGet(root) is RootAtSpiNode rootNode2)
                return rootNode2.ToScreen(bounds);

            return bounds;
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

            if (AtSpiNode.TryGet(root) is RootAtSpiNode rootNode)
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
            var rootNode = AtSpiNode.TryGet(root) as RootAtSpiNode;
            return rootNode?.ToScreen(bounds) ?? bounds;
        }
    }
}
