using System;
using System.Diagnostics;
using System.Threading;
using Avalonia.Native.Interop;
using Avalonia.Rendering;

namespace Avalonia.Native;

internal sealed class AvaloniaNativeRenderTimer : NativeCallbackBase, IRenderTimer, IAvnActionCallback
{
    private readonly IAvnPlatformRenderTimer _platformRenderTimer;
    private readonly StateChangedCallback _stateChangedCallback;
    private readonly Stopwatch _stopwatch;
    private readonly object _lock = new();
    private volatile Action<TimeSpan>? _tick;
    private Timer? _fallbackTimer;
    private bool _running;

    public AvaloniaNativeRenderTimer(IAvaloniaNativeFactory factory)
    {
        _stopwatch = Stopwatch.StartNew();
        _stateChangedCallback = new StateChangedCallback(this);
        _platformRenderTimer = factory.CreatePlatformRenderTimer(this, _stateChangedCallback);
    }

    public Action<TimeSpan>? Tick
    {
        get => _tick;
        set
        {
            if (value != null)
            {
                _tick = value;
                lock (_lock)
                {
                    _running = true;
                    _platformRenderTimer.Start();
                    UpdateFallbackTimer();
                }
            }
            else
            {
                lock (_lock)
                {
                    _running = false;
                    _platformRenderTimer.Stop();
                    UpdateFallbackTimer();
                }
                _tick = null;
            }
        }
    }

    public bool RunsInBackground => _platformRenderTimer.RunsInBackground().FromComBool();

    // Invoked both by the native CVDisplayLink and by the managed fallback timer.
    public void Run()
    {
        _tick?.Invoke(_stopwatch.Elapsed);
    }

    private void OnPlatformStateChanged()
    {
        lock (_lock)
            UpdateFallbackTimer();
    }

    // Must be called while holding _lock. Runs a software timer whenever CoreVideo cannot
    // provide a display link (e.g. during a wake-from-sleep reconfiguration race) and stops
    // it as soon as the native CVDisplayLink is available to drive ticks itself.
    private void UpdateFallbackTimer()
    {
        var needsFallback = _running && _platformRenderTimer.NeedsFallbackTimer != 0;
        if (needsFallback)
        {
            _fallbackTimer ??= new Timer(static state => ((AvaloniaNativeRenderTimer)state!).Run(),
                this, TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
        }
        else if (_fallbackTimer != null)
        {
            _fallbackTimer.Dispose();
            _fallbackTimer = null;
        }
    }

    private sealed class StateChangedCallback : NativeCallbackBase, IAvnActionCallback
    {
        private readonly AvaloniaNativeRenderTimer _owner;

        public StateChangedCallback(AvaloniaNativeRenderTimer owner) => _owner = owner;

        public void Run() => _owner.OnPlatformStateChanged();
    }
}
