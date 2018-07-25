using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Effects
{
    interface IDirect2DPlatformEffectImpl
    {
        void Render(DeviceContext context, Bitmap biutmap);
    }
}
