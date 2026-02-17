using System.Threading.Tasks;
using Avalonia.Automation.Provider;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using Avalonia.Platform;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal sealed class AtSpiComponentHandler(AtSpiServer server, AtSpiNode node) : IOrgA11yAtspiComponent
    {
        public uint Version => ComponentVersion;

        public ValueTask<bool> ContainsAsync(int x, int y, uint coordType)
        {
            var rect = GetScreenExtents();
            var point = TranslateToScreen(x, y, coordType);
            var contains = point.x >= rect.X && point.y >= rect.Y &&
                           point.x < rect.X + rect.Width && point.y < rect.Y + rect.Height;
            return ValueTask.FromResult(contains);
        }

        public ValueTask<AtSpiObjectReference> GetAccessibleAtPointAsync(int x, int y, uint coordType)
        {
            // Basic hit testing - return self for now
            var rect = GetScreenExtents();
            var point = TranslateToScreen(x, y, coordType);
            if (point.x >= rect.X && point.y >= rect.Y &&
                point.x < rect.X + rect.Width && point.y < rect.Y + rect.Height)
            {
                return ValueTask.FromResult(server.GetReference(node));
            }

            return ValueTask.FromResult(server.GetNullReference());
        }

        public ValueTask<AtSpiRect> GetExtentsAsync(uint coordType)
        {
            var rect = GetScreenExtents();
            var translated = TranslateRect(rect, coordType);
            return ValueTask.FromResult(new AtSpiRect(
                (int)translated.X, (int)translated.Y,
                (int)translated.Width, (int)translated.Height));
        }

        public ValueTask<(int X, int Y)> GetPositionAsync(uint coordType)
        {
            var rect = GetScreenExtents();
            var translated = TranslateRect(rect, coordType);
            return ValueTask.FromResult(((int)translated.X, (int)translated.Y));
        }

        public ValueTask<(int Width, int Height)> GetSizeAsync()
        {
            var rect = GetScreenExtents();
            return ValueTask.FromResult(((int)rect.Width, (int)rect.Height));
        }

        public ValueTask<uint> GetLayerAsync()
        {
            var controlType = node.Peer.GetAutomationControlType();
            // Window layer = 7, Widget layer = 3
            var layer = controlType == Automation.Peers.AutomationControlType.Window ? 7u : 3u;
            return ValueTask.FromResult(layer);
        }

        public ValueTask<short> GetMDIZOrderAsync() => ValueTask.FromResult((short)-1);

        public ValueTask<bool> GrabFocusAsync()
        {
            node.Peer.SetFocus();
            return ValueTask.FromResult(true);
        }

        public ValueTask<double> GetAlphaAsync() => ValueTask.FromResult(1.0);

        public ValueTask<bool> SetExtentsAsync(int x, int y, int width, int height, uint coordType)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> SetPositionAsync(int x, int y, uint coordType)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> SetSizeAsync(int width, int height)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> ScrollToAsync(uint type)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> ScrollToPointAsync(uint coordType, int x, int y)
        {
            return ValueTask.FromResult(false);
        }

        private Rect GetScreenExtents()
        {
            var bounds = node.Peer.GetBoundingRectangle();
            if (node is RootAtSpiNode rootNode)
                return rootNode.ToScreen(bounds);

            // Find root and translate to screen
            var root = node.Peer.GetVisualRoot();
            if (root is not null)
            {
                var rootNode2 = AtSpiNode.TryGet(root) as RootAtSpiNode;
                if (rootNode2 is not null)
                    return rootNode2.ToScreen(bounds);
            }

            return bounds;
        }

        private Rect TranslateRect(Rect screenRect, uint coordType)
        {
            // coordType: 0 = screen, 1 = window, 2 = parent
            if (coordType == 0)
                return screenRect;

            if (coordType == 1)
            {
                var windowRect = GetWindowRect();
                return new Rect(
                    screenRect.X - windowRect.X,
                    screenRect.Y - windowRect.Y,
                    screenRect.Width,
                    screenRect.Height);
            }

            if (coordType == 2)
            {
                var parentRect = GetParentScreenRect();
                return new Rect(
                    screenRect.X - parentRect.X,
                    screenRect.Y - parentRect.Y,
                    screenRect.Width,
                    screenRect.Height);
            }

            return screenRect;
        }

        private (int x, int y) TranslateToScreen(int x, int y, uint coordType)
        {
            if (coordType == 0)
                return (x, y);

            if (coordType == 1)
            {
                var windowRect = GetWindowRect();
                return (x + (int)windowRect.X, y + (int)windowRect.Y);
            }

            if (coordType == 2)
            {
                var parentRect = GetParentScreenRect();
                return (x + (int)parentRect.X, y + (int)parentRect.Y);
            }

            return (x, y);
        }

        private Rect GetWindowRect()
        {
            var root = node.Peer.GetVisualRoot();
            if (root is null) return default;
            
            if (AtSpiNode.TryGet(root) is RootAtSpiNode rootNode)
                return rootNode.ToScreen(root.GetBoundingRectangle());

            return default;
        }

        private Rect GetParentScreenRect()
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
