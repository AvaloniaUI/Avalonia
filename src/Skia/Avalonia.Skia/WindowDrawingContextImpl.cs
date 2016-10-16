
namespace Avalonia.Skia
{
    // not sure we need this yet
    internal class WindowDrawingContextImpl : DrawingContextImpl
    {
        WindowRenderTarget _target;

        public WindowDrawingContextImpl(WindowRenderTarget target, double scale = 1.0)
            : base(target.Surface.Canvas, scale)
        {
            _target = target;
        }

        public override void Dispose()
        {
            base.Dispose();
            _target.Present();
        }
    }
}