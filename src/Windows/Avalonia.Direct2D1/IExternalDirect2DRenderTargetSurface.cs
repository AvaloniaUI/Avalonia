namespace Avalonia.Direct2D1
{
    public interface IExternalDirect2DRenderTargetSurface
    {
        Vortice.Direct2D1.ID2D1RenderTarget GetOrCreateRenderTarget();
        void DestroyRenderTarget();
        void BeforeDrawing();
        void AfterDrawing();
    }
}
