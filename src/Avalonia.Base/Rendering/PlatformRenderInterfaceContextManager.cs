using System;
using System.Collections.Generic;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Reactive;

namespace Avalonia.Rendering;

internal class PlatformRenderInterfaceContextManager
{
    private readonly IPlatformGraphics? _graphics;
    private IPlatformRenderInterfaceContext? _backend;
    private OwnedDisposable<IPlatformGraphicsContext>? _gpuContext;
    public event Action? ContextDisposed;
    public event Action<IPlatformRenderInterfaceContext>? ContextCreated;

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
            if (_gpuContext != null)
            {
                _gpuContext?.Dispose();
                _gpuContext = null;
                ContextDisposed?.Invoke();
            }

            if (_graphics != null)
            {
                if (_graphics.UsesSharedContext)
                    _gpuContext = new OwnedDisposable<IPlatformGraphicsContext>(_graphics.GetSharedContext(), false);
                else
                    _gpuContext = new OwnedDisposable<IPlatformGraphicsContext>(_graphics.CreateContext(), true);
            }

            _backend = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>()
                .CreateBackendContext(_gpuContext?.Value);
            ContextCreated?.Invoke(_backend);
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
