using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class RootAccessible : Accessible
{
    public RootAccessible(Connection connection, string serviceName, RootApplication ac1) : base(serviceName, null, connection)
    {
         _pathHandler.Add(ac1);
    }
    
}
