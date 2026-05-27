using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using global::Avalonia.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Peers;
using AvPeer = global::Avalonia.Automation.Peers.AutomationPeer;
using AvControlPeer = global::Avalonia.Automation.Peers.ControlAutomationPeer;
using XamlAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.AutomationPeer;
using XamlPoint = global::Windows.Foundation.Point;
using AvPoint = global::Avalonia.Point;
using AvRect = global::Avalonia.Rect;

namespace Avalonia.WinUI.Automation;

internal sealed partial class AvaloniaSwapChainPanelAutomationPeer : FrameworkElementAutomationPeer
{
    private readonly AvaloniaSwapChainPanel _panel;
    private IEmbeddedRootProvider? _embeddedRoot;
    private AvPeer? _embeddedRootPeer;

    public AvaloniaSwapChainPanelAutomationPeer(AvaloniaSwapChainPanel panel) : base(panel)
    {
        _panel = panel;
    }

    internal AvaloniaSwapChainPanel Panel => _panel;

    protected override string GetClassNameCore() => nameof(AvaloniaSwapChainPanel);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    protected override IList<XamlAutomationPeer> GetChildrenCore()
    {
        var list = base.GetChildrenCore() ?? new List<XamlAutomationPeer>();

        if (TryGetEmbeddedRootPeer() is { } rootPeer)
        {
            var proxy = AvaloniaToXamlPeerProxy.GetOrCreate(rootPeer, this);
            proxy.SetParent(this);
            list.Add(proxy);
        }

        return list;
    }

    protected override object GetFocusedElementCore()
    {
        EnsureSubscribed();
        var focused = _embeddedRoot?.GetFocus();
        return focused is null
            ? base.GetFocusedElementCore()
            : AvaloniaToXamlPeerProxy.GetOrCreate(focused, this);
    }

    protected override object GetElementFromPointCore(XamlPoint point)
    {
        EnsureSubscribed();
        var hit = _embeddedRoot?.GetPeerFromPoint(new AvPoint(point.X, point.Y));
        return hit is null
            ? base.GetElementFromPointCore(point)
            : AvaloniaToXamlPeerProxy.GetOrCreate(hit, this);
    }

    /// <summary>
    /// Translate an Avalonia rectangle (in embedded-root coordinates) to screen pixels.
    /// </summary>
    internal (double x, double y, double width, double height) TransformAvaloniaRectToScreen(AvRect rect)
    {
        var transform = _panel.TransformToVisual(null);
        var topLeft = transform.TransformPoint(new XamlPoint(rect.X, rect.Y));
        var bottomRight = transform.TransformPoint(new XamlPoint(rect.Right, rect.Bottom));

        var xamlRoot = _panel.XamlRoot;
        var scale = xamlRoot?.RasterizationScale ?? 1.0;

        var x = topLeft.X * scale;
        var y = topLeft.Y * scale;
        var w = (bottomRight.X - topLeft.X) * scale;
        var h = (bottomRight.Y - topLeft.Y) * scale;

        if (xamlRoot?.ContentIslandEnvironment is { } island)
        {
            var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(island.AppWindowId);
            if (hwnd != IntPtr.Zero)
            {
                var pt = new POINT { x = 0, y = 0 };
                if (ClientToScreen(hwnd, ref pt))
                {
                    x += pt.x;
                    y += pt.y;
                }
            }
        }

        return (x, y, w, h);
    }

    private AvPeer? TryGetEmbeddedRootPeer()
    {
        EnsureSubscribed();
        return _embeddedRootPeer;
    }

    private void EnsureSubscribed()
    {
        if (_embeddedRootPeer is not null)
            return;

        var root = _panel.GetEmbeddedRootForAutomation();
        if (root is null)
            return;

        _embeddedRootPeer = AvControlPeer.CreatePeerForElement(root);
        _embeddedRoot = _embeddedRootPeer.GetProvider<IEmbeddedRootProvider>();

        if (_embeddedRoot is not null)
            _embeddedRoot.FocusChanged += OnEmbeddedFocusChanged;
    }

    private void OnEmbeddedFocusChanged(object? sender, EventArgs e)
    {
        if (!ListenerExists(AutomationEvents.AutomationFocusChanged))
            return;

        var focused = _embeddedRoot?.GetFocus();
        if (focused is null)
            return;

        var proxy = AvaloniaToXamlPeerProxy.GetOrCreate(focused, this);
        proxy.RaiseAutomationEvent(AutomationEvents.AutomationFocusChanged);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
}
