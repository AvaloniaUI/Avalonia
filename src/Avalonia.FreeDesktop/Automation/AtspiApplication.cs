using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.Automation;

internal class AtspiApplication : OrgA11yAtspiApplication
{
    private static bool s_instanceInitialized;
    private static AtspiApplication? _instance;

    private AtspiApplication(Connection connection, string destination, string path) : base(connection, destination, path)
    {
    }
    
    
    public static async Task<AtspiApplication?> RegisterRoot()
    {
        if (!s_instanceInitialized && DBusHelper.Connection is Connection sessionConnection)
        {
            var bus1 = new OrgA11yBus(sessionConnection, "org.a11y.Bus", "/org/a11y/bus");

            var address = await bus1.GetAddressAsync();
        
            _instance =  new AtspiApplication(sessionConnection, "org.a11y.atspi.Application", address); 
            s_instanceInitialized = true;
        }

        // _instance?._children.Add(new Child(peerGetter));
        return _instance;
    }


}
