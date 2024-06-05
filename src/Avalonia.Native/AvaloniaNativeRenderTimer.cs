using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Avalonia.Native.Interop;
using Avalonia.Rendering;
#nullable enable

namespace Avalonia.Native;

internal sealed class AvaloniaNativeRenderTimer : NativeCallbackBase, IRenderTimer, IAvnActionCallback
{
    private readonly IAvnPlatformRenderTimer _platformRenderTimer;
    private readonly Stopwatch _stopwatch;
    private Action<TimeSpan>? _tick;
    private int _subscriberCount;
    private bool registered;

    public AvaloniaNativeRenderTimer(IAvnPlatformRenderTimer platformRenderTimer)
    {
        _platformRenderTimer = platformRenderTimer;
        _stopwatch = Stopwatch.StartNew();
    }

    public event Action<TimeSpan> Tick
    {
        add
        {
            _tick += value;

            if (!registered)
            {
                registered = true;
                var registrationResult = _platformRenderTimer.RegisterTick(this);
                if (registrationResult != 0)
                {
                    throw new InvalidOperationException(
                        $"Avalonia.Native was not able to start the RenderTimer. Native error code is: {registrationResult}");
                }
            }

            if (_subscriberCount++ == 0)
            {
                _platformRenderTimer.Start();
            }
        }

        remove
        {
            if (--_subscriberCount == 0)
            {
                _platformRenderTimer.Stop();
            }

            _tick -= value;
        }
    }

    public bool RunsInBackground => _platformRenderTimer.RunsInBackground().FromComBool();

    public void Run()
    {
        _tick?.Invoke(_stopwatch.Elapsed);
    }
}
