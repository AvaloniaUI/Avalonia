using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Media;
using Android.Views;

namespace Avalonia.Android.Previewer
{
    internal class PreviewSurface : Java.Lang.Object, IDisposable, ImageReader.IOnImageAvailableListener
    {
        private const int BaseHeight = 1200;
        private readonly float _baseScaling = 1;

        private ImageReader _reader;

        public Surface? Surface => _reader.Surface;

        public float Width { get; }
        public float Height { get; }
        public float Scaling { get; set; } = 1;
        
        public event EventHandler<PreviewBitmapReadyEventArgs>? PreviewBitmapReady;

        public PreviewSurface(float width, float height)
        {
            _reader = ImageReader.NewInstance((int)width, (int)height, (ImageFormatType)1, 2);
            _reader.SetOnImageAvailableListener(this, null);
            Width = width;
            Height = height;

            _baseScaling = BaseHeight / Height;
        }

        void IDisposable.Dispose()
        {
            _reader.SetOnImageAvailableListener(null, null);
            _reader.Close();
        }

        public unsafe void OnImageAvailable(ImageReader? reader)
        {
            if(reader?.AcquireLatestImage() is { } image)
            {
                var planes = image.GetPlanes();
                if(planes != null && planes.Length == 1)
                {
                    var plane = planes[0];
                    var buffer = plane.Buffer;
                    var size = buffer!.Rewind()!.Capacity();
                    var pixelStride = plane.PixelStride;
                    var rowStride = plane.RowStride;
                    var rowPadding = rowStride - pixelStride * Width;

                    var bitmap = Bitmap.CreateBitmap((int)(Width + rowPadding / pixelStride), (int)Height, Bitmap.Config.Argb8888!);
                    bitmap.CopyPixelsFromBuffer(buffer);
                    var effectiveScaling = _baseScaling * Scaling;
                    var scaledWidth = (int)(effectiveScaling * Width);
                    var scaledHeight = (int)(effectiveScaling * Height);
                    var scaled = Bitmap.CreateScaledBitmap(bitmap, scaledWidth, scaledHeight, false);
                    var pixels = new int[scaledWidth * scaledHeight];
                    scaled.GetPixels(pixels, 0, scaledWidth, 0, 0, scaledWidth, scaledHeight);

                    byte[] data = Array.Empty<byte>();
                    fixed(int* p = pixels)
                    {
                        data = new Span<byte>(p, scaledWidth * scaledHeight * 4).ToArray();
                    }
                    PreviewBitmapReady?.Invoke(this, new PreviewBitmapReadyEventArgs(data, scaledWidth, scaledHeight, scaledWidth * 4));

                    bitmap.Recycle();
                    scaled.Recycle();
                }

                image.Close();
            }
        }
    }

    internal class PreviewBitmapReadyEventArgs(byte[] data, int width, int height, int rowStride) : System.EventArgs
    {
        public byte[] Data { get; } = data;
        public int Width { get; } = width;
        public int Height { get; } = height;
        public int RowStride { get; } = rowStride;
    }
}
