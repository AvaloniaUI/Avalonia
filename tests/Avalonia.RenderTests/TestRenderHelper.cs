using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
using SixLabors.ImageSharp;
using Xunit;
using Avalonia.Platform;
using System.Threading.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using Avalonia.Harfbuzz;
using Avalonia.Skia;

namespace Avalonia.Skia.RenderTests;

static class TestRenderHelper
{
    private static readonly TestDispatcherImpl s_dispatcherImpl =
        new TestDispatcherImpl();

    static TestRenderHelper()
    {
        SkiaPlatform.Initialize();
        AvaloniaLocator.CurrentMutable
            .Bind<IDispatcherImpl>()
            .ToConstant(s_dispatcherImpl);
        
        AvaloniaLocator.CurrentMutable.Bind<IAssetLoader>().ToConstant(new StandardAssetLoader());
        AvaloniaLocator.CurrentMutable.Bind<ITextShaperImpl>().ToConstant(new HarfBuzzTextShaper());
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
                bitmap.Save(path);
            }
        }
        else
        {
            var timer = new ManualRenderTimer();

            var compositor = new Compositor(new RenderLoop(timer), null, true,
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

                writableBitmap.Save(path);
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
        s_dispatcherImpl.MainThread = Thread.CurrentThread;
    }

    public static void EndTest()
    {
        if (Dispatcher.UIThread.CheckAccess()) 
            Dispatcher.UIThread.RunJobs();
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
    
    private class TestDispatcherImpl : IDispatcherImpl
    {
        public bool CurrentThreadIsLoopThread => MainThread?.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;

        public Thread? MainThread { get; set; }

        public event Action? Signaled { add { } remove { } }
        public event Action? Timer { add { } remove { } }

        public void Signal()
        {
            // No-op
        }

        public long Now => 0;

        public void UpdateTimer(long? dueTimeInMs)
        {
            // No-op
        }
    }

    public static void AssertCompareImages(string actualPath, string expectedPath)
    {
        using (var expected = Image.Load<Rgba32>(expectedPath))
        using (var actual = Image.Load<Rgba32>(actualPath))
        {
            double immediateError = TestRenderHelper.CompareImages(actual, expected);

            if (immediateError > 0.022)
            {
                Assert.Fail(actualPath + ": Error = " + immediateError);
            }
        }
    }
    
    /// <summary>
    /// Calculates root mean square error for given two images.
    /// Based roughly on ImageMagick implementation to ensure consistency.
    /// </summary>
    public static double CompareImages(Image<Rgba32> actual, Image<Rgba32> expected)
    {
        if (actual.Width != expected.Width || actual.Height != expected.Height)
        {
            throw new ArgumentException("Images have different resolutions");
        }

        var quantity = actual.Width * actual.Height;
        double squaresError = 0;

        const double scale = 1 / 255d;
            
        for (var x = 0; x < actual.Width; x++)
        {
            double localError = 0;
                
            for (var y = 0; y < actual.Height; y++)
            {
                var expectedAlpha = expected[x, y].A * scale;
                var actualAlpha = actual[x, y].A * scale;
                    
                var r = scale * (expectedAlpha * expected[x, y].R - actualAlpha * actual[x, y].R);
                var g = scale * (expectedAlpha * expected[x, y].G - actualAlpha * actual[x, y].G);
                var b = scale * (expectedAlpha * expected[x, y].B - actualAlpha * actual[x, y].B);
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
