using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal sealed class AtSpiCacheHandler(AtSpiServer server) : IOrgA11yAtspiCache
    {
        public uint Version => CacheVersion;

        public ValueTask<List<AtSpiAccessibleCacheItem>> GetItemsAsync()
        {
            var snapshot = server.GetAllNodes()
                .OrderBy(static node => node.Path, System.StringComparer.Ordinal)
                .ToList();

            var items = new List<AtSpiAccessibleCacheItem>(snapshot.Count + 1) { server.BuildAppRootCacheItem() };
            items.AddRange(snapshot.Select(n => server.BuildCacheItem(n)));
            return ValueTask.FromResult(items);
        }

        public void EmitAddAccessibleSignal(AtSpiAccessibleCacheItem item)
        {
            EmitSignal("AddAccessible", item);
        }

        public void EmitRemoveAccessibleSignal(AtSpiObjectReference node)
        {
            EmitSignal("RemoveAccessible", node);
        }

        private void EmitSignal(string member, params object[] body)
        {
            var connection = server.A11yConnection;
            if (connection is null)
                return;

            var message = DBusMessage.CreateSignal(
                (DBusObjectPath)CachePath,
                IfaceCache,
                member,
                body);

            _ = connection.SendMessageAsync(message);
        }
    }
}
