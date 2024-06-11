using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Logging;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop.AtSpi;

internal abstract class Accessible : OrgA11yAtspiAccessible
{
    private readonly Accessible? _internalParent;
    public Guid InternalGuid { get; protected set; }
    public CacheEntry InternalCacheEntry { get; } = new();
    public string DbusPath { get; }
    public string ServiceName { get; }

    public Accessible? InternalParent => _internalParent;

    protected Accessible(string serviceName, Accessible? internalParent)
    {
        _internalParent = internalParent;
        ServiceName = serviceName;
        InternalGuid = new Guid();
        DbusPath = Path.Combine(AtSpiConstants.AvaloniaPathPrefix, InternalGuid.ToString("N"));
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


    protected override ValueTask<(string, ObjectPath)> OnGetChildAtIndexAsync(int index)
    {
        var accessible = InternalChildren.ElementAtOrDefault(index);
        if (accessible is not null)
        {
            return ValueTask.FromResult(accessible.Convert());
        }

        return ValueTask.FromResult<(string, ObjectPath)>((":0.0", "/org/a11y/atspi/accessible/null"));
    }

    protected override ValueTask<(string, ObjectPath)[]> OnGetChildrenAsync()
    {
        return ValueTask.FromResult(InternalChildren.Select(x => x.Convert()).ToArray());
    }

    protected override ValueTask<int> OnGetIndexInParentAsync()
    {
        if (_internalParent is null) return ValueTask.FromResult(-1);
        return ValueTask.FromResult(_internalParent.InternalChildren.IndexOf(this));
    }

    protected override ValueTask<(uint, (string, ObjectPath)[])[]> OnGetRelationSetAsync()
    {
        return ValueTask.FromResult<(uint, (string, ObjectPath)[])[]>(default!);
    }

    protected override ValueTask<uint> OnGetRoleAsync()
    {
        return ValueTask.FromResult((uint)InternalCacheEntry.Role);
    }

    protected override ValueTask<string> OnGetRoleNameAsync()
    {
        return ValueTask.FromResult(InternalCacheEntry.RoleName);
    }

    protected override ValueTask<string> OnGetLocalizedRoleNameAsync()
    {
        return ValueTask.FromResult(InternalCacheEntry.LocalizedName);
    }

    protected override ValueTask<uint[]> OnGetStateAsync()
    {
        return ValueTask.FromResult(InternalCacheEntry.ApplicableStates);
    }

    protected override ValueTask<Dictionary<string, string>> OnGetAttributesAsync()
    {
        return ValueTask.FromResult<Dictionary<string, string>>(new() { { "toolkit", "Avalonia" } });
    }

    protected override ValueTask<(string, ObjectPath)> OnGetApplicationAsync()
    {
        return ValueTask.FromResult(InternalCacheEntry.Application);
    }

    protected override ValueTask<string[]> OnGetInterfacesAsync()
    {
        return ValueTask.FromResult(InternalCacheEntry.ApplicableInterfaces);
    }
}


