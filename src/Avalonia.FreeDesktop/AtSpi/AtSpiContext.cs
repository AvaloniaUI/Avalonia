using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Reactive;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal class AtSpiContext
{
    class RootCache : OrgA11yAtspiCache
    {
        public override Connection Connection { get; }
        
        protected override ValueTask<(
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
        }
    }

    class RootAccessible : OrgA11yAtspiAccessible
    {
        public override Connection Connection { get; }

        public RootAccessible()
        {
            
        }
        
        protected override async ValueTask<(string, ObjectPath)> OnGetChildAtIndexAsync(int index)
        {
            return default;
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
            return (uint)AtSpiConstants.Role.Application;
        }

        protected override async ValueTask<string> OnGetRoleNameAsync()
        {
            return default;
        }

        protected override async ValueTask<string> OnGetLocalizedRoleNameAsync()
        {
            return default;
        }

        protected override async ValueTask<uint[]> OnGetStateAsync()
        {
            return default;
        }

        protected override async ValueTask<Dictionary<string, string>> OnGetAttributesAsync()
        {
            return default;
        }

        protected override async ValueTask<(string, ObjectPath)> OnGetApplicationAsync()
        {
            return default;
        }

        protected override async ValueTask<string[]> OnGetInterfacesAsync()
        {
            return ["org.a11y.atspi.Accessible", "org.a11y.atspi.Application"];
        }
    }

    class RootApplication : OrgA11yAtspiApplication
    {
        public RootApplication()
        {
            AtspiVersion = _AtspiVersion;
            ToolkitName = "Avalonia";
            Id = 0;
            Version = typeof(RootApplication).Assembly.GetName().Version?.ToString();
        }

        private const string _AtspiVersion = "2.1";

        public override Connection Connection { get; }

        protected override async ValueTask<string> OnGetLocaleAsync(uint lctype)
        {
            return Environment.GetEnvironmentVariable("LANG") ?? string.Empty;
        }
    }

    private static bool s_instanced;
    private static RootCache cache;
    private static string? serviceName;
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

        var res = socket.EmbedAsync((serviceName, new ObjectPath(RootPath))!).GetAwaiter().GetResult();

        if (!res.Item1.StartsWith(":1.") || res.Item2.ToString() != RootPath) return;
        ac0.Parent = res;
        ac0.Name = Application.Current?.Name ?? "Avalonia Application";
    }


    public const string RootPath = "/org/a11y/atspi/accessible/root";

    public static AtSpiContext? Instance { get; private set; }

    public Connection Connection => _connection;

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

        cache = new RootCache();

        var cachePathHandler = new PathHandler("/org/a11y/atspi/cache");

        cachePathHandler.Add(cache);

        a11YConnection.AddMethodHandler(cachePathHandler);

        serviceName = a11YConnection.UniqueName;
        Instance = new AtSpiContext(a11YConnection);

        s_instanced = true;
    }
}
