using System;
using System.Runtime.InteropServices;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Platform;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
#if !NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode("Requires .NET COM interop")]
#endif
    internal class RootAutomationNode : AutomationNode, IRawElementProviderFragmentRoot
    {
        public RootAutomationNode(AutomationPeer peer)
            : base(peer)
        {
            Peer = base.Peer.GetProvider<IRootProvider>() ?? throw new AvaloniaInternalException(
                "Attempt to create RootAutomationNode from peer which does not implement IRootProvider.");
            Peer.FocusChanged += OnRootFocusChanged;
        }

        public override IRawElementProviderFragmentRoot? FragmentRoot => this;
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

        public Rect ToScreen(Rect rect)
        {
            if (WindowImpl is null)
                return default;
            return new PixelRect(
                WindowImpl.PointToScreen(rect.TopLeft),
                WindowImpl.PointToScreen(rect.BottomRight))
                    .ToRect(1);
        }

        public override IRawElementProviderSimple? HostRawElementProvider
        {
            get
            {
                var handle = WindowImpl?.Handle.Handle ?? IntPtr.Zero;
                if (handle == IntPtr.Zero)
                    return null;
#pragma warning disable IL2050 // Correctness of COM interop cannot be guaranteed after trimming. Interfaces and interface members might be removed.
                var hr = UiaCoreProviderApi.UiaHostProviderFromHwnd(handle, out var result);
#pragma warning restore IL2050 // Correctness of COM interop cannot be guaranteed after trimming. Interfaces and interface members might be removed.
                Marshal.ThrowExceptionForHR(hr);
                return result;
            }
        }

        private void OnRootFocusChanged(object? sender, EventArgs e)
        {
            RaiseFocusChanged(GetOrCreate(Peer.GetFocus()));
        }
    }

#if NET6_0_OR_GREATER
    internal unsafe partial class AutomationNodeWrapper :
        IRawElementProviderFragmentRoot
    {
        public void* IRawElementProviderFragmentRootInst { get; init; }

        IRawElementProviderFragment? IRawElementProviderFragmentRoot.ElementProviderFromPoint(double x, double y)
        {
            return IRawElementProviderFragmentRootNativeWrapper.ElementProviderFromPoint(IRawElementProviderFragmentInst, x, y);
        }

        IRawElementProviderFragment? IRawElementProviderFragmentRoot.GetFocus()
        {
            return IRawElementProviderFragmentRootNativeWrapper.GetFocus(IRawElementProviderFragmentInst);
        }
    }
#endif
}
