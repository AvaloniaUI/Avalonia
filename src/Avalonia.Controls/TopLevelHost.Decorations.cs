using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Reactive;
using Avalonia.Styling;

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
    /// Updates drawn window decorations with the specified parts and window state.
    /// When <paramref name="parts"/> is <c>null</c>, decorations are removed entirely.
    /// When non-null (including <see cref="DrawnWindowDecorationParts.None"/>), the decoration
    /// infrastructure is kept alive and parts/fullscreen state are updated.
    /// </summary>
    internal void UpdateDrawnDecorations(DrawnWindowDecorationParts? parts, WindowState windowState, ControlTheme? theme)
    {
        if (parts == null)
        {
            RemoveDecorations();
            return;
        }

        var enabledParts = parts.Value;

        if (_decorations != null)
        {
            // Layers persist across part changes; pseudo-classes driven by EnabledParts
            // control visibility of individual decoration elements in the theme.
            _decorations.EnabledParts = enabledParts;

            var oldTheme = _decorations.Theme;
            if (oldTheme != theme)
            {
                _decorations.Theme = theme;
                _decorations.ApplyStyling();
            }

            if (_resizeGrips != null)
                _resizeGrips.IsVisible = enabledParts.HasFlag(DrawnWindowDecorationParts.ResizeGrips);
        }
        else
        {
            _decorations = new WindowDrawnDecorations();
            _decorations.EnabledParts = enabledParts;
            _decorations.Theme = theme;

            // Set up logical parenting
            LogicalChildren.Add(_decorations);

            // Create layer wrappers
            _underlay = new LayerWrapper() { [AutomationProperties.AutomationIdProperty] = "WindowChromeUnderlay" };
            _overlay = new LayerWrapper()  { [AutomationProperties.AutomationIdProperty] = "WindowChromeOverlay" };
            _fullscreenPopover = new LayerWrapper()
            {
                IsVisible = false, [AutomationProperties.AutomationIdProperty] = "PopoverWindowChrome"
            };

            // Insert layers: underlay below TopLevel, overlay and popover above
            // Visual order: underlay(0), TopLevel(1), overlay(2), fullscreenPopover(3), resizeGrips(4)
            VisualChildren.Insert(0, _underlay);
            VisualChildren.Add(_overlay);
            VisualChildren.Add(_fullscreenPopover);

            // Always create resize grips; visibility is controlled by EnabledParts
            _resizeGrips = new ResizeGripLayer();
            _resizeGrips.IsVisible = enabledParts.HasFlag(DrawnWindowDecorationParts.ResizeGrips);
            VisualChildren.Add(_resizeGrips);

            // Attach to window if available
            if (_topLevel is Window window)
                _decorations.Attach(window);

            // Subscribe to template changes to re-apply and geometry changes for resize grips
            _decorations.EffectiveGeometryChanged += OnDecorationsGeometryChanged;
            _decorationsSubscriptions = _decorations.GetObservable(WindowDrawnDecorations.TemplateProperty)
                .Subscribe(_ => ApplyDecorationsTemplate());

            ApplyDecorationsTemplate();
            InvalidateMeasure();
        }

        ApplyFullscreenState(windowState == WindowState.FullScreen);
    }

    /// <summary>
    /// Removes drawn window decorations and all associated layers.
    /// </summary>
    private void RemoveDecorations()
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
        _decorations = null;
        _decorationsOverlayPeer?.InvalidateChildren();
    }

    private void ApplyDecorationsTemplate()
    {
        if (_decorations == null)
            return;

        _decorations.ApplyStyling();
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
    /// Applies fullscreen-specific layer visibility: hides overlay/underlay and enables
    /// popover hover detection, or restores normal state.
    /// </summary>
    private void ApplyFullscreenState(bool isFullscreen)
    {
        if (_fullscreenPopover == null)
            return;

        if (isFullscreen)
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
        _decorationsOverlayPeer?.InvalidateChildren();
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
                _decorationsOverlayPeer?.InvalidateChildren();
            }
            else if (pos.Y > titleBarHeight && _fullscreenPopover.IsVisible)
            {
                _fullscreenPopover.IsVisible = false;
                _decorationsOverlayPeer?.InvalidateChildren();
            }
        }
    }
}
