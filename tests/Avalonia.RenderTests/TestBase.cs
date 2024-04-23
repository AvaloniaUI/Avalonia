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
    public class TestBase : IDisposable
    {
#if AVALONIA_SKIA
        private static string s_fontUri = "resm:Avalonia.Skia.RenderTests.Assets?assembly=Avalonia.Skia.RenderTests#Noto Mono";
#else
        private static string s_fontUri = "resm:Avalonia.Direct2D1.RenderTests.Assets?assembly=Avalonia.Direct2D1.RenderTests#Noto Mono";
#endif
        public static FontFamily TestFontFamily = new FontFamily(s_fontUri);

#if AVALONIA_SKIA3
        // TODO: investigate why output is different.
        // Most likely we need to use new SKSamplingOptions API, as old filters are broken with SKBitmap.
        private const double AllowedError = 0.15;
#else
        private const double AllowedError = 0.022;
#endif

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

            TestRenderHelper.BeginTest();
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
            var compositedPath = Path.Combine(OutputPath, testName + ".composited.out.png");
            await TestRenderHelper.RenderToFile(target, immediatePath, true, dpi);
            await TestRenderHelper.RenderToFile(target, compositedPath, false, dpi);
        }

        protected void CompareImages([CallerMemberName] string testName = "",
            bool skipImmediate = false,  bool skipCompositor = false)
        {
            var expectedPath = Path.Combine(OutputPath, testName + ".expected.png");
            var immediatePath = Path.Combine(OutputPath, testName + ".immediate.out.png");
            var compositedPath = Path.Combine(OutputPath, testName + ".composited.out.png");

            using (var expected = Image.Load<Rgba32>(expectedPath))
            using (var immediate = skipImmediate ? null: Image.Load<Rgba32>(immediatePath))
            using (var composited = skipCompositor ? null : Image.Load<Rgba32>(compositedPath))
            {
                if (!skipImmediate)
                {
                    var immediateError = TestRenderHelper.CompareImages(immediate!, expected);
                    if (immediateError > AllowedError)
                    {
                        Assert.True(false, immediatePath + ": Error = " + immediateError);
                    }
                }

                if (!skipCompositor)
                {
                    var compositedError = TestRenderHelper.CompareImages(composited!, expected);
                    if (compositedError > AllowedError)
                    {
                        Assert.True(false, compositedPath + ": Error = " + compositedError);
                    }
                }
            }
        }

        protected void CompareImagesNoRenderer([CallerMemberName] string testName = "", string expectedName = null)
        {
            var expectedPath = Path.Combine(OutputPath, (expectedName ?? testName) + ".expected.png");
            var actualPath = Path.Combine(OutputPath, testName + ".out.png");
            TestRenderHelper.AssertCompareImages(actualPath, expectedPath);
        }
        
        private static string GetTestsDirectory() => TestRenderHelper.GetTestsDirectory();

        public void Dispose() => TestRenderHelper.EndTest();
    }
}
