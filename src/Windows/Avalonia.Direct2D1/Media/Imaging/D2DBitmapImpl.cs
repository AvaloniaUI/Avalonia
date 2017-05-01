using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using SharpDX.Direct2D1;

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
        public D2DBitmapImpl(Bitmap d2DBitmap)
        {
            if (d2DBitmap == null) throw new ArgumentNullException(nameof(d2DBitmap));

            _direct2D = d2DBitmap;
        }

        public override Bitmap GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target) => _direct2D;
               
        public override int PixelWidth => _direct2D.PixelSize.Width;
        public override int PixelHeight => _direct2D.PixelSize.Height;

        public override void Save(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void Save(Stream stream)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            base.Dispose();
            _direct2D.Dispose();
        }
    }
}
