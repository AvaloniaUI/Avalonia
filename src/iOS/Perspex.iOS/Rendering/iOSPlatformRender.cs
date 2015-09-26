using Perspex.Platform;
using System;
using System.Collections.Generic;
using System.Text;
using Perspex.Media;
using System.IO;

namespace Perspex.iOS.Rendering
{
    public class iOSPlatformRender : IPlatformRenderInterface
    {
        private static readonly iOSPlatformRender s_instance = new iOSPlatformRender();
        public static void Initialize() => PerspexLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(s_instance);

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            throw new NotImplementedException();
        }

        public IFormattedTextImpl CreateFormattedText(string text, string fontFamilyName, double fontSize, FontStyle fontStyle, TextAlignment textAlignment, FontWeight fontWeight)
        {
            throw new NotImplementedException();
        }

        public IRenderer CreateRenderer(IPlatformHandle handle, double width, double height)
        {
            return new Renderer(handle, width, height);
        }

        public IRenderTargetBitmapImpl CreateRenderTargetBitmap(int width, int height)
        {
            throw new NotImplementedException();
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(Stream stream)
        {
            throw new NotImplementedException();
        }

        public IBitmapImpl LoadBitmap(string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
