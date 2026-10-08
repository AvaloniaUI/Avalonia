using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Harfbuzz;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.RenderTests;

static class TestRenderHelper
{
    static TestRenderHelper()
    {
        SkiaPlatform.Initialize();
        AvaloniaLocator.CurrentMutable.Bind<IAssetLoader>().ToConstant(new StandardAssetLoader());
        AvaloniaLocator.CurrentMutable.Bind<ITextShaperImpl>().ToConstant(new HarfBuzzTextShaper());
        AvaloniaLocator.CurrentMutable.Bind<ICursorFactory>().ToConstant(new NullCursorFactory());
    }

    private sealed class NullCursorFactory : ICursorFactory
    {
        public ICursorImpl GetCursor(StandardCursorType cursorType) => new NullCursor();
        public ICursorImpl CreateCursor(Bitmap cursor, PixelPoint hotSpot) => new NullCursor();

        private sealed class NullCursor : ICursorImpl
        {
            public void Dispose() { }
        }
    }
    
    
    public static Task RenderToFile(Control target, string path, bool immediate, double dpi = 96)
    {
        var dir = Path.GetDirectoryName(path);
        Assert.NotNull(dir);

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
        var pixelSize = new PixelSize((int)target.Width, (int)target.Height);
        var size = new Size(target.Width, target.Height);
        var dpiVector = new Vector(dpi, dpi);

        if (immediate)
        {
            using (RenderTargetBitmap bitmap = new RenderTargetBitmap(pixelSize, dpiVector))
            {
                target.Measure(size);
                target.Arrange(new Rect(size));
                bitmap.Render(target);
                bitmap.Save(path, PngBitmapEncoderOptions.Default);
            }
        }
        else
        {
            var timer = new ManualRenderTimer();

            var compositor = new Compositor(RenderLoop.FromTimer(timer), null, true,
                new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);
            using (var writableBitmap = factory.CreateWriteableBitmap(pixelSize, dpiVector, factory.DefaultPixelFormat,
                       factory.DefaultAlphaFormat))
            {
                var root = new TestRenderRoot(dpiVector.X / 96, null!);
                using (var renderer = new CompositingRenderer(root, compositor,
                           () => new[] { new BitmapFramebufferSurface(writableBitmap) }))
                {
                    root.Initialize(renderer, target);
                    renderer.Start();
                    Dispatcher.UIThread.RunJobs();
                    renderer.Paint(new Rect(root.Bounds.Size), false);
                }

                using var fileStream = File.Create(path);
                writableBitmap.Save(fileStream, PngBitmapEncoderOptions.Default);
            }
        }

        return Task.CompletedTask;
    }

    class BitmapFramebufferSurface : IFramebufferPlatformSurface
    {
        private readonly IWriteableBitmapImpl _bitmap;

        public BitmapFramebufferSurface(IWriteableBitmapImpl bitmap)
        {
            _bitmap = bitmap;
        }

        public IFramebufferRenderTarget CreateFramebufferRenderTarget()
        {
            return new FuncFramebufferRenderTarget(() => _bitmap.Lock());
        }
    }


    public static void BeginTest()
    {
        Dispatcher.ResetBeforeUnitTests();
    }

    public static void EndTest()
    {
        if (Dispatcher.UIThread.CheckAccess()) 
            Dispatcher.UIThread.RunJobs();
        Dispatcher.ResetForUnitTests();
    }
    
    public static string GetTestsDirectory()
    {
        var path = Directory.GetCurrentDirectory();

        while (!string.IsNullOrEmpty(path) && Path.GetFileName(path) != "tests")
        {
            path = Path.GetDirectoryName(path);
        }

        Assert.NotNull(path);
        return path;
    }

    public static void AssertCompareImages(string actualPath, string expectedPath)
    {
        using (var expected = LoadImage(expectedPath))
        using (var actual = LoadImage(actualPath))
        {
            double immediateError = TestRenderHelper.CompareImages(actual, expected);

            if (immediateError > 0.022)
            {
                Assert.Fail(actualPath + ": Error = " + immediateError);
            }
        }
    }
    
    /// <summary>
    /// Loads an image as non-premultiplied RGBA, so that <see cref="CompareImages"/> can read raw channels.
    /// </summary>
    public static SKBitmap LoadImage(string path)
    {
        using var codec = SKCodec.Create(path) ?? throw new InvalidOperationException($"Couldn't decode {path}");
        var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        return SKBitmap.Decode(codec, info) ?? throw new InvalidOperationException($"Couldn't decode {path}");
    }

    /// <summary>
    /// Calculates root mean square error for given two images.
    /// Based roughly on ImageMagick implementation to ensure consistency.
    /// </summary>
    public static double CompareImages(SKBitmap actual, SKBitmap expected)
    {
        if (actual.ColorType != SKColorType.Rgba8888 ||
            actual.AlphaType != SKAlphaType.Unpremul ||
            expected.ColorType != SKColorType.Rgba8888 ||
            expected.AlphaType != SKAlphaType.Unpremul)
        {
            throw new ArgumentException("Images must be in non-premultiplied RGBA format");
        }

        if (actual.Width != expected.Width || actual.Height != expected.Height)
        {
            throw new ArgumentException("Images have different resolutions");
        }

        var quantity = actual.Width * actual.Height;
        double squaresError = 0;

        const double scale = 1 / 255d;

        var actualPixels = actual.GetPixelSpan();
        var expectedPixels = expected.GetPixelSpan();
        var actualRowBytes = actual.RowBytes;
        var expectedRowBytes = expected.RowBytes;

        for (var x = 0; x < actual.Width; x++)
        {
            double localError = 0;
                
            for (var y = 0; y < actual.Height; y++)
            {
                var expectedPixel = expectedPixels.Slice(y * expectedRowBytes + x * 4, 4);
                var actualPixel = actualPixels.Slice(y * actualRowBytes + x * 4, 4);

                var expectedAlpha = expectedPixel[3] * scale;
                var actualAlpha = actualPixel[3] * scale;
                    
                var r = scale * (expectedAlpha * expectedPixel[0] - actualAlpha * actualPixel[0]);
                var g = scale * (expectedAlpha * expectedPixel[1] - actualAlpha * actualPixel[1]);
                var b = scale * (expectedAlpha * expectedPixel[2] - actualAlpha * actualPixel[2]);
                var a = expectedAlpha - actualAlpha;

                var error = r * r + g * g + b * b + a * a;

                localError += error;
            }

            squaresError += localError;
        }

        var meanSquaresError = squaresError / quantity;

        const int channelCount = 4;
            
        meanSquaresError = meanSquaresError / channelCount;
            
        return Math.Sqrt(meanSquaresError);
    }

}
