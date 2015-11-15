using Android.Graphics;

namespace Perspex.Android.Platform.CanvasPlatform
{
    public interface IAndroidViewSupportRender
    {
        void PreRender(Canvas canvas, Rect rect);

        void Render(Canvas canvas, Rect rect);

        void PostRender(Canvas canvas, Rect rect);
    }
}