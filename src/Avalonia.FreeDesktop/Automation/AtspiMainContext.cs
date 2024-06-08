using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Tmds.DBus;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.Automation;

internal class HandlerAtspiApplication : OrgA11yAtspiApplication
{
    public const string RootPath = "/org/a11y/atspi/accessible/root";

    public HandlerAtspiApplication(Connection connection)
    {
        Connection = connection;
        AtspiVersion = atspiVersion;
        ToolkitName = "Avalonia";
        Id = 0;
        Version = typeof(HandlerAtspiApplication).Assembly.GetName().Version.ToString();
    }

    private const string atspiVersion = "2.1";
    public override string Path => RootPath;

    protected override Connection Connection { get; }

    protected override async ValueTask<string> OnGetLocaleAsync(uint lctype)
    {
        return String.Empty;
    }

    protected override async ValueTask OnRegisterEventListenerAsync(string @event)
    {
    }

    protected override async ValueTask OnDeregisterEventListenerAsync(string @event)
    {
    }
}

internal class AtspiAccessibleHandler : OrgA11yAtspiAccessible
{
    public AtspiAccessibleHandler(Connection a11YConnection, Func<AutomationPeer?> peer, string path)
    {
        Name = Application.Current?.Name ?? "Avalonia Application";
        Description = string.Empty;
        Locale = Environment.GetEnvironmentVariable("LANG") ?? CultureInfo.CurrentCulture.Name;
        ChildCount = 0; //peer()?.GetChildren()?.Count ?? 0;
        Connection = a11YConnection;
        Path = path;
    }

    protected override Connection Connection { get; }
    public override string Path { get; }

    protected override ValueTask<(string, ObjectPath)> OnGetChildAtIndexAsync(int index)
    {
        return default;
    }

    protected override ValueTask<(string, ObjectPath)[]> OnGetChildrenAsync()
    {
        return default;
    }

    protected override ValueTask<int> OnGetIndexInParentAsync()
    {
        return default;
    }

    protected override ValueTask<(uint, (string, ObjectPath)[])[]> OnGetRelationSetAsync()
    {
        return default;
    }

    protected override ValueTask<uint> OnGetRoleAsync()
    {
        return default;
    }

    protected override ValueTask<string> OnGetRoleNameAsync()
    {
        return default;
    }

    protected override ValueTask<string> OnGetLocalizedRoleNameAsync()
    {
        return default;
    }

    protected override ValueTask<uint[]> OnGetStateAsync()
    {
        return default;
    }

    protected override ValueTask<Dictionary<string, string>> OnGetAttributesAsync()
    {
        return default;
    }

    protected override ValueTask<(string, ObjectPath)> OnGetApplicationAsync()
    {
        return default;
    }

    protected override ValueTask<string[]> OnGetInterfacesAsync()
    {
        return default;
    }
}

internal class AtspiMainContext
{
    private static bool s_instanceInitialized;
    private static AtspiMainContext? _instance;
    private readonly HandlerAtspiApplication _app;
    private readonly Connection _connection;

    private readonly MethodHandlerMultiplexer _multiplex;
    // private readonly List<AtspiAccessibleHandler> handlers = new List<AtspiAccessibleHandler>();

    private AtspiMainContext(Connection a11yConnection)
    {
        _connection = a11yConnection;

        _multiplex = new MethodHandlerMultiplexer(HandlerAtspiApplication.RootPath);

        _connection.AddMethodHandler(_multiplex);

        _app = new HandlerAtspiApplication(_connection);

        _multiplex.AddHandler("org.a11y.atspi.Application", _app);

        var rootAccessible = new AtspiAccessibleHandler(_connection, null, HandlerAtspiApplication.RootPath);

        _multiplex.AddHandler("org.a11y.atspi.Accessible", rootAccessible);
    }

    public void RegisterRootPeer(Func<AutomationPeer?> peer)
    {
    }

    public static AtspiMainContext? Instance { get /*=> _instance*/; }

    public static async void StartService()
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
