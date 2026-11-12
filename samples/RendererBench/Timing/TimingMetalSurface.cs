using System.Diagnostics;
using Avalonia.Metal;
using Avalonia.Platform;

namespace RendererBench.Timing;

/// <summary>
/// Times how long a window frame waits for its drawable, so that the wait is not counted as drawing.
/// </summary>
internal sealed class TimingMetalSurface : IMetalPlatformSurface
{
    private readonly IMetalPlatformSurface _inner;

    public TimingMetalSurface(IMetalPlatformSurface inner) => _inner = inner;

    public bool IsReady => _inner.IsReady;

    public IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device) =>
        new Target(_inner.CreateMetalRenderTarget(device));

    private sealed class Target : IMetalPlatformSurfaceRenderTarget
    {
        private readonly IMetalPlatformSurfaceRenderTarget _inner;

        public Target(IMetalPlatformSurfaceRenderTarget inner) => _inner = inner;

        public PlatformRenderTargetState State => _inner.State;

        public IMetalPlatformSurfaceRenderingSession BeginRendering()
        {
            var start = Stopwatch.GetTimestamp();
            try
            {
                return _inner.BeginRendering();
            }
            finally
            {
                FrameRecorder.Instance.AddAcquire(Stopwatch.GetTimestamp() - start);
            }
        }

        public void Dispose() => _inner.Dispose();
    }
}
