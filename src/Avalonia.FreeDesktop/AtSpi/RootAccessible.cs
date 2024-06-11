using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class RootAccessible : Accessible
{
    public RootAccessible(Connection connection, string serviceName) : base(serviceName, null, connection)
    {
        InternalCacheEntry.Accessible = (serviceName, AtSpiContext.RootPath)!;
        InternalCacheEntry.Application = (serviceName, AtSpiContext.RootPath)!;
        InternalCacheEntry.ApplicableInterfaces = ["org.a11y.atspi.Accessible", "org.a11y.atspi.Application"];
        InternalCacheEntry.Role = AtSpiConstants.Role.Application;
        InternalCacheEntry.LocalizedName = AtSpiConstants.RoleNames[(int)InternalCacheEntry.Role];
        InternalCacheEntry.RoleName = AtSpiConstants.RoleNames[(int)InternalCacheEntry.Role];
        InternalCacheEntry.ChildCount = 0; //TODO
        InternalCacheEntry.ApplicableStates = [0, 0];
        this.InternalGuid = Guid.Empty;
    }
    
}
