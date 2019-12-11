// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.IO;
using System.Runtime.CompilerServices;
using ImageMagick;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;

using Xunit;
using Avalonia.Platform;
using System.Threading.Tasks;
using System;
using System.Threading;
using Avalonia.Threading;
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
        private static readonly TestThreadingInterface threadingInterface =
            new TestThreadingInterface();

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
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
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

                rtb.Save(deferredPath);
            }
        }

        protected void CompareImages([CallerMemberName] string testName = "")
        {
            var expectedPath = Path.Combine(OutputPath, testName + ".expected.png");
            var immediatePath = Path.Combine(OutputPath, testName + ".immediate.out.png");
            var deferredPath = Path.Combine(OutputPath, testName + ".deferred.out.png");

            using (var expected = new MagickImage(expectedPath))
            using (var immediate = new MagickImage(immediatePath))
            using (var deferred = new MagickImage(deferredPath))
            {
                double immediateError = expected.Compare(immediate, ErrorMetric.RootMeanSquared);
                double deferredError = expected.Compare(deferred, ErrorMetric.RootMeanSquared);

                if (immediateError > 0.022)
                {
                    Assert.True(false, immediatePath + ": Error = " + immediateError);
                }

                if (deferredError > 0.022)
                {
                    Assert.True(false, deferredPath + ": Error = " + deferredError);
                }
            }
        }

        protected void CompareImagesNoRenderer([CallerMemberName] string testName = "")
        {
            var expectedPath = Path.Combine(OutputPath, testName + ".expected.png");
            var actualPath = Path.Combine(OutputPath, testName + ".out.png");

            using (var expected = new MagickImage(expectedPath))
            using (var actual = new MagickImage(actualPath))
            {
                double immediateError = expected.Compare(actual, ErrorMetric.RootMeanSquared);

                if (immediateError > 0.022)
                {
                    Assert.True(false, actualPath + ": Error = " + immediateError);
                }
            }
        }

        private string GetTestsDirectory()
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
                throw new NotImplementedException();
            }

            public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
            {
                throw new NotImplementedException();
            }
        }
    }
}
