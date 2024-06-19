using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation;

/// <summary>
/// An automation node which serves as the root of an embedded native control automation tree.
/// </summary>
[RequiresUnreferencedCode("Requires .NET COM interop")]
internal class InteropAutomationNode : AutomationNode, IRawElementProviderFragmentRoot
{
    private readonly IntPtr _handle;

    public InteropAutomationNode(InteropAutomationPeer peer)
        : base(peer)
    {
        _handle = peer.NativeControlHandle.Handle;
    }

    public override Rect BoundingRectangle => default;
    public override IRawElementProviderFragmentRoot? FragmentRoot => null;
    public override ProviderOptions ProviderOptions => ProviderOptions.ServerSideProvider | ProviderOptions.OverrideProvider;

    public override object? GetPatternProvider(int patternId) => null;
    public override object? GetPropertyValue(int propertyId) => null;

    public override IRawElementProviderSimple? HostRawElementProvider
    {
        get
        {
            var hr = UiaCoreProviderApi.UiaHostProviderFromHwnd(_handle, out var result);
            Marshal.ThrowExceptionForHR(hr);
            return result;
        }
    }

    public override IRawElementProviderFragment? Navigate(NavigateDirection direction)
    {
        return direction == NavigateDirection.Parent ? base.Navigate(direction) : null;
    }

    public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y) => null;
    public IRawElementProviderFragment? GetFocus() => null;
    public IRawElementProviderSimple[]? GetEmbeddedFragmentRoots() => null;
}
