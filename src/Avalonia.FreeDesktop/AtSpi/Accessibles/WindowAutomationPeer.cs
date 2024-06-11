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
        
        Name = Peer.GetName();
        
        Peer.ChildrenChanged += PeerOnChildrenChanged;
        
        InternalCacheEntry.Role = AtSpiConstants.Role.Window;
        InternalCacheEntry.LocalizedName = AtSpiConstants.RoleNames[(int)InternalCacheEntry.Role];
        InternalCacheEntry.RoleName = AtSpiConstants.RoleNames[(int)InternalCacheEntry.Role];
        InternalCacheEntry.ChildCount = 0; //TODO
        InternalCacheEntry.ApplicableStates = [(uint)(AtSpiConstants.State.Visible), 0];
    }

    private void PeerOnChildrenChanged(object sender, EventArgs e)
    {
        
    }
}
