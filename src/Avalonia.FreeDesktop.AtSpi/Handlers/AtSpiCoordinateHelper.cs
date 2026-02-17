using Avalonia.Automation.Peers;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal static class AtSpiCoordinateHelper
    {
        public static Rect GetScreenExtents(AtSpiNode node)
        {
            var bounds = node.Peer.GetBoundingRectangle();
            if (node is RootAtSpiNode rootNode)
                return rootNode.ToScreen(bounds);

            var root = node.Peer.GetVisualRoot();
            if (root is not null)
            {
                var rootNode2 = AtSpiNode.TryGet(root) as RootAtSpiNode;
                if (rootNode2 is not null)
                    return rootNode2.ToScreen(bounds);
            }

            return bounds;
        }

        public static Rect TranslateRect(AtSpiNode node, Rect screenRect, uint coordType)
        {
            // coordType: 0 = screen, 1 = window, 2 = parent
            if (coordType == 0)
                return screenRect;

            if (coordType == 1)
            {
                var windowRect = GetWindowRect(node);
                return new Rect(
                    screenRect.X - windowRect.X,
                    screenRect.Y - windowRect.Y,
                    screenRect.Width,
                    screenRect.Height);
            }

            if (coordType == 2)
            {
                var parentRect = GetParentScreenRect(node);
                return new Rect(
                    screenRect.X - parentRect.X,
                    screenRect.Y - parentRect.Y,
                    screenRect.Width,
                    screenRect.Height);
            }

            return screenRect;
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
            if (parent is not null)
            {
                var bounds = parent.GetBoundingRectangle();
                var root = parent.GetVisualRoot();
                if (root is not null)
                {
                    var rootNode = AtSpiNode.TryGet(root) as RootAtSpiNode;
                    if (rootNode is not null)
                        return rootNode.ToScreen(bounds);
                }

                return bounds;
            }

            return default;
        }
    }
}
