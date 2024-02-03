#nullable enable

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Xunit;
using Path = System.IO.Path;

#pragma warning disable CS0649

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

            public void Deallocate() => Marshal.FreeHGlobal(Address);
            public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new FuncFramebufferRenderTarget(() => this);
        }

        
        [Theory]
        [InlineData(PixelFormatEnum.Rgba8888), InlineData(PixelFormatEnum.Bgra8888),
#if AVALONIA_SKIA
             InlineData(PixelFormatEnum.Rgb565)
#endif
            ]
        internal void FramebufferRenderResultsShouldBeUsableAsBitmap(PixelFormatEnum fmte)
        {
            var fmt = new PixelFormat(fmte);
            var testName = nameof(FramebufferRenderResultsShouldBeUsableAsBitmap) + "_" + fmt;
            var fb = new Framebuffer(fmt, new PixelSize(80, 80));
            var r = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
            using(var cpuContext = r.CreateBackendContext(null))
            using (var target = cpuContext.CreateRenderTarget(new object[] { fb }))
            using (var ctx = target.CreateDrawingContext())
            {
                ctx.Clear(Colors.Transparent);
                ctx.PushOpacity(0.8, new Rect(0, 0, 80, 80));
                ctx.DrawRectangle(Brushes.Chartreuse, null, new Rect(0, 0, 20, 100));
                ctx.DrawRectangle(Brushes.Crimson, null, new Rect(20, 0, 20, 100));
                ctx.DrawRectangle(Brushes.Gold,null, new Rect(40, 0, 20, 100));
                ctx.PopOpacity();
            }

            var bmp = new Bitmap(fmt, AlphaFormat.Premul, fb.Address, fb.Size, new Vector(96, 96), fb.RowBytes);
            fb.Deallocate();
            using (var rtb = new RenderTargetBitmap(new PixelSize(100, 100), new Vector(96, 96)))
            {
                using (var ctx = rtb.CreateDrawingContext())
                {
                    ctx.DrawRectangle(Brushes.Blue, null, new Rect(0, 0, 100, 100));
                    ctx.DrawRectangle(Brushes.Pink, null, new Rect(0, 20, 100, 10));

                    var rc = new Rect(0, 0, 60, 60);
                    ctx.DrawBitmap(bmp.PlatformImpl, 1, rc, rc);
                }
                rtb.Save(Path.Combine(OutputPath, testName + ".out.png"));
            }
            CompareImagesNoRenderer(testName);
        }

        [Theory]
        [InlineData(PixelFormatEnum.Bgra8888), InlineData(PixelFormatEnum.Rgba8888)]
        internal void WriteableBitmapShouldBeUsable(PixelFormatEnum fmte)
        {
            var fmt = new PixelFormat(fmte);
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

            writeableBitmap.Save(Path.Combine(OutputPath, name + ".out.png"));
            CompareImagesNoRenderer(name);

        }
        
        struct RawHeader
        {
            public int Width, Height, Stride;
        }

        [Theory,
         InlineData(PixelFormatEnum.BlackWhite),
         InlineData(PixelFormatEnum.Gray2),
         InlineData(PixelFormatEnum.Gray4),
         InlineData(PixelFormatEnum.Gray8),
         InlineData(PixelFormatEnum.Gray16),
         InlineData(PixelFormatEnum.Rgb24),
         InlineData(PixelFormatEnum.Bgr24),
         InlineData(PixelFormatEnum.Gray32Float),
         InlineData(PixelFormatEnum.Rgba64),
         InlineData(PixelFormatEnum.Rgba64, AlphaFormat.Premul),
        ]
        internal unsafe void BitmapsShouldSupportTranscoders_Lenna(PixelFormatEnum format, AlphaFormat alphaFormat = AlphaFormat.Unpremul)
        {
            var relativeFilesDir = "../../../PixelFormats/Lenna";
            var filesDir = Path.Combine(OutputPath, relativeFilesDir);
            
            var formatName = format.ToString();
            if (alphaFormat == AlphaFormat.Premul)
                formatName = "P" + formatName.ToLowerInvariant();

            var bitsData = File.ReadAllBytes(Path.Combine(filesDir, formatName + ".bits")).AsSpan();
            var header = MemoryMarshal.Cast<byte, RawHeader>(bitsData.Slice(0, Unsafe.SizeOf<RawHeader>()))[0];
            var data = bitsData.Slice(Unsafe.SizeOf<RawHeader>());

            var size = new PixelSize(header.Width, header.Height);
            var stride = header.Stride;
            
            string expectedName = Path.Combine(relativeFilesDir, formatName);
            if (!File.Exists(Path.Combine(OutputPath, expectedName + ".expected.png")))
                expectedName = Path.Combine(relativeFilesDir, "Default");

            var names = new[]
            {
                "_Writeable",
                "_WriteableInitialized",
                "_Normal"
            };
            
            foreach (var step in new[] { 0,1,2 })
            {

                var testName = nameof(BitmapsShouldSupportTranscoders_Lenna) + "_" + formatName + names[step];

                var path = Path.Combine(OutputPath, testName + ".out.png");
                fixed (byte* pData = data)
                {
                    Bitmap? b = null;
                    try
                    {
                        if (step == 0)
                        {
                            var bmp = new WriteableBitmap(size, new Vector(96, 96), new PixelFormat(format),
                                alphaFormat);

                            using (var l = bmp.Lock())
                            {
                                var minStride = (l.Size.Width * l.Format.BitsPerPixel + 7) / 8;
                                for (var y = 0; y < size.Height; y++)
                                {
                                    Unsafe.CopyBlock((l.Address + y * l.RowBytes).ToPointer(), pData + y * stride,
                                        (uint)minStride);
                                }
                            }

                            b = bmp;
                        }
                        else if (step == 1)
                            b = new WriteableBitmap(new PixelFormat(format), alphaFormat, new IntPtr(pData),
                                size, new Vector(96, 96), stride);
                        else
                            b = new Bitmap(new PixelFormat(format), alphaFormat, new IntPtr(pData),
                                size, new Vector(96, 96), stride);

                        if (step < 2)
                        {
                            var copyTo = new byte[data.Length];
                            fixed (byte* pCopyTo = copyTo)
                                b.CopyPixels(default, new IntPtr(pCopyTo), copyTo.Length, stride);
                            Assert.Equal(data.ToArray(), copyTo);
                        }

                        b.Save(path);
                        CompareImagesNoRenderer(testName, expectedName);
                    }
                    finally
                    {
                        b?.Dispose();
                    }
                }
            }
        }

        [Fact]
        public unsafe void CopyPixelsShouldWorkForNonTranscodedBitmaps()
        {
            var stride = 32 * 4;
            var data = new byte[32 * stride];
            new Random().NextBytes(data);
            for (var c = 0; c < data.Length; c++)
                if (data[c] == 0)
                    data[c] = 1;

            Bitmap bmp;
            fixed (byte* pData = data)
                bmp = new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Unpremul, new IntPtr(pData), new PixelSize(32, 32),
                    new Vector(96, 96), 32 * 4);

            var copyTo = new byte[data.Length];
            fixed (byte* pCopyTo = copyTo)
                bmp.CopyPixels(default, new IntPtr(pCopyTo), data.Length, stride);
            Assert.Equal(data, copyTo);
        }
    }
}
