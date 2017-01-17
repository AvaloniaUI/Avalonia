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
    public abstract class BitmapImpl : IBitmapImpl, IDisposable
    {
        public abstract Bitmap GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget target);
        public abstract int PixelWidth { get; }
        public abstract int PixelHeight { get; }
        public abstract void Save(string fileName);
        public abstract void Save(Stream stream);

        public virtual void Dispose()
        {
        }
    }
}
