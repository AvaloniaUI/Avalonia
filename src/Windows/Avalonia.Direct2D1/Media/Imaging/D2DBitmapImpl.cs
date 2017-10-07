using System;
using System.IO;
using SharpDX.Direct2D1;
using WICFactory = SharpDX.WIC.ImagingFactory;
using ImagingFactory2 = SharpDX.WIC.ImagingFactory2;
using ImageParameters = SharpDX.WIC.ImageParameters;
using PngBitmapEncoder = SharpDX.WIC.PngBitmapEncoder;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D Bitmap implementation that uses a GPU memory bitmap as its image.
    /// </summary>
    public class D2DBitmapImpl : BitmapImpl
    {
        private Bitmap _direct2D;

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
        public D2DBitmapImpl(WICFactory imagingFactory, Bitmap d2DBitmap)
            : base(imagingFactory)
        {
            _direct2D = d2DBitmap ?? throw new ArgumentNullException(nameof(d2DBitmap));
        }
              
        public override int PixelWidth => _direct2D.PixelSize.Width;
        public override int PixelHeight => _direct2D.PixelSize.Height;

        public override void Dispose()
        {
            base.Dispose();
            _direct2D.Dispose();
        }

        public override OptionalDispose<Bitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target)
        {
            return new OptionalDispose<Bitmap>(_direct2D, false);
        }

        public override void Save(Stream stream)
        {
            using (var encoder = new PngBitmapEncoder(WicImagingFactory, stream))
            using (var frameEncode = new SharpDX.WIC.BitmapFrameEncode(encoder))
            using (var imageEncoder = new SharpDX.WIC.ImageEncoder((ImagingFactory2)WicImagingFactory, null))
            {
                var parameters = new ImageParameters(
                    new PixelFormat(SharpDX.DXGI.Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied),
                    _direct2D.DotsPerInch.Width,
                    _direct2D.DotsPerInch.Height,
                    0, 0, PixelWidth, PixelHeight);

                imageEncoder.WriteFrame(_direct2D, frameEncode, parameters);
                frameEncode.Commit();
                encoder.Commit();
            }
        }
    }
}
