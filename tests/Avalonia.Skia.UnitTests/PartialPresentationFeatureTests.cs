using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class PartialPresentationFeatureTests
{
    [Fact]
    public void Drawing_Context_Exposes_The_Feature_And_Owns_The_Session()
    {
        using var session = new Session();
        using var target = new SkiaGpuRenderTarget(null!, new Target(session));
        Assert.True(target.Properties.SupportsPartialPresentation);
        using (var context = target.CreateDrawingContext(new(new PixelSize(10, 10), 1,
                   CompositionTransparencyLevel.None), out _))
        {
            Assert.Same(session, context.GetFeature<IPartialPresentationFeature>());
            Assert.Empty(session.Events);
            context.GetFeature<IPartialPresentationFeature>()!.Succeeded();
        }
        Assert.Equal(new[] { "succeeded", "present", "dispose" }, session.Events);
    }

    [Fact]
    public void Failed_Context_Construction_Disposes_An_Unsuccessful_Session()
    {
        using var session = new Session { FailSurfaceAccess = true };
        using var target = new SkiaGpuRenderTarget(null!, new Target(session));
        Assert.Throws<InvalidOperationException>(() => target.CreateDrawingContext(
            new(new PixelSize(10, 10), 1, CompositionTransparencyLevel.None), out _));
        Assert.Equal(new[] { "discard", "dispose" }, session.Events);
    }

    private sealed class Target(Session session) : ISkiaGpuRenderTarget
    {
        public RenderTargetProperties Properties => new() { SupportsPartialPresentation = true };
        public ISkiaGpuRenderSession BeginRenderingSession(IRenderTarget.RenderTargetSceneInfo sceneInfo) => session;
        public void Dispose() { }
    }

    private sealed class Session : ISkiaGpuRenderSessionWithPartialPresentation, IPartialPresentationFeature
    {
        private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(10, 10));
        private bool _disposed;
        private bool _succeeded;
        public readonly List<string> Events = new();
        public bool FailSurfaceAccess;
        public GRContext GrContext => null!;
        public SKSurface SkSurface => FailSurfaceAccess ? throw new InvalidOperationException() : _surface;
        public double ScaleFactor => 1;
        public GRSurfaceOrigin SurfaceOrigin => GRSurfaceOrigin.TopLeft;
        public IPartialPresentationFeature PartialPresentation => this;
        public IPartialPresentationBuffer? Buffer => null;
        public void SetDirtyRects(ReadOnlySpan<PixelRect> frameDirtyRects, ReadOnlySpan<PixelRect> bufferDirtyRects) { }
        public void Succeeded()
        {
            _succeeded = true;
            Events.Add("succeeded");
        }
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Events.Add(_succeeded ? "present" : "discard");
            Events.Add("dispose");
            _surface.Dispose();
        }
    }
}
