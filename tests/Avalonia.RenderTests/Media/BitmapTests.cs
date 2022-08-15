using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests.Media
#endif
{
    public class BitmapTests : TestBase
    {
        public BitmapTests()
            : base(@"Media\Bitmap")
        {
            Directory.CreateDirectory(OutputPath);
        }

        class Framebuffer : ILockedFramebuffer, IFramebufferPlatformSurface
        {
            public Framebuffer(PixelFormat fmt, PixelSize size)
            {
                Format = fmt;
                var bpp = fmt == PixelFormat.Rgb565 ? 2 : 4;
                Size = size;
                RowBytes = bpp * size.Width;
                Address = Marshal.AllocHGlobal(size.Height * RowBytes);
            }

            public IntPtr Address { get; }

            public Vector Dpi { get; } = new Vector(96, 96);

            public PixelFormat Format { get; }

            public PixelSize Size { get; }

            public int RowBytes { get; }

            public void Dispose()
            {
                //no-op
            }

            public ILockedFramebuffer Lock()
            {
                return this;
            }

            public void Deallocate() => Marshal.FreeHGlobal(Address);
        }

        
        [Theory]
        [InlineData(PixelFormat.Rgba8888), InlineData(PixelFormat.Bgra8888),
#if AVALONIA_SKIA
             InlineData(PixelFormat.Rgb565)
#endif
            ]
        public void FramebufferRenderResultsShouldBeUsableAsBitmap(PixelFormat fmt)
        {
            var testName = nameof(FramebufferRenderResultsShouldBeUsableAsBitmap) + "_" + fmt;
            var fb = new Framebuffer(fmt, new PixelSize(80, 80));
            var r = Avalonia.AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            using (var target = r.CreateRenderTarget(new object[] { fb }))
            using (var ctx = target.CreateDrawingContext(null))
            {
                ctx.Clear(Colors.Transparent);
                ctx.PushOpacity(0.8);
                ctx.DrawRectangle(Brushes.Chartreuse, null, new Rect(0, 0, 20, 100));
                ctx.DrawRectangle(Brushes.Crimson, null, new Rect(20, 0, 20, 100));
                ctx.DrawRectangle(Brushes.Gold,null, new Rect(40, 0, 20, 100));
                ctx.PopOpacity();
            }

            var bmp = new Bitmap(fmt, AlphaFormat.Premul, fb.Address, fb.Size, new Vector(96, 96), fb.RowBytes);
            fb.Deallocate();
            using (var rtb = new RenderTargetBitmap(new PixelSize(100, 100), new Vector(96, 96)))
            {
                using (var ctx = rtb.CreateDrawingContext(null))
                {
                    ctx.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 100, 100));
                    ctx.DrawRectangle(Brushes.Pink, null, new Rect(0, 20, 100, 10));

                    var rc = new Rect(0, 0, 60, 60);
                    ctx.DrawBitmap(bmp.PlatformImpl, 1, rc, rc);
                }
                rtb.Save(System.IO.Path.Combine(OutputPath, testName + ".out.png"));
            }
            CompareImagesNoRenderer(testName);
        }

        [Theory]
        [InlineData(PixelFormat.Bgra8888), InlineData(PixelFormat.Rgba8888)]
        public void WriteableBitmapShouldBeUsable(PixelFormat fmt)
        {
            var writeableBitmap = new WriteableBitmap(new PixelSize(256, 256), new Vector(96, 96), fmt);

            var data = new int[256 * 256];
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                    data[y * 256 + x] =(int)((uint)(x + (y << 8)) | 0xFF000000u);


            using (var l = writeableBitmap.Lock())
            {
                for(var r = 0; r<256; r++)
                {
                    Marshal.Copy(data, r * 256, new IntPtr(l.Address.ToInt64() + r * l.RowBytes), 256);
                }
            }


            var name = nameof(WriteableBitmapShouldBeUsable) + "_" + fmt;

            writeableBitmap.Save(System.IO.Path.Combine(OutputPath, name + ".out.png"));
            CompareImagesNoRenderer(name);

        }
    }
}
