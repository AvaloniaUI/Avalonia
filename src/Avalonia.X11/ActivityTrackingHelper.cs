using System;
using System.Linq;
using Avalonia.Threading;

namespace Avalonia.X11;

internal class WindowActivationTrackingHelper : IDisposable
{
    private readonly AvaloniaX11Platform _platform;
    private readonly X11Window _window;
    private bool _active;

    // Set when the current active state came from a speculative transfer (see SetActiveSpeculatively).
    // Only meaningful in _NET_WM_STATE_FOCUSED mode, where the guess is re-verified against the real
    // _NET_WM_STATE the next time the active window changes.
    private bool _speculative;

    public event Action<bool>? ActivationChanged;

    public WindowActivationTrackingHelper(AvaloniaX11Platform platform, X11Window window)
    {
        _platform = platform;
        _window = window;
        _platform.ActiveWindowTracker.ActiveWindowChanged += OnActiveWindowChanged;
        _platform.Globals.WindowActivationTrackingModeChanged += OnWindowActivationTrackingModeChanged;
    }

    public bool IsActive => _active;

    void SetActive(bool active)
    {
        if (active != _active)
        {
            _active = active;
            ActivationChanged?.Invoke(active);
        }
    }

    private X11Globals.WindowActivationTrackingMode Mode => _platform.Globals.ActivationTrackingMode;

    void RequeryNetWmState() =>
        OnNetWmStateChanged(XLib.XGetWindowPropertyAsIntPtrArray(_platform.Display, _window.Handle.Handle,
            _platform.Info.Atoms._NET_WM_STATE, _platform.Info.Atoms.ATOM) ?? []);

    void RequeryActivation()
    {
        // The _NET_ACTIVE_WINDOW mode is driven by the shared X11ActiveWindowTracker, so only the
        // per-window _NET_WM_STATE needs re-reading here.
        if (Mode == X11Globals.WindowActivationTrackingMode._NET_WM_STATE_FOCUSED)
            RequeryNetWmState();
    }

    private void OnWindowActivationTrackingModeChanged() =>
        DispatcherTimer.RunOnce(RequeryActivation, TimeSpan.FromSeconds(1), DispatcherPriority.Input);

    public void OnEvent(ref XEvent ev)
    {
        if (ev.type is not XEventName.FocusIn and not XEventName.FocusOut)
            return;
        
        // Always attempt to activate transient children on focus events
        if (ev.type == XEventName.FocusIn && _window.ActivateTransientChildIfNeeded()) return;

        if (Mode != X11Globals.WindowActivationTrackingMode.FocusEvents)
            return;
        
        // See: https://github.com/fltk/fltk/issues/295
        if ((NotifyMode)ev.FocusChangeEvent.mode is not NotifyMode.NotifyNormal)
            return;
        
        SetActive(ev.type == XEventName.FocusIn);
    }

    private void OnActiveWindowChanged(IntPtr activeWindow)
    {
        if (Mode == X11Globals.WindowActivationTrackingMode._NET_ACTIVE_WINDOW)
        {
            // Authoritative in this mode — the published XID is the active window.
            _speculative = false;
            SetActive(activeWindow == _window.Handle.Handle);
        }
        // In _NET_WM_STATE_FOCUSED mode the root _NET_ACTIVE_WINDOW change is only a "focus moved"
        // pulse; use it to re-verify a speculative activation against the authoritative _NET_WM_STATE.
        else if (Mode == X11Globals.WindowActivationTrackingMode._NET_WM_STATE_FOCUSED && _speculative)
            RequeryNetWmState();
    }

    /// <summary>
    /// Marks the window as active without waiting for the real activation notification. Used when
    /// handing activation back to a dialog owner. The next active window change re-verifies the guess
    /// (see <see cref="OnActiveWindowChanged"/>): authoritatively via the published XID in
    /// _NET_ACTIVE_WINDOW mode, or by re-reading our own _NET_WM_STATE in _NET_WM_STATE_FOCUSED mode.
    /// </summary>
    public void SetActiveSpeculatively()
    {
        _speculative = true;
        SetActive(true);
    }

    public void Dispose()
    {
        _platform.ActiveWindowTracker.ActiveWindowChanged -= OnActiveWindowChanged;
        _platform.Globals.WindowActivationTrackingModeChanged -= OnWindowActivationTrackingModeChanged;
    }

    public void OnNetWmStateChanged(IntPtr[] atoms)
    {
        if (Mode == X11Globals.WindowActivationTrackingMode._NET_WM_STATE_FOCUSED)
        {
            // Authoritative signal — it overrides any speculative guess.
            _speculative = false;
            SetActive(atoms.Contains(_platform.Info.Atoms._NET_WM_STATE_FOCUSED));
        }
    }
}
