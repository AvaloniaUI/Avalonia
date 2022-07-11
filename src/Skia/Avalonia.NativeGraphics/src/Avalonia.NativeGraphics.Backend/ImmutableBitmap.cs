using System.IO;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class ImmutableBitmap : IBitmapImpl
    {
        private IAvgStream _stream;
        public IAvgImage _image;
        
        public ImmutableBitmap(Stream stream)
        {
            _stream = AvaloniaNativeGraphicsPlatform.Factory.CreateAvgStream();
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            unsafe
            {
                fixed (void* ptr = &data[0])
                {
                    _stream.write(ptr, (uint) data.Length);
                }
            }

            _image = _stream.makeAvgImage();
        }


        public void Dispose() => _stream.Dispose();

        public void Draw(AvgDrawingContext context, AvgRect source, AvgRect dest)
        {
            context.DrawImage(_image);
        }

        public void Save(string fileName)
        {
            throw new System.NotImplementedException();
        }

        public void Save(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public Vector Dpi => new Vector(96, 96);

        public PixelSize PixelSize
        {
            get
            {
                var size = _image.getSize();
                var px = new PixelSize((int)size.X, (int)size.Y);
                return px;
            }
        }

        public int Version { get; } = 1;
    }
}