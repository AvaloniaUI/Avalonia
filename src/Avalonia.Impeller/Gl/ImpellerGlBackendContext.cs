using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using NImpeller;

namespace Avalonia.Impeller;

/// <summary>An Impeller context over an OpenGL ES context.</summary>
internal sealed class ImpellerGlBackendContext : ImpellerBackendContext
{
    private ImpellerGlBackendContext(IGlContext glContext, ImpellerContext impellerContext)
        : base(impellerContext)
    {
        GlContext = glContext;
        PublicFeatures = new Dictionary<Type, object>
        {
            [typeof(IOpenGlTextureSharingRenderInterfaceContextFeature)] = new ImpellerTextureSharingFeature(this),
            [typeof(IExternalObjectsRenderInterfaceContextFeature)] = new ImpellerExternalObjectsFeature(this),
        };
    }

    public static ImpellerGlBackendContext Create(IGlContext glContext)
    {
        ImpellerContext impellerContext;
        using (glContext.EnsureCurrent())
        {
            impellerContext = ImpellerContext.CreateOpenGLESNew(glContext.GlInterface.GetProcAddress)
                ?? throw new PlatformNotSupportedException("Failed to create an Impeller OpenGL ES context.");
        }

        return new ImpellerGlBackendContext(glContext, impellerContext);
    }

    internal IGlContext GlContext { get; }

    internal override IImpellerOffscreenLayer CreateTextureLayer(PixelSize size, Vector dpi) =>
        new ImpellerGlRenderLayer(this, size, dpi);

    public override IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
    {
        foreach (var surface in surfaces)
        {
            if (surface is IGlPlatformSurface glSurface)
                return new ImpellerGlRenderTarget(this, glSurface);
        }

        throw new NotSupportedException("The Impeller OpenGL backend needs an IGlPlatformSurface.");
    }

    public override bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces) =>
        surfaces.Any(s => s is IGlPlatformSurface { IsReady: true });

    public override bool IsLost => GlContext.IsLost;

    public override IReadOnlyDictionary<Type, object> PublicFeatures { get; }
}
