using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server;

internal partial class ServerCompositor
{
    private IReadOnlyDictionary<Type, object>? _renderInterfaceFeatureCache;
    private readonly object _renderInterfaceFeaturesUserApiLock = new();

    void RT_OnContextCreated(IPlatformRenderInterfaceContext context)
    {
        lock (_renderInterfaceFeaturesUserApiLock)
        {
            _renderInterfaceFeatureCache = null;
            _renderInterfaceFeatureCache = context.PublicFeatures.ToDictionary(x => x.Key, x => x.Value);
        }
    }

    bool RT_OnContextLostExceptionFilterObserver(Exception e)
    {
        if (e is PlatformGraphicsContextLostException)
        {
            lock (_renderInterfaceFeaturesUserApiLock)
                _renderInterfaceFeatureCache = null;
        }
        return false;
    }
    
    void RT_OnContextDisposed()
    {
        lock (_renderInterfaceFeaturesUserApiLock)
            _renderInterfaceFeatureCache = null;
    }

    public IReadOnlyDictionary<Type, object>? AT_TryGetCachedRenderInterfaceFeatures()
    {
        lock (_renderInterfaceFeaturesUserApiLock)
            return _renderInterfaceFeatureCache;
    }
    
    public IReadOnlyDictionary<Type, object> RT_GetRenderInterfaceFeatures()
    {
        lock (_renderInterfaceFeaturesUserApiLock)
            return _renderInterfaceFeatureCache ??= RenderInterface.Value.PublicFeatures;
    }
}