using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// Registers a dummy AT-SPI cache interface at /org/a11y/atspi/cache.
    /// </summary>
    internal sealed class AtSpiCacheHandler : IOrgA11yAtspiCache
    {
        public uint Version => CacheVersion;

        public ValueTask<List<AtSpiAccessibleCacheItem>> GetItemsAsync()
        {
            return ValueTask.FromResult(new List<AtSpiAccessibleCacheItem>());
        }
    }
}
