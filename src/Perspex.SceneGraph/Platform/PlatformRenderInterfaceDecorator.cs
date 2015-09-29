using System.IO;
using Perspex.Media;

namespace Perspex.Platform
{
    public class PlatformRenderInterfaceDecorator : IPlatformRenderInterface
    {
        private readonly IPlatformRenderInterface _orig;

        public PlatformRenderInterfaceDecorator(IPlatformRenderInterface orig)
        {
            _orig = orig;
        }

        public virtual IBitmapImpl CreateBitmap(int width, int height) => _orig.CreateBitmap(width, height);

        public virtual IFormattedTextImpl CreateFormattedText(string text, string fontFamilyName, double fontSize,
            FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight)
            =>
                _orig.CreateFormattedText(text, fontFamilyName, fontSize, fontStyle,
                    textAlignment, fontWeight);

        public virtual IStreamGeometryImpl CreateStreamGeometry() => _orig.CreateStreamGeometry();

        public virtual IRenderer CreateRenderer(IPlatformHandle handle, double width, double height)
            => _orig.CreateRenderer(handle, width, height);

        public virtual IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
            => _orig.CreateRenderTargetBitmap(width, height);

        public virtual IBitmapImpl LoadBitmap(string fileName) => _orig.LoadBitmap(fileName);
        public virtual IBitmapImpl LoadBitmap(Stream stream) => _orig.LoadBitmap(stream);
    }
}