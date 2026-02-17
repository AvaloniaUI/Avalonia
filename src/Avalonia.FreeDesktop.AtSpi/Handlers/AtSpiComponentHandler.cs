using System.Threading.Tasks;
using Avalonia.Automation.Provider;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using Avalonia.Platform;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Implements the AT-SPI Component interface (geometry and focus).
    /// </summary>
    internal sealed class AtSpiComponentHandler(AtSpiServer server, AtSpiNode node) : IOrgA11yAtspiComponent
    {
        public uint Version => ComponentVersion;

        public ValueTask<bool> ContainsAsync(int x, int y, uint coordType)
        {
            var rect = AtSpiCoordinateHelper.GetScreenExtents(node);
            var point = TranslateToScreen(x, y, coordType);
            var contains = point.x >= rect.X && point.y >= rect.Y &&
                           point.x < rect.X + rect.Width && point.y < rect.Y + rect.Height;
            return ValueTask.FromResult(contains);
        }

        public ValueTask<AtSpiObjectReference> GetAccessibleAtPointAsync(int x, int y, uint coordType)
        {
            var rect = AtSpiCoordinateHelper.GetScreenExtents(node);
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
            var rect = AtSpiCoordinateHelper.GetScreenExtents(node);
            var translated = AtSpiCoordinateHelper.TranslateRect(node, rect, coordType);
            return ValueTask.FromResult(new AtSpiRect(
                (int)translated.X, (int)translated.Y,
                (int)translated.Width, (int)translated.Height));
        }

        public ValueTask<(int X, int Y)> GetPositionAsync(uint coordType)
        {
            var rect = AtSpiCoordinateHelper.GetScreenExtents(node);
            var translated = AtSpiCoordinateHelper.TranslateRect(node, rect, coordType);
            return ValueTask.FromResult(((int)translated.X, (int)translated.Y));
        }

        public ValueTask<(int Width, int Height)> GetSizeAsync()
        {
            var rect = AtSpiCoordinateHelper.GetScreenExtents(node);
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
            // Only support moving (not resizing) for now
            return SetPositionAsync(x, y, coordType);
        }

        public ValueTask<bool> SetPositionAsync(int x, int y, uint coordType)
        {
            if (node.Peer.GetProvider<IRootProvider>() is not { PlatformImpl: IWindowImpl windowImpl })
                return ValueTask.FromResult(false);

            var screenPos = TranslateToScreen(x, y, coordType);
            windowImpl.Move(new PixelPoint(screenPos.x, screenPos.y));
            return ValueTask.FromResult(true);
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

        private (int x, int y) TranslateToScreen(int x, int y, uint coordType)
        {
            var ct = (AtSpiCoordType)coordType;

            switch (ct)
            {
                case AtSpiCoordType.Screen:
                    return (x, y);
                case AtSpiCoordType.Window:
                {
                    var windowRect = AtSpiCoordinateHelper.GetWindowRect(node);
                    return (x + (int)windowRect.X, y + (int)windowRect.Y);
                }
                case AtSpiCoordType.Parent:
                {
                    var parentRect = AtSpiCoordinateHelper.GetParentScreenRect(node);
                    return (x + (int)parentRect.X, y + (int)parentRect.Y);
                }
                default:
                    return (x, y);
            }
        }
    }
}
