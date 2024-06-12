using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Avalonia.FreeDesktop.AtSpi;

internal class EventObject(Connection connection) : OrgA11yAtspiEventObject
{
    public override Connection Connection => connection;
    protected override ValueTask OnDummyAsync()
    {
        return default;
    }

    public void EmitOnPropertyChange(string? @property, int arg1, int arg2, Variant value,
        Dictionary<string, Variant>? properties)
        => EmitPropertyChange(@property, arg1, arg2, value,
            properties);

    public void EmitOnChildrenChange(string? operation, int indexInParent, int arg2, Variant child,
        Dictionary<string, Variant>? properties) =>
        EmitChildrenChanged(operation, indexInParent, arg2, child, properties);
}

enum GtkAccessibleChildState
{
    GTK_ACCESSIBLE_CHILD_STATE_ADDED,
    GTK_ACCESSIBLE_CHILD_STATE_REMOVED
};

enum GtkAccessibleChildChange
{
    GTK_ACCESSIBLE_CHILD_CHANGE_ADDED = 1 << GtkAccessibleChildState.GTK_ACCESSIBLE_CHILD_STATE_ADDED,
    GTK_ACCESSIBLE_CHILD_CHANGE_REMOVED = 1 << GtkAccessibleChildState.GTK_ACCESSIBLE_CHILD_STATE_REMOVED
};

internal abstract class Accessible : OrgA11yAtspiAccessible
{
    private readonly Accessible? _internalParent;
    protected PathHandler? _pathHandler;
    public Guid InternalGuid { get; protected init; }
    public CacheEntry InternalCacheEntry { get; } = new();
    public string DbusPath { get; }
    public string ServiceName { get; }
    public override Connection Connection { get; }
    public Accessible? InternalParent => _internalParent;


    protected Accessible(string serviceName, Accessible? internalParent, Connection? connection = null)
    {
        if (connection is null && internalParent is not null)
        {
            _internalParent = internalParent;
            Parent = internalParent.Convert();
            ServiceName = serviceName;
            InternalGuid = Guid.NewGuid();

            DbusPath = Path.Combine(AtSpiConstants.AvaloniaPathPrefix, InternalGuid.ToString("N"));

            InternalCacheEntry.Accessible = (serviceName, DbusPath);
            InternalCacheEntry.Parent = Parent!;
            InternalCacheEntry.Application = internalParent.InternalCacheEntry.Application;

            this.Locale = (Environment.GetEnvironmentVariable("LANG") ?? string.Empty);

            _pathHandler = new PathHandler(DbusPath);
            _pathHandler.Add(this);
            Connection = internalParent.Connection;
            _eventObject = new EventObject(Connection);
            Connection.AddMethodHandler(_pathHandler);

            internalParent.AddChild(this);
        }
        else if (connection is not null && this.Connection is null)
        {
            
            InternalCacheEntry.Accessible = (serviceName, AtSpiContext.RootPath)!;
            InternalCacheEntry.Application = (serviceName, AtSpiContext.RootPath)!;
            InternalCacheEntry.ApplicableInterfaces = ["org.a11y.atspi.Accessible", "org.a11y.atspi.Application"];
            InternalCacheEntry.Role = AtSpiConstants.Role.Application;
            InternalCacheEntry.Name = Application.Current?.Name ?? "Avalonia Application";
            InternalCacheEntry.Description = string.Empty;
            InternalCacheEntry.ChildCount = 0; //TODO
            InternalCacheEntry.ApplicableStates = [0, 0];
            this.InternalGuid = Guid.Empty;
            
            Connection = connection;
            DbusPath = AtSpiContext.RootPath;
            ServiceName = serviceName;
            _pathHandler = new PathHandler(DbusPath);
            _eventObject = new EventObject(Connection);
            _pathHandler.Add(this);
            _pathHandler.Add(_eventObject);
            Connection.AddMethodHandler(_pathHandler);
        }
    }

    public (string, ObjectPath) Convert() => (ServiceName, (ObjectPath)DbusPath);


    private readonly List<Accessible> _internalChildren = new();
    private readonly EventObject _eventObject;

    public void AddChild(Accessible accessible)
    {
        _internalChildren.Add(accessible);
        ChildCount = _internalChildren.Count;
        InternalCacheEntry.ChildCount = ChildCount;
        AtSpiContext.Cache.TryAddEntry(accessible);

        var k = new Struct<string, ObjectPath>(ServiceName, accessible.DbusPath);
        
        // new Thread(async () =>
        // {
        //     for (int i = 0; i < 10; i++)
        //     {
        //         await Task.Delay(2000);
        //         _eventObject.EmitOnPropertyChange("accessible-childcount", 0, 0, new Variant($"test{i}"), null);
        //     }
        // }).Start();
        
        _eventObject.EmitOnChildrenChange("add", _internalChildren.IndexOf(accessible), 0, 
           k , null);
    }


    public void RemoveChild(Accessible accessible)
    {
        _internalChildren.Remove(accessible);
        ChildCount = _internalChildren.Count;
    }

    public IList<Accessible> InternalChildren => _internalChildren;

    protected override async ValueTask<(string, ObjectPath)> OnGetChildAtIndexAsync(int index)
    {
        var accessible = InternalChildren.ElementAtOrDefault(index);
        if (accessible is not null)
        {
            return accessible.Convert();
        }

        return ("", "/org/a11y/atspi/null");
    }

    protected override async ValueTask<(string, ObjectPath)[]> OnGetChildrenAsync()
    {
        return InternalChildren.Select(x => x.Convert()).ToArray();
    }

    protected override async ValueTask<int> OnGetIndexInParentAsync()
    {
        if (_internalParent is null) return -1;
        return _internalParent.InternalChildren.IndexOf(this);
    }

    protected override async ValueTask<(uint, (string, ObjectPath)[])[]> OnGetRelationSetAsync()
    {
#pragma warning disable CS8603 // Possible null reference return.
        return default;
#pragma warning restore CS8603 // Possible null reference return.
    }

    protected override async ValueTask<uint> OnGetRoleAsync()
    {
        return (uint)InternalCacheEntry.Role;
    }

    protected override async ValueTask<string> OnGetRoleNameAsync()
    {
        return AtSpiConstants.RoleNames[(int)InternalCacheEntry.Role];
    }

    protected override async ValueTask<string> OnGetLocalizedRoleNameAsync()
    {
        return AtSpiConstants.RoleNames[(int)InternalCacheEntry.Role];
    }

    protected override async ValueTask<uint[]> OnGetStateAsync()
    {
        return InternalCacheEntry.ApplicableStates;
    }

    protected override async ValueTask<Dictionary<string, string>> OnGetAttributesAsync()
    {
        return new() { { "toolkit", "Avalonia" } };
    }

    protected override async ValueTask<(string, ObjectPath)> OnGetApplicationAsync()
    {
        return InternalCacheEntry.Application;
    }

    protected override async ValueTask<string[]> OnGetInterfacesAsync()
    {
        return InternalCacheEntry.ApplicableInterfaces;
    }
}
