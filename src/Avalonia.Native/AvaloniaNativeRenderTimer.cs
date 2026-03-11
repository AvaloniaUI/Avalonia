using System;
using System.Diagnostics;
using Avalonia.Native.Interop;
using Avalonia.Rendering;

namespace Avalonia.Native;

internal sealed class AvaloniaNativeRenderTimer : NativeCallbackBase, IRenderTimer, IAvnActionCallback
{
    private readonly IAvnPlatformRenderTimer _platformRenderTimer;
    private readonly Stopwatch _stopwatch;
    private bool _registered;

    public AvaloniaNativeRenderTimer(IAvnPlatformRenderTimer platformRenderTimer)
    {
        _platformRenderTimer = platformRenderTimer;
        _stopwatch = Stopwatch.StartNew();
    }

    public Action<TimeSpan>? Tick { get; set; }

    public bool RunsInBackground => _platformRenderTimer.RunsInBackground().FromComBool();

    public void Start()
    {
        EnsureRegistered();
        _platformRenderTimer.Start();
    }

    public void Stop()
    {
        _platformRenderTimer.Stop();
    }

    private void EnsureRegistered()
    {
        if (!_registered)
        {
            _registered = true;
            var registrationResult = _platformRenderTimer.RegisterTick(this);
            if (registrationResult != 0)
            {
                throw new InvalidOperationException(
                    $"Avalonia.Native was not able to start the RenderTimer. Native error code is: {registrationResult}");
            }
        }
    }

    public void Run()
    {
        Tick?.Invoke(_stopwatch.Elapsed);
    }
}
