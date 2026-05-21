using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using global::Avalonia.Controls.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using AvPeer = global::Avalonia.Automation.Peers.AutomationPeer;
using XamlAutomationPeer = Microsoft.UI.Xaml.Automation.Peers.AutomationPeer;
using XamlRect = global::Windows.Foundation.Rect;

namespace Avalonia.WinUI.Automation;

/// <summary>
/// Wraps an Avalonia <see cref="AvPeer"/> so it can be exposed to Microsoft UI Automation
/// through WinUI XAML's automation infrastructure. Mirrors the role of
/// <c>Avalonia.Win32.Automation.AutomationNode</c> but for WinUI hosts.
/// </summary>
/// <remarks>
/// WinUI XAML <see cref="XamlAutomationPeer"/> instances are strictly UI-thread bound:
/// calling an overridden <c>*Core</c> method from a worker thread throws
/// <c>RPC_E_WRONG_THREAD</c> at the WinRT ABI boundary. UIA itself marshals into the
/// UI thread (via the WinUI <c>DispatcherQueue</c>) before invoking us, so we can call
/// the wrapped Avalonia peer directly. No explicit dispatcher marshalling is required.
/// </remarks>
internal sealed partial class AvaloniaToXamlPeerProxy : XamlAutomationPeer
{
    private static readonly ConditionalWeakTable<AvPeer, AvaloniaToXamlPeerProxy> s_cache = new();
    private static long s_nextId;

    private readonly AvaloniaSwapChainPanelAutomationPeer _host;
    private readonly AvPeer _peer;
    private readonly bool _isEmbeddedRoot;
    private readonly string _uniqueId;

    private AvaloniaToXamlPeerProxy(AvPeer peer, AvaloniaSwapChainPanelAutomationPeer host)
    {
        _peer = peer;
        _host = host;
        _isEmbeddedRoot = peer is EmbeddableControlRootAutomationPeer;
        _uniqueId = "Avalonia.WinUI.Peer:" + System.Threading.Interlocked.Increment(ref s_nextId).ToString(
            System.Globalization.CultureInfo.InvariantCulture);

        peer.ChildrenChanged += OnPeerChildrenChanged;
    }

    internal AvPeer Peer => _peer;

    public static AvaloniaToXamlPeerProxy GetOrCreate(AvPeer peer, AvaloniaSwapChainPanelAutomationPeer host)
    {
        return s_cache.GetValue(peer, p => new AvaloniaToXamlPeerProxy(p, host));
    }

    internal static AvaloniaToXamlPeerProxy? TryGet(AvPeer? peer)
    {
        if (peer is null)
            return null;
        return s_cache.TryGetValue(peer, out var proxy) ? proxy : null;
    }

    protected override IList<XamlAutomationPeer> GetChildrenCore()
    {
        var list = new List<XamlAutomationPeer>();
        var children = _peer.GetChildren();
        if (children is null)
            return list;

        foreach (var child in children)
        {
            var childProxy = GetOrCreate(child, _host);
            // Peers not tied to a UIElement must declare their parent explicitly;
            // without it UIA can't connect them into the tree and silently prunes
            // them. (Microsoft.UI.Xaml.Automation.Peers.AutomationPeer.SetParent.)
            childProxy.SetParent(this);
            list.Add(childProxy);
        }

        return list;
    }

    protected override string GetClassNameCore() => _peer.GetClassName() ?? string.Empty;

    protected override string GetNameCore() => _peer.GetName() ?? string.Empty;

    protected override string GetAutomationIdCore() => _peer.GetAutomationId() ?? string.Empty;

    protected override string GetHelpTextCore() => _peer.GetHelpText() ?? string.Empty;

    protected override string GetAcceleratorKeyCore() => _peer.GetAcceleratorKey() ?? string.Empty;

    protected override string GetAccessKeyCore() => _peer.GetAccessKey() ?? string.Empty;

    protected override string GetItemStatusCore() => _peer.GetItemStatus() ?? string.Empty;

    protected override string GetItemTypeCore() => _peer.GetItemType() ?? string.Empty;

    protected override string GetLocalizedControlTypeCore()
        => _peer.GetLocalizedControlType() ?? base.GetLocalizedControlTypeCore();

    protected override AutomationControlType GetAutomationControlTypeCore()
        => ControlTypeMap.ToXaml(_peer.GetAutomationControlType());

    protected override XamlRect GetBoundingRectangleCore()
    {
        var rect = _peer.GetBoundingRectangle();
        if (rect.Width <= 0 && rect.Height <= 0)
            return default;

        var (screenX, screenY, w, h) = _host.TransformAvaloniaRectToScreen(rect);
        return new XamlRect(screenX, screenY, w, h);
    }

    protected override bool HasKeyboardFocusCore() => _peer.HasKeyboardFocus();

    protected override bool IsKeyboardFocusableCore() => _peer.IsKeyboardFocusable();

    protected override bool IsEnabledCore() => _peer.IsEnabled();

    // Collapse the redundant Pane → Pane: the embedded root sits beneath the
    // panel's own peer and adds nothing to the screen reader's view.
    protected override bool IsContentElementCore() => !_isEmbeddedRoot && _peer.IsContentElement();

    protected override bool IsControlElementCore() => !_isEmbeddedRoot && _peer.IsControlElement();

    protected override bool IsOffscreenCore() => _peer.IsOffscreen();

    protected override void SetFocusCore() => _peer.SetFocus();

    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.Invoke
                when _peer.GetProvider<global::Avalonia.Automation.Provider.IInvokeProvider>() is not null => this,
            PatternInterface.Toggle
                when _peer.GetProvider<global::Avalonia.Automation.Provider.IToggleProvider>() is not null => this,
            PatternInterface.Value
                when _peer.GetProvider<global::Avalonia.Automation.Provider.IValueProvider>() is not null => this,
            PatternInterface.RangeValue
                when _peer.GetProvider<global::Avalonia.Automation.Provider.IRangeValueProvider>() is not null => this,
            PatternInterface.Scroll
                when _peer.GetProvider<global::Avalonia.Automation.Provider.IScrollProvider>() is not null => this,
            // Every peer supports BringIntoView, so ScrollItem is always available.
            PatternInterface.ScrollItem => this,
            PatternInterface.Selection
                when _peer.GetProvider<global::Avalonia.Automation.Provider.ISelectionProvider>() is not null => this,
            PatternInterface.SelectionItem
                when _peer.GetProvider<global::Avalonia.Automation.Provider.ISelectionItemProvider>() is not null => this,
            PatternInterface.ExpandCollapse
                when _peer.GetProvider<global::Avalonia.Automation.Provider.IExpandCollapseProvider>() is not null => this,
            _ => null!,
        };
    }

    private void OnPeerChildrenChanged(object? sender, EventArgs e)
    {
        if (ListenerExists(AutomationEvents.StructureChanged))
            RaiseAutomationEvent(AutomationEvents.StructureChanged);
    }
}
