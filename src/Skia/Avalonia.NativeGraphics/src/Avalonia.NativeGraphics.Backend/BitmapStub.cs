using System.IO;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class BitmapStub : IBitmapImpl
    {
        MemoryStream _ms = new MemoryStream();
        public BitmapStub(Stream s)
        {
            s.CopyTo(_ms);
            
        }
        public void Dispose()
        {
            
        }

        public void Save(string fileName)
        {
            File.WriteAllBytes(fileName, _ms.ToArray());
            
        }

        public void Save(Stream stream)
        {
            _ms.Position = 0;
            _ms.CopyTo(stream);
        }

        public Vector Dpi => new Vector(96, 96);
        public PixelSize PixelSize => new PixelSize(1, 1);
        public int Version { get; } = 1;
    }
}