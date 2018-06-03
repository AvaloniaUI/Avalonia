using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Effects
{
    interface IDirect2DPlatformEffectImpl
    {
        Effect Render(DeviceContext context, Bitmap biutmap);
    }
}
