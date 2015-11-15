using Perspex.Android.Platform.Specific;
using Perspex.Media;
using Perspex.Platform;
using System.IO;
using AG = Android.Graphics;
using System;
using Perspex.Android.Platform.CanvasPlatform;

namespace Perspex.Android.CanvasRendering
{
    public class AndroidPlatformRender : IPlatformRenderInterface
    {
        private static readonly AndroidPlatformRender instance = new AndroidPlatformRender();

        public IFormattedTextImpl CreateFormattedText(string text, string fontFamilyName, double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight)
        {
            return new FormattedTextImpl(text, fontFamilyName, fontSize, fontStyle, textAlignment, fontWeight);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            return new StreamGeometryImpl();
        }

        public IRenderTarget CreateRenderer(IPlatformHandle handle)
        {
            return new RenderTarget((IAndroidCanvasView)handle);
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int pwidth, int pheight)
        {
            int width = PointUnitService.Instance.PerspexToNativeXInt(pwidth);
            int height = PointUnitService.Instance.PerspexToNativeYInt(pwidth);

            return new RenderTargetBitmapImpl(CreateNativeBitmap(width, height));
        }

        public IBitmapImpl CreateBitmap(int pwidth, int pheight)
        {
            int width = PointUnitService.Instance.PerspexToNativeXInt(pwidth);
            int height = PointUnitService.Instance.PerspexToNativeYInt(pwidth);
            return new BitmapImpl(CreateNativeBitmap(width, height));
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            return new BitmapImpl(AG.BitmapFactory.DecodeFile(fileName));
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            return new BitmapImpl(AG.BitmapFactory.DecodeStream(stream));
        }

        public static void Initialize()
            => PerspexLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(instance);

        private AG.Bitmap CreateNativeBitmap(int width, int height)
        {
            return AG.Bitmap.CreateBitmap(width, height, AG.Bitmap.Config.Argb8888);
        }
    }
}