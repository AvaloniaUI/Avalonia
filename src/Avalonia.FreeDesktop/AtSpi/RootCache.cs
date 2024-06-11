using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class RootCache : OrgA11yAtspiCache
{
    public bool TryAddEntry(Guid id, CacheEntry cacheEntry)
    {
        if (!_globalCache.TryAdd(id, cacheEntry))
        {
            return false;
        }

        if (Connection is not null)
            EmitAddAccessible(cacheEntry.Convert());
        return true;
    }

    public CacheEntry? GetCacheEntry(Guid id)
    {
        return _globalCache.TryGetValue(id, out var entry) ? entry : default;
    }

    public bool TryRemoveEntry(Guid id)
    {
        if (!_globalCache.TryGetValue(id, out var item)) return false;
        var ret = _globalCache.Remove(id);
        if (ret && Connection is not null)
            EmitRemoveAccessible(item.Accessible);
        return ret;
    }

    private Dictionary<Guid, CacheEntry> _globalCache = new();

    public class CacheEntry
    {
        public (string, ObjectPath) Accessible = (":0.0", "/org/a11y/atspi/accessible/object");
        public (string, ObjectPath) Application = (":0.0", "/org/a11y/atspi/accessible/application");
        public (string, ObjectPath) Parent = (":0.0", "/org/a11y/atspi/accessible/parent");
        public int IndexInParent = 0;
        public int ChildCount = 0;
        public string[] ApplicableInterfaces = [];
        public string LocalizedName = string.Empty;
        public AtSpiConstants.Role Role = default;
        public string RoleName = string.Empty;
        public uint[] ApplicableStates = [];

        public (
            (string, ObjectPath),
            (string, ObjectPath),
            (string, ObjectPath),
            int,
            int,
            string[],
            string,
            uint,
            string,
            uint[]) Convert() => (Accessible,
            Application,
            Parent,
            IndexInParent,
            ChildCount,
            ApplicableInterfaces,
            LocalizedName,
            (uint)Role,
            RoleName,
            ApplicableStates);
    }

    public override Connection? Connection { get; }

    protected override async ValueTask<(
        (string, ObjectPath),
        (string, ObjectPath),
        (string, ObjectPath),
        int,
        int,
        string[],
        string,
        uint,
        string,
        uint[])[]> OnGetItemsAsync()
    {
        return (_globalCache.Values.Select(x => x.Convert()
        ).ToArray());
    }
}