using System;
using Avalonia.Automation.Peers;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;
#pragma warning disable CA1823

internal class AtSpiContext
{
    public const string AvaloniaPathPrefix = "/net/avaloniaui/accessibles/";
    private static bool s_instanced;
    public static RootCache? Cache;
    public static string? ServiceName;
    private Connection _connection;

    public AtSpiContext(Connection connection)
    {
        _connection = connection;

        var ac0 = new RootAccessible();
        var ac1 = new RootApplication();
        var path = "/org/a11y/atspi/accessible/root";
        var pathHandler = new PathHandler(path);

        pathHandler.Add(ac0);
        pathHandler.Add(ac1);

        _connection.AddMethodHandler(pathHandler);

        var socket = new OrgA11yAtspiSocket(_connection, "org.a11y.atspi.Registry", RootPath);

        var res = socket.EmbedAsync((ServiceName, new ObjectPath(RootPath))!).GetAwaiter().GetResult();

        if (!res.Item1.StartsWith(":") || res.Item2.ToString() != RootPath || ServiceName is not { }) return;
        ac0.Parent = res;
        ac0.Name = Application.Current?.Name ?? "Avalonia Application";

        if (!Cache.TryAddEntry(Guid.Empty, ac0.CacheEntry))
        {
            // shouldnt happen.
        }
    }

    public const string RootPath = "/org/a11y/atspi/accessible/root";

    public static AtSpiContext? Instance { get; private set; }

    public Connection Connection => _connection;

    public void RegisterRootAutomationPeer(AutomationPeer peer)
    {
        // DelayedInit();
    }

    public static async void Initialize()
    {
        if (s_instanced || DBusHelper.Connection is not { } sessionConnection) return;

        var bus1 = new OrgA11yBus(sessionConnection, "org.a11y.Bus", "/org/a11y/bus");

        var address = await bus1.GetAddressAsync();

        if (DBusHelper.TryCreateNewConnection(address) is not { } a11YConnection) return;

        await a11YConnection.ConnectAsync();

        Cache = new RootCache();

        var cachePathHandler = new PathHandler("/org/a11y/atspi/cache");

        cachePathHandler.Add(Cache);

        a11YConnection.AddMethodHandler(cachePathHandler);


        ServiceName = a11YConnection.UniqueName;

        Instance = new AtSpiContext(a11YConnection);

        s_instanced = true;
    }
}
