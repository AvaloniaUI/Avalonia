using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.Automation;

internal class HandlerAtspiApplication : OrgA11yAtspiApplication
{
    public HandlerAtspiApplication(Connection connection)
    {
        Connection = connection;
        BackingProperties.AtspiVersion = AtspiVersion;
        BackingProperties.ToolkitName = "Avalonia";
        BackingProperties.Id = 0;
        BackingProperties.Version = typeof(HandlerAtspiApplication).Assembly.GetName().Version.ToString();
    }
    
    private const string AtspiVersion = "2.1";
    protected override Connection Connection { get; }
    public override string Path { get; } = "/org/a11y/atspi/accessible/root";
    protected override async ValueTask<string> OnGetLocaleAsync(uint lctype)
    {
        return string.Empty;
    }
}

internal class AtspiMainContext 
{
    private static bool s_instanceInitialized;
    private static AtspiMainContext? _instance;

    private AtspiMainContext(Connection a11yConnection)
    {
        var app = new HandlerAtspiApplication(a11yConnection);
        a11yConnection.AddMethodHandler(app);

    }

    public static AtspiMainContext? Instance { get => _instance; }
    
    public static async void RegisterRoot()
    {
        if (s_instanceInitialized || DBusHelper.Connection is not { } sessionConnection) return;
        var bus1 = new OrgA11yBus(sessionConnection, "org.a11y.Bus", "/org/a11y/bus");

        var address = await bus1.GetAddressAsync();

        if (DBusHelper.TryCreateNewConnection(address) is not { } a11YConnection) return;
        
        await a11YConnection.ConnectAsync();
        _instance = new AtspiMainContext(a11YConnection); 
        s_instanceInitialized = true;
    } 
}
