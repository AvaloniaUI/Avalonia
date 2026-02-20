using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// Registers the AT-SPI cache interface at /org/a11y/atspi/cache.
    /// 
    /// GetItems returns an empty list and cache signals
    /// are not emitted. Screen readers fall back to live Accessible queries.
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
