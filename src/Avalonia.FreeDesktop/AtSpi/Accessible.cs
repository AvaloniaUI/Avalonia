using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Avalonia.FreeDesktop.AtSpi;

internal abstract class Accessible : OrgA11yAtspiAccessible
{
    private readonly Accessible? _internalParent;
    private  PathHandler? _pathHandler;
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

            internalParent.Connection.AddMethodHandler(_pathHandler);
            
            internalParent.AddChild(this);
        }
        else if(connection is not null && this.Connection is null)
        {
            Connection = connection;
            DbusPath = AtSpiContext.RootPath;
            ServiceName = serviceName;
        }
    }

    public (string, ObjectPath) Convert() => (ServiceName, (ObjectPath)DbusPath);


    private readonly List<Accessible> _internalChildren = new();

    public void AddChild(Accessible accessible)
    {
        if (AtSpiContext.Cache != null && AtSpiContext.Cache.TryAddEntry(accessible))
        {
            _internalChildren.Add(accessible);
            ChildCount = _internalChildren.Count;
        }
        else
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.FreeDesktopPlatform)?.Log(this, 
                $"Unable to add Accessible object to the root AT-SPI cache. {accessible.InternalGuid}");

        }
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

        return ("", "/org/a11y/atspi/accessible/null");
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
        return InternalCacheEntry.RoleName;
    }

    protected override async ValueTask<string> OnGetLocalizedRoleNameAsync()
    {
        return InternalCacheEntry.LocalizedName;
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


