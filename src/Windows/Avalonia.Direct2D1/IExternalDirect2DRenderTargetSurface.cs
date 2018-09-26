namespace Avalonia.Direct2D1
{
    public interface IExternalDirect2DRenderTargetSurface
    {
        SharpDX.Direct2D1.RenderTarget GetOrCreateRenderTarget();
        void DestroyRenderTarget();
        void BeforeDrawing();
        void AfterDrawing();
    }
}
