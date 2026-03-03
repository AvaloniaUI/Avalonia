using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;

namespace Avalonia.FreeDesktop.AtSpi;

/// <summary>
/// <see cref="IOrgA11yAtspiAccessible"/> implementation for <see cref="ApplicationAtSpiNode"/>.
/// </summary>
internal sealed class ApplicationAccessibleHandler(AtSpiServer server, ApplicationAtSpiNode appNode)
    : IOrgA11yAtspiAccessible
{
    private static readonly List<string> s_interfaces =
    [
        AtSpiConstants.IfaceAccessible,
        AtSpiConstants.IfaceApplication,
    ];

    public uint Version => AtSpiConstants.AccessibleVersion;
    public string Name => appNode.Name;
    public string Description => string.Empty;

    public AtSpiObjectReference Parent =>
        new(string.Empty, new DBusObjectPath(AtSpiConstants.NullPath));

    public int ChildCount => appNode.WindowChildren.Count;
    public string Locale => AtSpiConstants.ResolveLocale();
    public string AccessibleId => string.Empty;
    public string HelpText => string.Empty;

    public ValueTask<AtSpiObjectReference> GetChildAtIndexAsync(int index)
    {
        var children = appNode.WindowChildren;
        if (index >= 0 && index < children.Count)
            return ValueTask.FromResult(server.GetReference(children[index]));
        return ValueTask.FromResult(server.GetNullReference());
    }

    public ValueTask<List<AtSpiObjectReference>> GetChildrenAsync()
    {
        var children = appNode.WindowChildren;
        var refs = new List<AtSpiObjectReference>(children.Count);
        foreach (var child in children)
            refs.Add(server.GetReference(child));
        return ValueTask.FromResult(refs);
    }

    public ValueTask<int> GetIndexInParentAsync() => ValueTask.FromResult(-1);

    public ValueTask<List<AtSpiRelationEntry>> GetRelationSetAsync() =>
        ValueTask.FromResult(new List<AtSpiRelationEntry>());

    public ValueTask<uint> GetRoleAsync() => ValueTask.FromResult((uint)AtSpiRole.Application);

    public ValueTask<string> GetRoleNameAsync() => ValueTask.FromResult("application");

    public ValueTask<string> GetLocalizedRoleNameAsync() => ValueTask.FromResult("application");

    public ValueTask<List<uint>> GetStateAsync() =>
        ValueTask.FromResult(AtSpiConstants.BuildStateSet([AtSpiState.Active]));

    public ValueTask<AtSpiAttributeSet> GetAttributesAsync() =>
        ValueTask.FromResult(new AtSpiAttributeSet());

    public ValueTask<AtSpiObjectReference> GetApplicationAsync() =>
        ValueTask.FromResult(server.GetRootReference());

    public ValueTask<List<string>> GetInterfacesAsync() =>
        ValueTask.FromResult(s_interfaces.ToList());
}
