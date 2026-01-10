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

    public IBitmapImpl CreateCompositionVisualSnapshot(ServerCompositionVisual visual,
        double scaling, bool renderChildren)
    {
        using (RenderInterface.EnsureCurrent())
        {
            var pixelSize = PixelSize.FromSize(new Size(visual.Size.X, visual.Size.Y), scaling);
            
            var scaleTransform = Matrix.CreateScale(scaling, scaling);
            var invertRootTransform = visual.CombinedTransformMatrix.Invert();

            IDrawingContextLayerImpl? target = null;
            try
            {
                target = RenderInterface.Value.CreateOffscreenRenderTarget(pixelSize, scaling);
                using (var canvas = target.CreateDrawingContext(false))
                {
                    var proxy = new CompositorDrawingContextProxy(canvas)
                    {
                        PostTransform = invertRootTransform * scaleTransform,
                        Transform = Matrix.Identity
                    };
                    var ctx = new ServerVisualRenderContext(proxy, null, true, renderChildren);
                    visual.Render(ctx, null);
                }

                if (target is IDrawingContextLayerWithRenderContextAffinityImpl affined
                    && affined.HasRenderContextAffinity)
                    return affined.CreateNonAffinedSnapshot();
                
                // We are returning the original target, so prevent it from being disposed
                var rv = target;
                target = null;
                return rv;
            }
            finally
            {
                target?.Dispose();
            }
        }
    }
}
