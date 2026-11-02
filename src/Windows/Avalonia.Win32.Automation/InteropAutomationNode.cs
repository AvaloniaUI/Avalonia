using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation;

/// <summary>
/// An automation node which serves as the root of an embedded native control automation tree.
/// </summary>
    [GeneratedComClass]
internal partial class InteropAutomationNode : AutomationNode, IRawElementProviderFragmentRoot
{
    private readonly IntPtr _handle;

    public InteropAutomationNode(InteropAutomationPeer peer)
        : base(peer)
    {
        _handle = peer.NativeControlHandle.Handle;
    }

    public override Rect GetBoundingRectangle() => default;
    public override IRawElementProviderFragmentRoot? GetFragmentRoot() => null;
    public override ProviderOptions GetProviderOptions() => ProviderOptions.ServerSideProvider | ProviderOptions.OverrideProvider;

    public override object? GetPatternProvider(int patternId) => null;
    public override object? GetPropertyValue(int propertyId) => null;

    public override IRawElementProviderSimple? GetHostRawElementProvider()
    {
        var hr = UiaCoreProviderApi.UiaHostProviderFromHwnd(GetHostedWindow(_handle), out var result);
        Marshal.ThrowExceptionForHR(hr);
        return result;
    }

    internal static IntPtr GetHostedWindow(IntPtr handle)
    {
        var root = GetAncestor(handle, GA_ROOT);
        var hwnd = handle;

        while (GetAncestor(hwnd, GA_PARENT) is var parent && parent != IntPtr.Zero && parent != root)
            hwnd = parent;

        return hwnd;
    }

    public override IRawElementProviderFragment? Navigate(NavigateDirection direction)
    {
        return direction == NavigateDirection.Parent ? base.Navigate(direction) : null;
    }

    public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y) => null;
    public IRawElementProviderFragment? GetFocus() => null;
    public IRawElementProviderSimple[]? GetEmbeddedFragmentRoots() => null;

    private const uint GA_PARENT = 1;
    private const uint GA_ROOT = 2;

    [LibraryImport("user32.dll")]
    private static partial IntPtr GetAncestor(IntPtr hwnd, uint flags);
}
