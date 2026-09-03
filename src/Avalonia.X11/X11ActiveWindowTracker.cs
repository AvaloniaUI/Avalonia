using System;
using System.Linq;
using Avalonia.Threading;

namespace Avalonia.X11;

/// <summary>
/// Watches the root <c>_NET_ACTIVE_WINDOW</c> property and raises <see cref="ActiveWindowChanged"/>
/// whenever the active window changes. This is a single, always-updated global notification that
/// <c>_NET_WM_STATE_FOCUSED</c> mode lacks (that one only notifies the window whose own state changed),
/// so windows use it both to track activation (in <c>_NET_ACTIVE_WINDOW</c> mode) and to re-verify a
/// speculative activation (in <c>_NET_WM_STATE_FOCUSED</c> mode).
/// </summary>
internal class X11ActiveWindowTracker
{
    private readonly AvaloniaX11Platform _platform;
    private IntPtr _activeWindow;

    public event Action<IntPtr>? ActiveWindowChanged;

    public X11ActiveWindowTracker(AvaloniaX11Platform platform)
    {
        _platform = platform;
        _platform.Globals.NetActiveWindowPropertyChanged += Requery;
        _platform.Globals.WindowActivationTrackingModeChanged += OnModeChanged;
        _platform.Globals.NetSupportedChanged += Requery;
        Requery();
    }

    // Whether the WM publishes root _NET_ACTIVE_WINDOW at all. Speculative activations can only be
    // auto-corrected when it does, since that's the notification we re-verify against.
    public bool TracksRootActiveWindow =>
        _platform.Globals.NetSupported?.Contains(_platform.Info.Atoms._NET_ACTIVE_WINDOW) ?? false;

    private void SetActiveWindow(IntPtr window)
    {
        if (_activeWindow != window)
        {
            _activeWindow = window;
            ActiveWindowChanged?.Invoke(window);
        }
    }

    private void Requery()
    {
        if (!TracksRootActiveWindow)
            return;
        var value = XLib.XGetWindowPropertyAsIntPtrArray(_platform.Info.Display, _platform.Info.RootWindow,
            _platform.Info.Atoms._NET_ACTIVE_WINDOW, (IntPtr)_platform.Info.Atoms.WINDOW);
        SetActiveWindow(value is { Length: > 0 } ? value[0] : IntPtr.Zero);
    }

    private void OnModeChanged() =>
        DispatcherTimer.RunOnce(Requery, TimeSpan.FromSeconds(1), DispatcherPriority.Input);
}
