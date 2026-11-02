using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    [GeneratedComClass]
    internal partial class RootAutomationNode : AutomationNode, IRawElementProviderFragmentRoot, IRawElementProviderHwndOverride
    {
        public RootAutomationNode(AutomationPeer peer)
            : base(peer)
        {
            Peer = base.Peer.GetProvider<IRootProvider>() ?? throw new AvaloniaInternalException(
                "Attempt to create RootAutomationNode from peer which does not implement IRootProvider.");
            Peer.FocusChanged += OnRootFocusChanged;
        }

        public override IRawElementProviderFragmentRoot? GetFragmentRoot() => this;
        public new IRootProvider Peer { get; }
        public IWindowBaseImpl? WindowImpl => Peer.PlatformImpl as IWindowBaseImpl;

        public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y)
        {
            if (WindowImpl is null)
                return null;

            var p = WindowImpl.PointToClient(new PixelPoint((int)x, (int)y));
            var found = InvokeSync(() => Peer.GetPeerFromPoint(p));
            var result = GetOrCreate(found) as IRawElementProviderFragment;
            return result;
        }

        public IRawElementProviderFragment? GetFocus()
        {
            var focus = InvokeSync(() => Peer.GetFocus());
            return GetOrCreate(focus);
        }

        public override IRawElementProviderSimple? GetHostRawElementProvider()
        {
            var handle = WindowImpl?.Handle?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero)
                return null;
            var hr = UiaCoreProviderApi.UiaHostProviderFromHwnd(handle, out var result);
            Marshal.ThrowExceptionForHR(hr);
            return result;
        }

        public IRawElementProviderSimple? GetOverrideProviderForHwnd(IntPtr hwnd)
        {
            return InvokeSync<IRawElementProviderSimple?>(() =>
            {
                if (base.Peer is not ControlAutomationPeer { Owner: { } owner })
                    return null;

                foreach (var host in owner.GetVisualDescendants().OfType<NativeControlHost>())
                {
                    if (host.NativeControlHandle is { } handle &&
                        InteropAutomationNode.GetHostedWindow(handle.Handle) == hwnd)
                    {
                        var peer = ControlAutomationPeer.CreatePeerForElement(host)
                            .GetChildren()
                            .OfType<InteropAutomationPeer>()
                            .FirstOrDefault();
                        return GetOrCreate(peer);
                    }
                }

                return null;
            });
        }

        private void OnRootFocusChanged(object? sender, EventArgs e)
        {
            RaiseFocusChanged(GetOrCreate(Peer.GetFocus()));
        }
    }
}
