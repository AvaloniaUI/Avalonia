// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
            public Framebuffer(PixelFormat fmt, int width, int height)
            {
                Format = fmt;
                var bpp = fmt == PixelFormat.Rgb565 ? 2 : 4;
                Width = width;
                Height = height;
                RowBytes = bpp * width;
                Address = Marshal.AllocHGlobal(Height * RowBytes);
            }

            public IntPtr Address { get; }

            public Vector Dpi { get; } = new Vector(96, 96);

            public PixelFormat Format { get; }

            public int Height { get; }

            public int RowBytes { get; }

            public int Width { get; }

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
#if SKIA
             InlineData(PixelFormat.Rgb565)
#endif
            ]
        public void FramebufferRenderResultsShouldBeUsableAsBitmap(PixelFormat fmt)
        {
            var testName = nameof(FramebufferRenderResultsShouldBeUsableAsBitmap) + "_" + fmt;
            var fb = new Framebuffer(fmt, 80, 80);
            var r = Avalonia.AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            using (var target = r.CreateRenderTarget(new object[] { fb }))
            using (var ctx = target.CreateDrawingContext(null))
            {
                ctx.Clear(Colors.Transparent);
                ctx.PushOpacity(0.8);
                ctx.FillRectangle(Brushes.Chartreuse, new Rect(0, 0, 20, 100));
                ctx.FillRectangle(Brushes.Crimson, new Rect(20, 0, 20, 100));
                ctx.FillRectangle(Brushes.Gold, new Rect(40, 0, 20, 100));
                ctx.PopOpacity();
            }

            var bmp = new Bitmap(fmt, fb.Address, fb.Width, fb.Height, fb.RowBytes);
            fb.Deallocate();
            using (var rtb = new RenderTargetBitmap(100, 100))
            {
                using (var ctx = rtb.CreateDrawingContext(null))
                {
                    ctx.FillRectangle(Brushes.Blue, new Rect(0, 0, 100, 100));
                    ctx.FillRectangle(Brushes.Pink, new Rect(0, 20, 100, 10));

                    var rc = new Rect(0, 0, 60, 60);
                    ctx.DrawImage(bmp.PlatformImpl, 1, rc, rc);
                }
                rtb.Save(System.IO.Path.Combine(OutputPath, testName + ".out.png"));
            }
            CompareImagesNoRenderer(testName);
        }

        [Theory]
        [InlineData(PixelFormat.Bgra8888), InlineData(PixelFormat.Rgba8888)]
        public void WritableBitmapShouldBeUsable(PixelFormat fmt)
        {
            var writableBitmap = new WritableBitmap(256, 256, fmt);

            var data = new int[256 * 256];
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                    data[y * 256 + x] =(int)((uint)(x + (y << 8)) | 0xFF000000u);


            using (var l = writableBitmap.Lock())
            {
                for(var r = 0; r<256; r++)
                {
                    Marshal.Copy(data, r * 256, new IntPtr(l.Address.ToInt64() + r * l.RowBytes), 256);
                }
            }


            var name = nameof(WritableBitmapShouldBeUsable) + "_" + fmt;

            writableBitmap.Save(System.IO.Path.Combine(OutputPath, name + ".out.png"));
            CompareImagesNoRenderer(name);

        }
    }
}
