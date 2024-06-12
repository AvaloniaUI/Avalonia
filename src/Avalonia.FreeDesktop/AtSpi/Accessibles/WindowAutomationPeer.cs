using System;
using Avalonia.Automation.Peers;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.AtSpi.Accessibles;

internal class WindowAccessible : Accessible
{
    public WindowAutomationPeer Peer { get; }

    public WindowAccessible(string serviceName, Accessible? internalParent,
        WindowAutomationPeer windowAutomationPeer) : base(serviceName, internalParent)
    {
        Peer = windowAutomationPeer;

        Name = "Software";
        Locale = Environment.GetEnvironmentVariable("LANG");
        
        Peer.ChildrenChanged += PeerOnChildrenChanged;
        
        InternalCacheEntry.Role = AtSpiConstants.Role.Frame;
        InternalCacheEntry.Name = "Software";
        InternalCacheEntry.Description = "";
        InternalCacheEntry.ChildCount = 0; //TODO
        InternalCacheEntry.ApplicableStates = [(uint)(1124073472), 0];
    }

    private void PeerOnChildrenChanged(object sender, EventArgs e)
    {
        
    }
}
