using System.Threading.Tasks;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal sealed class AtSpiImageHandler : IOrgA11yAtspiImage
    {
        private readonly AtSpiNode _node;

        public AtSpiImageHandler(AtSpiServer server, AtSpiNode node)
        {
            _ = server;
            _node = node;
        }

        public uint Version => ImageVersion;

        public string ImageDescription => _node.Peer.GetHelpText() ?? _node.Peer.GetName() ?? string.Empty;

        public string ImageLocale => ResolveLocale();

        public ValueTask<AtSpiRect> GetImageExtentsAsync(uint coordType)
        {
            var rect = AtSpiCoordinateHelper.GetScreenExtents(_node);
            var translated = AtSpiCoordinateHelper.TranslateRect(_node, rect, coordType);
            return ValueTask.FromResult(new AtSpiRect(
                (int)translated.X, (int)translated.Y,
                (int)translated.Width, (int)translated.Height));
        }

        public ValueTask<(int X, int Y)> GetImagePositionAsync(uint coordType)
        {
            var rect = AtSpiCoordinateHelper.GetScreenExtents(_node);
            var translated = AtSpiCoordinateHelper.TranslateRect(_node, rect, coordType);
            return ValueTask.FromResult(((int)translated.X, (int)translated.Y));
        }

        public ValueTask<(int Width, int Height)> GetImageSizeAsync()
        {
            var rect = AtSpiCoordinateHelper.GetScreenExtents(_node);
            return ValueTask.FromResult(((int)rect.Width, (int)rect.Height));
        }
    }
}
