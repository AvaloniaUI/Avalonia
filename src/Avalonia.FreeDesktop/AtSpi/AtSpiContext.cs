using Avalonia.Automation.Peers;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class AtSpiContext(Connection connection)
{
    private static bool s_instanced;
    public static AtSpiContext? Instance { get; private set; }

    public Connection Connection => connection;

    public void RegisterRootAutomationPeer(AutomationPeer peer)
    {
        
    }

    public static async void Initialize()
    {
        if (s_instanced || DBusHelper.Connection is not { } sessionConnection) return;

        var bus1 = new OrgA11yBus(sessionConnection, "org.a11y.Bus", "/org/a11y/bus");

        var address = await bus1.GetAddressAsync();

        if (DBusHelper.TryCreateNewConnection(address) is not { } a11YConnection) return;

        await a11YConnection.ConnectAsync();

        Instance = new AtSpiContext(a11YConnection);
        s_instanced = true;
    }
}
