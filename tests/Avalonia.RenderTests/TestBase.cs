// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.IO;
using System.Runtime.CompilerServices;
using ImageMagick;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Rendering;

using Xunit;

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

            string path = Path.Combine(OutputPath, testName + ".out.png");

            using (RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)target.Width,
                (int)target.Height))
            {
                Size size = new Size(target.Width, target.Height);
                target.Measure(size);
                target.Arrange(new Rect(size));
                bitmap.Render(target);
                bitmap.Save(path);
            }
        }

        protected void CompareImages([CallerMemberName] string testName = "")
        {
            string expectedPath = Path.Combine(OutputPath, testName + ".expected.png");
            string actualPath = Path.Combine(OutputPath, testName + ".out.png");
            using (MagickImage expected = new MagickImage(expectedPath))
            using (MagickImage actual = new MagickImage(actualPath))
            {
                double error = expected.Compare(actual, ErrorMetric.RootMeanSquared);

                if (error > 0.022)
                {
                    Assert.True(false, actualPath + ": Error = " + error);
                }
            }
        }
    }
}
