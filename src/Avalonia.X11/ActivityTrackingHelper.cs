using System;
using System.Linq;
using Avalonia.Threading;

namespace Avalonia.X11;

internal class WindowActivationTrackingHelper : IDisposable
{
    private readonly AvaloniaX11Platform _platform;
    private readonly X11Window _window;
    private bool _active;

    public event Action<bool>? ActivationChanged;
    
    public WindowActivationTrackingHelper(AvaloniaX11Platform platform, X11Window window)
    {
        _platform = platform;
        _window = window;
        _platform.Globals.NetActiveWindowPropertyChanged += OnNetActiveWindowChanged;
        _platform.Globals.WindowActivationTrackingModeChanged += OnWindowActivationTrackingModeChanged;
    }

    void SetActive(bool active)
    {
        if (active != _active)
        {
            _active = active;
            ActivationChanged?.Invoke(active);
        }
    }

    void RequeryActivation()
    {
        // Update the active state from WM-set properties
        
        if (Mode == X11Globals.WindowActivationTrackingMode._NET_ACTIVE_WINDOW) 
            OnNetActiveWindowChanged();
        
        if (Mode == X11Globals.WindowActivationTrackingMode._NET_WM_STATE_FOCUSED)
            OnNetWmStateChanged(XLib.XGetWindowPropertyAsIntPtrArray(_platform.Display, _window.Handle.Handle,
                _platform.Info.Atoms._NET_WM_STATE, _platform.Info.Atoms.XA_ATOM) ?? []);
    }

    private void OnWindowActivationTrackingModeChanged() =>
        DispatcherTimer.RunOnce(RequeryActivation, TimeSpan.FromSeconds(1), DispatcherPriority.Input);

    private X11Globals.WindowActivationTrackingMode Mode => _platform.Globals.ActivationTrackingMode;
    
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
    
    private void OnNetActiveWindowChanged()
    {
        if (Mode == X11Globals.WindowActivationTrackingMode._NET_ACTIVE_WINDOW)
        {
            var value = XLib.XGetWindowPropertyAsIntPtrArray(_platform.Display, _platform.Info.RootWindow,
                _platform.Info.Atoms._NET_ACTIVE_WINDOW,
                (IntPtr)_platform.Info.Atoms.XA_WINDOW);
            if (value == null || value.Length == 0)
                SetActive(false);
            else
                SetActive(value[0] == _window.Handle.Handle);
        }
    }

    public void Dispose()
    {
        _platform.Globals.NetActiveWindowPropertyChanged -= OnNetActiveWindowChanged;
        _platform.Globals.WindowActivationTrackingModeChanged -= OnWindowActivationTrackingModeChanged;
    }

    public void OnNetWmStateChanged(IntPtr[] atoms)
    {
        if (Mode == X11Globals.WindowActivationTrackingMode._NET_WM_STATE_FOCUSED)
            SetActive(atoms.Contains(_platform.Info.Atoms._NET_WM_STATE_FOCUSED));
    }
}
