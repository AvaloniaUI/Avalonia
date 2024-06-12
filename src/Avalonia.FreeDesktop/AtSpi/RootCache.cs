using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class RootCache : OrgA11yAtspiCache
{
    public bool TryAddEntry(Accessible accessible)
    {
        if (!_globalCache.TryAdd(accessible.InternalGuid, accessible))
        {
            return false;
        }

        // if (Connection is not null)
        //     EmitAddAccessible(accessible.InternalCacheEntry.Convert());
        return true;
    }

    public Accessible? GetCacheEntry(Guid id)
    {
        return _globalCache.TryGetValue(id, out var entry) ? entry : default;
    }

    public bool TryRemoveEntry(Guid id)
    {
        if (!_globalCache.TryGetValue(id, out var item)) return false;
        var ret = _globalCache.Remove(id);
       // if (ret && Connection is not null)
       //     EmitRemoveAccessible(item.Convert());
        return ret;
    }

    private Dictionary<Guid, Accessible> _globalCache = new();

    public RootCache(Connection a11YConnection)
    {
        Connection = a11YConnection;
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
        return default;
        // return (_globalCache.Values.Select(x => x.InternalCacheEntry.Convert()
        // ).ToArray());
    }
}
