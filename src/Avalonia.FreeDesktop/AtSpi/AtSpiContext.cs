using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;
#pragma warning disable CA1823

internal class AtSpiContext
{
    public const string AvaloniaPathPrefix = "/net/avaloniaui/accessibles/";

    public class RootCache : OrgA11yAtspiCache
    {
        public bool TryAddEntry(Guid id, CacheEntry cacheEntry)
        {
            if (!_globalCache.TryAdd(id, cacheEntry))
            {
                return false;
            }

            if (Connection is not null)
                EmitAddAccessible(cacheEntry.Convert());
            return true;
        }

        public CacheEntry? GetCacheEntry(Guid id)
        {
            return _globalCache.TryGetValue(id, out var entry) ? entry : default;
        }

        public bool TryRemoveEntry(Guid id)
        {
            if (!_globalCache.TryGetValue(id, out var item)) return false;
            var ret = _globalCache.Remove(id);
            if (ret && Connection is not null)
                EmitRemoveAccessible(item.Accessible);
            return ret;
        }

        private Dictionary<Guid, CacheEntry> _globalCache = new();

        public class CacheEntry
        {
            public (string, ObjectPath) Accessible = (":0.0", "/org/a11y/atspi/accessible/object");
            public (string, ObjectPath) Application = (":0.0", "/org/a11y/atspi/accessible/application");
            public (string, ObjectPath) Parent = (":0.0", "/org/a11y/atspi/accessible/parent");
            public int IndexInParent = 0;
            public int ChildCount = 0;
            public string[] ApplicableInterfaces = [];
            public string LocalizedName = string.Empty;
            public AtSpiConstants.Role Role = default;
            public string RoleName = string.Empty;
            public uint[] ApplicableStates = [];

            public (
                (string, ObjectPath),
                (string, ObjectPath),
                (string, ObjectPath),
                int,
                int,
                string[],
                string,
                uint,
                string,
                uint[]) Convert() => (Accessible,
                Application,
                Parent,
                IndexInParent,
                ChildCount,
                ApplicableInterfaces,
                LocalizedName,
                (uint)Role,
                RoleName,
                ApplicableStates);
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
            return (_globalCache.Values.Select(x => x.Convert()
            ).ToArray());
        }
    }

    class RootAccessible : OrgA11yAtspiAccessible
    {
        public override Connection Connection { get; }
        public RootCache.CacheEntry CacheEntry { get; } = new();

        public RootAccessible()
        {
            CacheEntry.Accessible = (s_serviceName, RootPath)!;
            CacheEntry.Application = (s_serviceName, RootPath)!;
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

    class RootApplication : OrgA11yAtspiApplication
    {
        public RootApplication()
        {
            AtspiVersion = AVersion;
            ToolkitName = "Avalonia";
            Id = 0;
            Version = typeof(RootApplication).Assembly.GetName().Version?.ToString();
        }

        private const string AVersion = "2.1";

        public override Connection Connection { get; }

        protected override async ValueTask<string> OnGetLocaleAsync(uint lctype)
        {
            return Environment.GetEnvironmentVariable("LANG") ?? string.Empty;
        }
    }

    private static bool s_instanced;
    private static RootCache s_cache;
    private static string? s_serviceName;
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

        var res = socket.EmbedAsync((s_serviceName, new ObjectPath(RootPath))!).GetAwaiter().GetResult();

        if (!res.Item1.StartsWith(":") || res.Item2.ToString() != RootPath || s_serviceName is not { }) return;
        ac0.Parent = res;
        ac0.Name = Application.Current?.Name ?? "Avalonia Application";

        if (!s_cache.TryAddEntry(Guid.Empty, ac0.CacheEntry))
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

        s_cache = new RootCache();

        var cachePathHandler = new PathHandler("/org/a11y/atspi/cache");

        cachePathHandler.Add(s_cache);

        a11YConnection.AddMethodHandler(cachePathHandler);


        s_serviceName = a11YConnection.UniqueName;

        Instance = new AtSpiContext(a11YConnection);

        s_instanced = true;
    }
}
