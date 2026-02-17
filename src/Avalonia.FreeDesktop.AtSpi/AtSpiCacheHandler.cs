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
        private bool _reentryGuard;

        public uint Version => CacheVersion;

        public ValueTask<List<AtSpiAccessibleCacheItem>> GetItemsAsync()
        {
            _reentryGuard = true;
            try
            {
                var snapshot = server.GetAllNodes()
                    .OrderBy(static node => node.Path, System.StringComparer.Ordinal)
                    .ToList();

                List<AtSpiAccessibleCacheItem> items = [];
                items.AddRange(snapshot.Select(server.BuildCacheItem));
                return ValueTask.FromResult(items);
            }
            finally
            {
                _reentryGuard = false;
            }
        }

        public void EmitAddAccessibleSignal(AtSpiAccessibleCacheItem item)
        {
            if (_reentryGuard)
                return;
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
