using System;
using System.IO;
using Perspex.Media;
using Perspex.Platform;

namespace Perspex.Android.Rendering
{
    public class AndroidPlatformRender : IPlatformRenderInterface
    {
        private static readonly AndroidPlatformRender instance = new AndroidPlatformRender();

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            throw new NotImplementedException();
        }

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

        public IRenderTarget CreateRenderer(IPlatformHandle handle, double width, double height)
        {
            return PerspexActivity.Instance.View ?? new PerspexView();
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            throw new NotImplementedException();
        }

        public static void Initialize()
            => PerspexLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(instance);
    }
}