using System;
using System.IO;
using SharpDX.Direct2D1;
using ImageParameters = SharpDX.WIC.ImageParameters;
using ImagingFactory2 = SharpDX.WIC.ImagingFactory2;
using PngBitmapEncoder = SharpDX.WIC.PngBitmapEncoder;
using WICFactory = SharpDX.WIC.ImagingFactory;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D Bitmap implementation that uses a GPU memory bitmap as its image.
    /// </summary>
    public class D2DBitmapImpl : BitmapImpl
    {
        private Bitmap _direct2DBitmap;

        /// <summary>
        /// Initialize a new instance of the <see cref="BitmapImpl"/> class
        /// with a bitmap backed by GPU memory.
        /// </summary>
        /// <param name="d2DBitmap">The GPU bitmap.</param>
        /// <remarks>
        /// This bitmap must be either from the same render target,
        /// or if the render target is a <see cref="SharpDX.Direct2D1.DeviceContext"/>,
        /// the device associated with this context, to be renderable.
        /// </remarks>
        public D2DBitmapImpl(Bitmap d2DBitmap)
        {
            _direct2DBitmap = d2DBitmap ?? throw new ArgumentNullException(nameof(d2DBitmap));
        }
              
        public override int PixelWidth => _direct2DBitmap.PixelSize.Width;
        public override int PixelHeight => _direct2DBitmap.PixelSize.Height;

        public override void Dispose()
        {
            base.Dispose();
            _direct2DBitmap.Dispose();
        }

        public override OptionalDispose<Bitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target)
        {
            return new OptionalDispose<Bitmap>(_direct2DBitmap, false);
        }

        public override void Save(Stream stream)
        {
            using (var encoder = new PngBitmapEncoder(Direct2D1Platform.ImagingFactory, stream))
            using (var frameEncode = new SharpDX.WIC.BitmapFrameEncode(encoder))
            //ToDo: Not supported under Windows 7!
            using (var imageEncoder = new SharpDX.WIC.ImageEncoder((ImagingFactory2)Direct2D1Platform.ImagingFactory, null))
            {
                var parameters = new ImageParameters(
                    new PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied),
                    _direct2DBitmap.DotsPerInch.Width,
                    _direct2DBitmap.DotsPerInch.Height,
                    0, 0, PixelWidth, PixelHeight);

                imageEncoder.WriteFrame(_direct2DBitmap, frameEncode, parameters);
                frameEncode.Commit();
                encoder.Commit();
            }
        }
    }
}
