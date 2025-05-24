using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;
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
using SkiaSharp;

#if AVALONIA_SKIA
using Avalonia.Skia;
#else
using Avalonia.Direct2D1;
#endif

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;
#else
namespace Avalonia.Direct2D1.RenderTests;
#endif

static class TestRenderHelper
{
    private static readonly TestDispatcherImpl s_dispatcherImpl =
        new TestDispatcherImpl();

    static TestRenderHelper()
    {
#if AVALONIA_SKIA
        SkiaPlatform.Initialize();
#else
            Direct2D1Platform.Initialize();
#endif
        AvaloniaLocator.CurrentMutable
            .Bind<IDispatcherImpl>()
            .ToConstant(s_dispatcherImpl);
        
        AvaloniaLocator.CurrentMutable.Bind<IAssetLoader>().ToConstant(new StandardAssetLoader());
    }
    
    
    public static Task RenderToFile(Control target, string path, bool immediate, double dpi = 96)
    {
        var dir = Path.GetDirectoryName(path);
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

        while (path.Length > 0 && Path.GetFileName(path) != "tests")
        {
            path = Path.GetDirectoryName(path);
        }

        return path;
    }
    
    private class TestDispatcherImpl : IDispatcherImpl
    {
        public bool CurrentThreadIsLoopThread => MainThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;

        public Thread MainThread { get; set; }

#pragma warning disable 67
        public event Action Signaled;
        public event Action Timer;
#pragma warning restore 67

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
        var expected = File.ReadAllBytes(expectedPath);
        var actual = File.ReadAllBytes(actualPath);
        double immediateError = TestRenderHelper.CompareImages(actual, expected);

        if (immediateError < 95)
        {
            Assert.Fail(actualPath + ": Error = " + immediateError);
        }
    }
    
    /// <summary>
    /// Calculates root mean square error for given two images.
    /// Based roughly on ImageMagick implementation to ensure consistency.
    /// </summary>
    public static double CompareImages(byte[] actualFile, byte[] expectedFile)
    {
        SKBitmap actual = SKBitmap.Decode(actualFile);
        SKBitmap expected = SKBitmap.Decode(expectedFile);
        if (actual.Width != expected.Width || actual.Height != expected.Height)
        {
            throw new ArgumentException("Images have different resolutions");
        }
        var DifferenceHash = new AverageHash();
        var actualHash = DifferenceHash.Hash(actual);
        var expectedHash = DifferenceHash.Hash(expected);
        return CompareHash.Similarity(actualHash, expectedHash);
    }

}
