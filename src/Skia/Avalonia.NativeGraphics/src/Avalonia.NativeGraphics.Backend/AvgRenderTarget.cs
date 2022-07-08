using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia.NativeGraphics.Backend
{
    internal class AvgRenderTarget : IRenderTarget
    {
        private readonly IAvgRenderTarget _native;

        public AvgRenderTarget(IAvgRenderTarget native)
        {
            _native = native;
        }

        public void Dispose() => _native.Dispose();

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            return new AvgDrawingContext(_native.CreateDrawingContext());
        }
    }
}