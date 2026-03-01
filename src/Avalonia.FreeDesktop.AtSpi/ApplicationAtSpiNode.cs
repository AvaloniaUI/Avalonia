using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
}
