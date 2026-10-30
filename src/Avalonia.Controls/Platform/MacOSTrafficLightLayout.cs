using System;
using System.Collections.Generic;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.Controls.Platform;

/// <summary>
/// Places the native macOS window buttons of a window in its traffic light hosts.
/// </summary>
internal sealed class MacOSTrafficLightLayout
{
    private static readonly AttachedProperty<MacOSTrafficLightLayout?> LayoutProperty =
        AvaloniaProperty.RegisterAttached<MacOSTrafficLightLayout, Window, MacOSTrafficLightLayout?>("Layout");

    private readonly Window _window;
    private readonly IMacOSOptionsTopLevelImpl _impl;
    private readonly List<HostRegistration> _hosts = new();
    private Point? _position;

    private MacOSTrafficLightLayout(Window window, IMacOSOptionsTopLevelImpl impl)
    {
        _window = window;
        _impl = impl;
        window.SetValue(MacOSProperties.TrafficLightSizeProperty, impl.TrafficLightSize);
        impl.TrafficLightSizeChanged = TrafficLightSizeChanged;
        window.Closed += WindowClosed;
    }

    /// <summary>
    /// Connects a window to the platform's native window buttons.
    /// </summary>
    public static void Attach(Window window, IMacOSOptionsTopLevelImpl impl) =>
        window.SetValue(LayoutProperty, new MacOSTrafficLightLayout(window, impl));

    /// <summary>
    /// Starts or stops placing the native window buttons in a control.
    /// </summary>
    public static void SetIsHost(Control control, bool isHost)
    {
        if (isHost)
        {
            control.AttachedToVisualTree += HostAttached;
            FindLayout(control)?.AddHost(control);
        }
        else
        {
            control.AttachedToVisualTree -= HostAttached;
            FindLayout(control)?.RemoveHost(control);
        }
    }

    private static MacOSTrafficLightLayout? FindLayout(Control control) =>
        TopLevel.GetTopLevel(control) is Window window ? window.GetValue(LayoutProperty) : null;

    private static void HostAttached(object? sender, VisualTreeAttachmentEventArgs args) =>
        FindLayout((Control)sender!)?.AddHost((Control)sender!);

    private void AddHost(Control host)
    {
        if (_hosts.Exists(registration => registration.Host == host))
            return;

        // At style priority, whether a style setter or this binding wins would depend on when styles are applied.
        // Template priority makes it deterministic: local values replace the minimum size, style setters don't.
        IDisposable BindToTrafficLightSize(StyledProperty<double> property, Func<Size, double> selector) =>
            host.Bind(
                property,
                _window.GetObservable(MacOSProperties.TrafficLightSizeProperty, selector),
                BindingPriority.Template);

        _hosts.Add(new HostRegistration(
            host,
            BindToTrafficLightSize(Layoutable.MinWidthProperty, size => size.Width),
            BindToTrafficLightSize(Layoutable.MinHeightProperty, size => size.Height)));
        host.DetachedFromVisualTree += HostDetached;

        if (_hosts.Count == 1)
            _window.LayoutUpdated += WindowLayoutUpdated;

        Update();
    }

    private void RemoveHost(Control host)
    {
        var index = _hosts.FindIndex(registration => registration.Host == host);
        if (index < 0)
            return;

        _hosts[index].Dispose();
        _hosts.RemoveAt(index);
        host.DetachedFromVisualTree -= HostDetached;

        if (_hosts.Count == 0)
            _window.LayoutUpdated -= WindowLayoutUpdated;

        Update();
    }

    private void Update()
    {
        var position = CalculatePosition();
        if (position == _position)
            return;

        _position = position;
        _impl.SetTrafficLightPosition(position);
    }

    private Point? CalculatePosition()
    {
        var size = MacOSProperties.GetTrafficLightSize(_window);
        if (size.Width <= 0 || size.Height <= 0)
            return null;

        foreach (var registration in _hosts)
        {
            if (!TryGetHostBounds(registration.Host, out var bounds))
                continue;

            // Host bounds are physical, but the native position is measured from the title bar's leading edge.
            var leading = _impl.TitlebarFlowDirection == FlowDirection.RightToLeft
                ? _window.Bounds.Width - bounds.Right
                : bounds.X;

            // Buttons keep their native size, so a host smaller than the group aligns them to its top-leading corner.
            return new Point(
                leading + Math.Max(0, (bounds.Width - size.Width) / 2),
                bounds.Y + Math.Max(0, (bounds.Height - size.Height) / 2));
        }

        return null;
    }

    private bool TryGetHostBounds(Control host, out Rect bounds)
    {
        if (!host.IsEffectivelyVisible || !host.IsArrangeValid || host.TransformToVisual(_window) is not { } transform)
        {
            bounds = default;
            return false;
        }

        bounds = new Rect(host.Bounds.Size).TransformToAABB(transform);
        return true;
    }

    private void TrafficLightSizeChanged(Size size)
    {
        _window.SetValue(MacOSProperties.TrafficLightSizeProperty, size);
        Update();
    }

    private void WindowLayoutUpdated(object? sender, EventArgs args) => Update();

    private void HostDetached(object? sender, VisualTreeAttachmentEventArgs args) => RemoveHost((Control)sender!);

    private void WindowClosed(object? sender, EventArgs args)
    {
        // The native window is gone, so the hosts are released without restoring the system layout.
        foreach (var registration in _hosts)
        {
            registration.Dispose();
            registration.Host.DetachedFromVisualTree -= HostDetached;
        }

        _hosts.Clear();
        _window.LayoutUpdated -= WindowLayoutUpdated;
        _window.Closed -= WindowClosed;
        _impl.TrafficLightSizeChanged = null;
    }

    private sealed record HostRegistration(Control Host, IDisposable MinWidth, IDisposable MinHeight) : IDisposable
    {
        public void Dispose()
        {
            MinWidth.Dispose();
            MinHeight.Dispose();
        }
    }
}
