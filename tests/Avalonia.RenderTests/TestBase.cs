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
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
#if AVALONIA_SKIA
using Avalonia.Skia;
#else
using Avalonia.Direct2D1;
#endif

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests
#endif
{
    public class TestBase
    {
#if AVALONIA_SKIA
        private static string s_fontUri = "resm:Avalonia.Skia.RenderTests.Assets?assembly=Avalonia.Skia.RenderTests#Noto Mono";
#else
        private static string s_fontUri = "resm:Avalonia.Direct2D1.RenderTests.Assets?assembly=Avalonia.Direct2D1.RenderTests#Noto Mono";
#endif
        public static FontFamily TestFontFamily = new FontFamily(s_fontUri);

        private static readonly TestThreadingInterface threadingInterface =
            new TestThreadingInterface();

        private static readonly IAssetLoader assetLoader = new AssetLoader();
        
        static TestBase()
        {
#if AVALONIA_SKIA
            SkiaPlatform.Initialize();
#else
            Direct2D1Platform.Initialize();
#endif
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>()
                .ToConstant(threadingInterface);

            AvaloniaLocator.CurrentMutable
                .Bind<IAssetLoader>()
                .ToConstant(assetLoader);
        }

        public TestBase(string outputPath)
        {
            outputPath = outputPath.Replace('\\', Path.DirectorySeparatorChar);
            var testPath = GetTestsDirectory();
            var testFiles = Path.Combine(testPath, "TestFiles");
#if AVALONIA_SKIA
            var platform = "Skia";
#else
            var platform = "Direct2D1";
#endif
            OutputPath = Path.Combine(testFiles, platform, outputPath);

            threadingInterface.MainThread = Thread.CurrentThread;
        }

        public string OutputPath
        {
            get;
        }

        protected async Task RenderToFile(Control target, [CallerMemberName] string testName = "", double dpi = 96)
        {
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            var immediatePath = Path.Combine(OutputPath, testName + ".immediate.out.png");
            var deferredPath = Path.Combine(OutputPath, testName + ".deferred.out.png");
            var compositedPath = Path.Combine(OutputPath, testName + ".composited.out.png");
            var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
            var pixelSize = new PixelSize((int)target.Width, (int)target.Height);
            var size = new Size(target.Width, target.Height);
            var dpiVector = new Vector(dpi, dpi);

            using (RenderTargetBitmap bitmap = new RenderTargetBitmap(pixelSize, dpiVector))
            {
                target.Measure(size);
                target.Arrange(new Rect(size));
                bitmap.Render(target);
                bitmap.Save(immediatePath);
            }
            
            
            using (var rtb = factory.CreateRenderTargetBitmap(pixelSize, dpiVector))
            using (var renderer = new DeferredRenderer(target, rtb))
            {
                target.Measure(size);
                target.Arrange(new Rect(size));
                renderer.UnitTestUpdateScene();

                // Do the deferred render on a background thread to expose any threading errors in
                // the deferred rendering path.
                await Task.Run((Action)renderer.UnitTestRender);
                threadingInterface.MainThread = Thread.CurrentThread;

                rtb.Save(deferredPath);
            }

            var timer = new ManualRenderTimer();

            var compositor = new Compositor(new RenderLoop(timer, Dispatcher.UIThread), null);
            using (var writableBitmap = factory.CreateWriteableBitmap(pixelSize, dpiVector, factory.DefaultPixelFormat, factory.DefaultAlphaFormat))
            {
                var root = new TestRenderRoot(dpiVector.X / 96, null!);
                using (var renderer = new CompositingRenderer(root, compositor, () => new[]
                       {
                           new BitmapFramebufferSurface(writableBitmap)
                       }) { RenderOnlyOnRenderThread = false })
                {
                    root.Initialize(renderer, target);
                    renderer.Start();
                    Dispatcher.UIThread.RunJobs();
                    timer.TriggerTick();
                }

                // Free pools
                for (var c = 0; c < 11; c++)
                    TestThreadingInterface.RunTimers();
                writableBitmap.Save(compositedPath);
            }
        }

        class BitmapFramebufferSurface : IFramebufferPlatformSurface
        {
            private readonly IWriteableBitmapImpl _bitmap;

            public BitmapFramebufferSurface(IWriteableBitmapImpl bitmap)
            {
                _bitmap = bitmap;
            }
            
            public ILockedFramebuffer Lock() => _bitmap.Lock();
        }

        protected void CompareImages([CallerMemberName] string testName = "",
            bool skipImmediate = false, bool skipDeferred = false, bool skipCompositor = false)
        {
            var expectedPath = Path.Combine(OutputPath, testName + ".expected.png");
            var immediatePath = Path.Combine(OutputPath, testName + ".immediate.out.png");
            var deferredPath = Path.Combine(OutputPath, testName + ".deferred.out.png");
            var compositedPath = Path.Combine(OutputPath, testName + ".composited.out.png");

            using (var expected = Image.Load<Rgba32>(expectedPath))
            using (var immediate = Image.Load<Rgba32>(immediatePath))
            using (var deferred = Image.Load<Rgba32>(deferredPath))
            using (var composited = Image.Load<Rgba32>(compositedPath))
            {
                var immediateError = CompareImages(immediate, expected);
                var deferredError = CompareImages(deferred, expected);
                var compositedError = CompareImages(composited, expected);

                if (immediateError > 0.022 && !skipImmediate)
                {
                    Assert.True(false, immediatePath + ": Error = " + immediateError);
                }

                if (deferredError > 0.022 && !skipDeferred)
                {
                    Assert.True(false, deferredPath + ": Error = " + deferredError);
                }
                
                if (compositedError > 0.022 && !skipCompositor)
                {
                    Assert.True(false, compositedPath + ": Error = " + compositedError);
                }
            }
        }

        protected void CompareImagesNoRenderer([CallerMemberName] string testName = "")
        {
            var expectedPath = Path.Combine(OutputPath, testName + ".expected.png");
            var actualPath = Path.Combine(OutputPath, testName + ".out.png");

            using (var expected = Image.Load<Rgba32>(expectedPath))
            using (var actual = Image.Load<Rgba32>(actualPath))
            {
                double immediateError = CompareImages(actual, expected);

                if (immediateError > 0.022)
                {
                    Assert.True(false, actualPath + ": Error = " + immediateError);
                }
            }
        }
        
        /// <summary>
        /// Calculates root mean square error for given two images.
        /// Based roughly on ImageMagick implementation to ensure consistency.
        /// </summary>
        private static double CompareImages(Image<Rgba32> actual, Image<Rgba32> expected)
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

        private static string GetTestsDirectory()
        {
            var path = Directory.GetCurrentDirectory();

            while (path.Length > 0 && Path.GetFileName(path) != "tests")
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }

        private class TestThreadingInterface : IPlatformThreadingInterface
        {
            public bool CurrentThreadIsLoopThread => MainThread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId;

            public Thread MainThread { get; set; }

#pragma warning disable 67
            public event Action<DispatcherPriority?> Signaled;
#pragma warning restore 67

            public void RunLoop(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public void Signal(DispatcherPriority prio)
            {
                // No-op
            }

            private static List<Action> s_timers = new();
            
            public static void RunTimers()
            {
                lock (s_timers)
                {
                    foreach(var t in s_timers.ToList())
                        t.Invoke();
                }
            }

            public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
            {
                var act = () => tick();
                lock (s_timers) s_timers.Add(act);
                return Disposable.Create(() =>
                {
                    lock (s_timers) s_timers.Remove(act);
                });
            }
        }
    }
}
