using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class RootAccessible : OrgA11yAtspiAccessible
{
    public override Connection Connection { get; }
    public RootCache.CacheEntry CacheEntry { get; } = new();

    public RootAccessible()
    {
        CacheEntry.Accessible = (AtSpiContext.ServiceName, AtSpiContext.RootPath)!;
        CacheEntry.Application = (AtSpiContext.ServiceName, AtSpiContext.RootPath)!;
        CacheEntry.ApplicableInterfaces = ["org.a11y.atspi.Accessible", "org.a11y.atspi.Application"];
        CacheEntry.Role = AtSpiConstants.Role.Application;
        CacheEntry.LocalizedName = AtSpiConstants.RoleNames[(int)CacheEntry.Role];
        CacheEntry.RoleName = AtSpiConstants.RoleNames[(int)CacheEntry.Role];
        CacheEntry.ChildCount = 0; //TODO
        CacheEntry.ApplicableStates = [0, 0];
    }

    protected override async ValueTask<(string, ObjectPath)> OnGetChildAtIndexAsync(int index)
    {
        return (":0.0", "/org/a11y/atspi/accessible/null");
    }

    protected override async ValueTask<(string, ObjectPath)[]> OnGetChildrenAsync()
    {
        return default;
    }

    protected override async ValueTask<int> OnGetIndexInParentAsync()
    {
        return -1;
    }

    protected override async ValueTask<(uint, (string, ObjectPath)[])[]> OnGetRelationSetAsync()
    {
        return default;
    }

    protected override async ValueTask<uint> OnGetRoleAsync()
    {
        return (uint)CacheEntry.Role;
    }

    protected override async ValueTask<string> OnGetRoleNameAsync()
    {
        return CacheEntry.RoleName;
    }

    protected override async ValueTask<string> OnGetLocalizedRoleNameAsync()
    {
        return CacheEntry.LocalizedName;
    }

    protected override async ValueTask<uint[]> OnGetStateAsync()
    {
        return CacheEntry.ApplicableStates;
    }

    protected override async ValueTask<Dictionary<string, string>> OnGetAttributesAsync()
    {
        return new() { { "toolkit", "Avalonia" } };
    }

    protected override async ValueTask<(string, ObjectPath)> OnGetApplicationAsync()
    {
        return CacheEntry.Application;
    }

    protected override async ValueTask<string[]> OnGetInterfacesAsync()
    {
        return CacheEntry.ApplicableInterfaces;
    }
}