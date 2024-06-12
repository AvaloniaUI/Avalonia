using System;
using Avalonia.Automation.Peers;
using Avalonia.FreeDesktop.AtSpi.Accessibles;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;
#pragma warning disable CA1823

internal class AtSpiContext
{
    private static bool s_instanced;
    public static RootCache? Cache;
    public static string? ServiceName;
    private Connection _connection;
    private readonly RootAccessible _rootAccessible;

    public AtSpiContext(Connection connection)
    {
        _connection = connection;

        if (ServiceName == null) return;
        var ac1 = new RootApplication(connection);
        this._rootAccessible = new RootAccessible(connection, ServiceName, ac1);

        var socket = new OrgA11yAtspiSocket(_connection, "org.a11y.atspi.Registry", RootPath);

        var res = socket.EmbedAsync((ServiceName, new ObjectPath(RootPath))!).GetAwaiter().GetResult();

        if (!res.Item1.StartsWith(":") || res.Item2.ToString() != RootPath) return;
        _rootAccessible.Parent = res;
        
        var appName = Application.Current?.Name ?? "Avalonia Application";
        _rootAccessible.Name = appName;
        _rootAccessible.InternalCacheEntry.Description = "application";
        
        Cache?.TryAddEntry(_rootAccessible);
    }

    public const string RootPath = "/org/a11y/atspi/accessible/root";

    public static AtSpiContext? Instance { get; private set; }

    public Connection Connection => _connection;

    public void RegisterRootAutomationPeer(AutomationPeer peer)
    {
        if (peer is WindowAutomationPeer winAutoPeer && ServiceName != null)
        {
            var win = new WindowAccessible(ServiceName, _rootAccessible, winAutoPeer);
        }
    }

    public static async void Initialize()
    {
        if (s_instanced || DBusHelper.Connection is not { } sessionConnection) return;

        var bus1 = new OrgA11yBus(sessionConnection, "org.a11y.Bus", "/org/a11y/bus");

        var address = await bus1.GetAddressAsync();

        if (DBusHelper.TryCreateNewConnection(address) is not { } a11YConnection) return;

        await a11YConnection.ConnectAsync();

        Cache = new RootCache(a11YConnection);

        var cachePathHandler = new PathHandler("/org/a11y/atspi/cache");

        cachePathHandler.Add(Cache);

        a11YConnection.AddMethodHandler(cachePathHandler);
        
        ServiceName = a11YConnection.UniqueName;

        Instance = new AtSpiContext(a11YConnection);

        s_instanced = true;
    }
}
