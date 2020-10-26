using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal class WindowProvider : AutomationProvider, IRawElementProviderFragmentRoot
    {
        public WindowProvider(WindowImpl owner, WindowAutomationPeer peer)
            : base(peer)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public WindowImpl Owner { get; }
        
        public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y)
        {
            var p = Owner.PointToClient(new PixelPoint((int)x, (int)y));
            var peer = InvokeSync(() => Peer.GetPeerFromPoint(p));
            return peer?.PlatformImpl as IRawElementProviderFragment;
        }

        public IRawElementProviderFragment? GetFocus() => null;

        public override IRawElementProviderSimple? HostRawElementProvider
        {
            get
            {
                var hr = UiaCoreProviderApi.UiaHostProviderFromHwnd(Owner.Handle.Handle, out var result);
                Marshal.ThrowExceptionForHR(hr);
                return result;
            }
        }
    }
}
