using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Reactive;

namespace Avalonia.Controls;

internal partial class TopLevelHost
{
    
    /// <summary>
    /// Wrapper that holds a single visual child, used to host decoration layer content
    /// extracted from the decorations template.
    /// </summary>
    private class LayerWrapper : Control
    {
        public Control? Inner
        {
            get => field;
            set
            {
                if (field == value)
                    return;
                if (field != null)
                    VisualChildren.Remove(field);
                field = value;
                if (field != null)
                    VisualChildren.Add(field);
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
            => new LayerWrapperAutomationPeer(this);

        /// <summary>
        /// Returns no children so that EnsureConnected on content peers
        /// doesn't override their parent set by WindowAutomationPeer.
        /// </summary>
        private class LayerWrapperAutomationPeer(LayerWrapper owner) : ControlAutomationPeer(owner)
        {
            protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
        }
    }

    private readonly TopLevel _topLevel;
    private WindowDrawnDecorations? _decorations;
    private LayerWrapper? _underlay;
    private LayerWrapper? _overlay;
    private LayerWrapper? _fullscreenPopover;
    private ResizeGripLayer? _resizeGrips;
    private IDisposable? _decorationsSubscriptions;
    
    /// <summary>
    /// Gets the current drawn decorations instance, if active.
    /// </summary>
    internal WindowDrawnDecorations? Decorations => _decorations;

    /// <summary>
    /// Enables drawn window decorations with the specified parts.
    /// Creates the decorations instance, applies the template, and inserts layers into the visual tree.
    /// </summary>
    internal void EnableDecorations(DrawnWindowDecorationParts parts)
    {
        if (_decorations != null)
        {
            _decorations.EnabledParts = parts;
            return;
        }

        _decorations = new WindowDrawnDecorations();
        _decorations.EnabledParts = parts;

        // Set up logical parenting
        ((ISetLogicalParent)_decorations).SetParent(this);
        LogicalChildren.Add(_decorations);

        // Create layer wrappers
        _underlay = new LayerWrapper();
        _overlay = new LayerWrapper();
        _fullscreenPopover = new LayerWrapper { IsVisible = false };

        // Insert layers: underlay below TopLevel, overlay and popover above
        // Visual order: underlay(0), TopLevel(1), overlay(2), fullscreenPopover(3), resizeGrips(4)
        VisualChildren.Insert(0, _underlay);
        VisualChildren.Add(_overlay);
        VisualChildren.Add(_fullscreenPopover);

        // Add resize grips as topmost layer
        if (parts.HasFlag(DrawnWindowDecorationParts.ResizeGrips))
        {
            _resizeGrips = new ResizeGripLayer();
            VisualChildren.Add(_resizeGrips);
        }

        // Attach to window if available
        if (_topLevel is Window window)
            _decorations.Attach(window);

        // Subscribe to template changes to re-apply and geometry changes for resize grips
        _decorations.EffectiveGeometryChanged += OnDecorationsGeometryChanged;
        _decorationsSubscriptions = _decorations.GetObservable(WindowDrawnDecorations.TemplateProperty)
            .Subscribe(_ => ApplyDecorationsTemplate());

        ApplyDecorationsTemplate();
    }

    /// <summary>
    /// Disables drawn window decorations and removes all layers.
    /// </summary>
    internal void DisableDecorations()
    {
        if (_decorations == null)
            return;

        _decorationsSubscriptions?.Dispose();
        _decorationsSubscriptions = null;

        _decorations.EffectiveGeometryChanged -= OnDecorationsGeometryChanged;
        _decorations.Detach();

        // Remove layers
        if (_underlay != null)
        {
            VisualChildren.Remove(_underlay);
            _underlay = null;
        }
        if (_overlay != null)
        {
            VisualChildren.Remove(_overlay);
            _overlay = null;
        }
        if (_fullscreenPopover != null)
        {
            VisualChildren.Remove(_fullscreenPopover);
            _fullscreenPopover = null;
        }
        if (_resizeGrips != null)
        {
            VisualChildren.Remove(_resizeGrips);
            _resizeGrips = null;
        }

        // Clean up logical tree
        LogicalChildren.Remove(_decorations);
        ((ISetLogicalParent)_decorations).SetParent(null);
        _decorations = null;
    }

    private void ApplyDecorationsTemplate()
    {
        if (_decorations == null)
            return;

        _decorations.ApplyTemplate();

        var content = _decorations.Content;
        if (_underlay != null)
            _underlay.Inner = content?.Underlay;
        if (_overlay != null)
            _overlay.Inner = content?.Overlay;
        if (_fullscreenPopover != null)
            _fullscreenPopover.Inner = content?.FullscreenPopover;

        UpdateResizeGripThickness();
    }

    private void UpdateResizeGripThickness()
    {
        if (_resizeGrips == null || _decorations == null)
            return;

        var frame = _decorations.FrameThickness;
        var shadow = _decorations.ShadowThickness;
        // Grips strictly cover frame + shadow area, never client area
        _resizeGrips.GripThickness = new Thickness(
            frame.Left + shadow.Left,
            frame.Top + shadow.Top,
            frame.Right + shadow.Right,
            frame.Bottom + shadow.Bottom);
    }

    internal void UpdateResizeGrips()
    {
        UpdateResizeGripThickness();
    }

    private void OnDecorationsGeometryChanged()
    {
        UpdateResizeGripThickness();

        // Notify Window to update margins
        if (_topLevel is Window window)
            window.OnDrawnDecorationsGeometryChanged();
    }

    /// <summary>
    /// Shows or hides the fullscreen popover based on the window state.
    /// Called by Window when window state changes.
    /// </summary>
    internal void SetFullscreenPopoverEnabled(bool enabled)
    {
        if (_fullscreenPopover == null)
            return;

        if (enabled)
        {
            // In fullscreen mode, hide overlay and underlay, enable popover hover detection
            if (_overlay != null)
                _overlay.IsVisible = false;
            if (_underlay != null)
                _underlay.IsVisible = false;
            // Popover starts hidden, will show on hover at top edge
            _fullscreenPopover.IsVisible = false;
            _fullscreenPopoverEnabled = true;
        }
        else
        {
            // Not fullscreen: show overlay and underlay, hide popover
            if (_overlay != null)
                _overlay.IsVisible = true;
            if (_underlay != null)
                _underlay.IsVisible = true;
            _fullscreenPopover.IsVisible = false;
            _fullscreenPopoverEnabled = false;
        }
    }

    private bool _fullscreenPopoverEnabled;
    private const double PopoverTriggerZoneHeight = 1;

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_fullscreenPopoverEnabled && _fullscreenPopover != null)
        {
            var pos = e.GetPosition(this);
            // Use DefaultTitleBarHeight since TitleBarHeight is 0 in fullscreen
            var titleBarHeight = _decorations?.DefaultTitleBarHeight ?? 30;

            if (!_fullscreenPopover.IsVisible && pos.Y <= PopoverTriggerZoneHeight)
            {
                _fullscreenPopover.IsVisible = true;
            }
            else if (pos.Y > titleBarHeight && _fullscreenPopover.IsVisible)
            {
                _fullscreenPopover.IsVisible = false;
            }
        }
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        // Don't hide popover on PointerExited — it's hidden via the Y threshold
        // check in OnPointerMoved. PointerExited can fire spuriously at window edges
        // when the popover becomes visible (layout change re-routes the pointer),
        // causing a show/hide feedback loop.
    }

}