// This code was really useful for debugging,
// but I'm not going to port it to MicroCom
// If the need ever arises again, just define HAS_SHARPDX

#if HAS_SHARPDX
using System.IO;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using Device = SharpDX.Direct3D11.Device;
using Device1 = SharpDX.Direct3D11.Device1;

namespace Avalonia.Win32.OpenGl
{

    static class Texture2DExtensions
    {
        public static void Save(this Texture2D texture, string path, Device device)
        {
            var textureCopy = new Texture2D(device,
                new Texture2DDescription
                {
                    Width = (int)texture.Description.Width,
                    Height = (int)texture.Description.Height,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = texture.Description.Format,
                    Usage = ResourceUsage.Staging,
                    SampleDescription = new SampleDescription(1, 0),
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None
                });
            device.ImmediateContext.CopyResource(texture, textureCopy);
            device.ImmediateContext.Flush();

            DataStream dataStream;
            var dataBox = device.ImmediateContext.MapSubresource(
                textureCopy,
                0,
                0,
                MapMode.Read,
                SharpDX.Direct3D11.MapFlags.None,
                out dataStream);

            var dataRectangle = new DataRectangle { DataPointer = dataStream.DataPointer, Pitch = dataBox.RowPitch };

            using var factory = new ImagingFactory();
            using var bitmap = new Bitmap(
                factory,
                texture.Description.Width,
                texture.Description.Height,
                PixelFormat.Format32bppBGRA,
                dataRectangle);

            using (var s = File.Create(path))
            {
                s.Position = 0;
                using (var bitmapEncoder = new PngBitmapEncoder(factory, s))
                {
                    using (var bitmapFrameEncode = new BitmapFrameEncode(bitmapEncoder))
                    {
                        bitmapFrameEncode.Initialize();
                        bitmapFrameEncode.SetSize(bitmap.Size.Width, bitmap.Size.Height);
                        var pixelFormat = PixelFormat.FormatDontCare;
                        bitmapFrameEncode.SetPixelFormat(ref pixelFormat);
                        bitmapFrameEncode.WriteSource(bitmap);
                        bitmapFrameEncode.Commit();
                        bitmapEncoder.Commit();
                    }
                }
            }

            device.ImmediateContext.UnmapSubresource(texture, 0);
        }
    }
}
#endif
