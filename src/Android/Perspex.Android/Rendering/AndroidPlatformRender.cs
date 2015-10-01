using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Perspex.Media;
using Perspex.Platform;
using TextAlignment = Perspex.Media.TextAlignment;

namespace Perspex.Android.Rendering
{
    public class AndroidPlatformRender : IPlatformRenderInterface
    {
        private readonly static AndroidPlatformRender instance = new AndroidPlatformRender();

        public static void Initialize()
            => PerspexLocator.CurrentMutable.Bind<IPlatformRenderInterface>().ToConstant(instance);

        public IBitmapImpl CreateBitmap(int width, int height)
        {
            throw new NotImplementedException();
        }

        public IFormattedTextImpl CreateFormattedText(string text, string fontFamilyName, double fontSize, FontStyle fontStyle,
            TextAlignment textAlignment, FontWeight fontWeight)
        {
            return new FormattedTextImpl(text, fontFamilyName, fontSize, fontStyle, textAlignment, fontWeight);
        }

        public IStreamGeometryImpl CreateStreamGeometry()
        {
            throw new NotImplementedException();
        }

        public IRenderTarget CreateRenderer(IPlatformHandle handle, double width, double height)
        {
            throw new NotImplementedException();
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
    }
}