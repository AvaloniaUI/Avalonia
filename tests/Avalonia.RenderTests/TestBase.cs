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

#if AVALONIA_CAIRO
using Avalonia.Cairo;
#elif AVALONIA_SKIA
using Avalonia.Skia;
#else
using Avalonia.Direct2D1;
#endif

#if AVALONIA_CAIRO
namespace Avalonia.Cairo.RenderTests
#elif AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests
#else
namespace Avalonia.Direct2D1.RenderTests
#endif
{
    public class TestBase
    {
        static TestBase()
        {
#if AVALONIA_CAIRO
            CairoPlatform.Initialize();
#elif AVALONIA_SKIA
            SkiaPlatform.Initialize();
#else
            Direct2D1Platform.Initialize();
#endif
        }

        public TestBase(string outputPath)
        {
#if AVALONIA_CAIRO
            string testFiles = Path.GetFullPath(@"..\..\tests\TestFiles\Cairo");
#elif AVALONIA_SKIA
            string testFiles = Path.GetFullPath(@"..\..\tests\TestFiles\Skia");
#else
            string testFiles = Path.GetFullPath(@"..\..\tests\TestFiles\Direct2D1");
#endif
            OutputPath = Path.Combine(testFiles, outputPath);
        }

        public string OutputPath
        {
            get;
        }

        protected void RenderToFile(Control target, [CallerMemberName] string testName = "")
        {
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            var immediatePath = Path.Combine(OutputPath, testName + ".immediate.out.png");
            var deferredPath = Path.Combine(OutputPath, testName + ".deferred.out.png");
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            using (RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)target.Width,
                (int)target.Height))
            {
                Size size = new Size(target.Width, target.Height);
                target.Measure(size);
                target.Arrange(new Rect(size));
                bitmap.Render(target);
                bitmap.Save(immediatePath);
            }

            using (var rtb = factory.CreateRenderTargetBitmap((int)target.Width, (int)target.Height, 96, 96))
            using (var renderer = new DeferredRenderer(target, rtb))
            {
                Size size = new Size(target.Width, target.Height);
                target.Measure(size);
                target.Arrange(new Rect(size));
                renderer.Render(target.Bounds);
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
    }
}
