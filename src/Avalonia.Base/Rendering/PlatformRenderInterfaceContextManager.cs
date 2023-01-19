using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Rendering;

[Unstable]
// TODO: Make it internal once legacy renderers are removed
public class PlatformRenderInterfaceContextManager
{
    private readonly IPlatformGraphics? _graphics;
    private IPlatformRenderInterfaceContext? _backend;
    private OwnedDisposable<IPlatformGraphicsContext>? _gpuContext;

    public PlatformRenderInterfaceContextManager(IPlatformGraphics? graphics)
    {
        _graphics = graphics;
    }

    public void EnsureValidBackendContext()
    {
        if (_backend == null || _gpuContext?.Value.IsLost == true)
        {
            _backend?.Dispose();
            _backend = null;
            _gpuContext?.Dispose();
            _gpuContext = null;

            if (_graphics != null)
            {
                if (_graphics.UsesSharedContext)
                    _gpuContext = new OwnedDisposable<IPlatformGraphicsContext>(_graphics.GetSharedContext(), false);
                else
                    _gpuContext = new OwnedDisposable<IPlatformGraphicsContext>(_graphics.CreateContext(), true);
            }

            _backend = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>()
                .CreateBackendContext(_gpuContext?.Value);
        }
    }

    public IPlatformRenderInterfaceContext Value
    {
        get
        {
            EnsureValidBackendContext();
            return _backend!;
        }
    }

    internal IPlatformGraphicsContext? GpuContext => _gpuContext?.Value;

    public IDisposable EnsureCurrent()
    {
        EnsureValidBackendContext();
        if (_gpuContext.HasValue)
            return _gpuContext.Value.Value.EnsureCurrent();
        return Disposable.Empty;
    }
    
    public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
    {
        EnsureValidBackendContext();
        return _backend!.CreateRenderTarget(surfaces);
    }
}
