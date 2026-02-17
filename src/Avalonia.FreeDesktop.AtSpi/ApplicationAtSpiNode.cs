using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using Avalonia.FreeDesktop.AtSpi.Handlers;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// Synthetic application root node that is not backed by an <see cref="Avalonia.Automation.Peers.AutomationPeer"/>.
    /// Registered at <c>/org/a11y/atspi/accessible/root</c> and serves as the AT-SPI tree root.
    /// </summary>
    internal sealed class ApplicationAtSpiNode(string? applicationName)
    {
        private readonly List<RootAtSpiNode> _windowChildren = [];

        public string Path => RootPath;
        public string Name { get; } = applicationName
                                      ?? Application.Current?.Name
                                      ?? Process.GetCurrentProcess().ProcessName;

        public AtSpiRole Role => AtSpiRole.Application;
        public List<RootAtSpiNode> WindowChildren => _windowChildren;

        public void AddWindowChild(RootAtSpiNode windowNode) => _windowChildren.Add(windowNode);
        public void RemoveWindowChild(RootAtSpiNode windowNode) => _windowChildren.Remove(windowNode);
    }

    /// <summary>
    /// <see cref="IOrgA11yAtspiAccessible"/> implementation for <see cref="ApplicationAtSpiNode"/>.
    /// </summary>
    internal sealed class ApplicationAccessibleHandler(AtSpiServer server, ApplicationAtSpiNode appNode)
        : IOrgA11yAtspiAccessible
    {
        public uint Version => AccessibleVersion;
        public string Name => appNode.Name;
        public string Description => string.Empty;

        public AtSpiObjectReference Parent =>
            new(string.Empty, new DBusObjectPath(NullPath));

        public int ChildCount => appNode.WindowChildren.Count;
        public string Locale => ResolveLocale();
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
            ValueTask.FromResult(BuildStateSet(new[] { AtSpiState.Active }));

        public ValueTask<AtSpiAttributeSet> GetAttributesAsync() =>
            ValueTask.FromResult(new AtSpiAttributeSet());

        public ValueTask<AtSpiObjectReference> GetApplicationAsync() =>
            ValueTask.FromResult(server.GetRootReference());

        public ValueTask<List<string>> GetInterfacesAsync()
        {
            var interfaces = new List<string> { IfaceAccessible, IfaceApplication };
            interfaces.Sort(StringComparer.Ordinal);
            return ValueTask.FromResult(interfaces);
        }
    }

    /// <summary>
    /// <see cref="IOrgA11yAtspiApplication"/> implementation for <see cref="ApplicationAtSpiNode"/>.
    /// </summary>
    internal sealed class ApplicationNodeApplicationHandler : IOrgA11yAtspiApplication
    {
        public ApplicationNodeApplicationHandler()
        {
            var version = ResolveToolkitVersion();
            ToolkitName = "Avalonia";
            Version = version;
            ToolkitVersion = version;
            AtspiVersion = "2.1";
            InterfaceVersion = ApplicationVersion;
        }

        public string ToolkitName { get; }
        public string Version { get; }
        public string ToolkitVersion { get; }
        public string AtspiVersion { get; }
        public uint InterfaceVersion { get; }
        public int Id { get; set; }

        public ValueTask<string> GetLocaleAsync(uint lctype) =>
            ValueTask.FromResult(ResolveLocale());

        public ValueTask<string> GetApplicationBusAddressAsync() =>
            ValueTask.FromResult(string.Empty);
    }
}
