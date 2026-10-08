using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Xunit;

namespace Avalonia.Skia.RenderTests
{
    public class TestBase : IDisposable
    {
        private static string s_fontUri = "resm:Avalonia.Skia.RenderTests.Assets?assembly=Avalonia.Skia.RenderTests#Noto Mono";

        public static FontFamily TestFontFamily = new FontFamily(s_fontUri);

        private const double AllowedError = 0.022;

        public TestBase(string outputPath)
        {
            outputPath = outputPath.Replace('\\', Path.DirectorySeparatorChar);
            var testPath = GetTestsDirectory();
            var testFiles = Path.Combine(testPath, "TestFiles");
            OutputPath = Path.Combine(testFiles, "Skia", outputPath);

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

            using (var expected = TestRenderHelper.LoadImage(expectedPath))
            using (var immediate = skipImmediate ? null: TestRenderHelper.LoadImage(immediatePath))
            using (var composited = skipCompositor ? null : TestRenderHelper.LoadImage(compositedPath))
            {
                if (!skipImmediate)
                {
                    var immediateError = TestRenderHelper.CompareImages(immediate!, expected);
                    if (immediateError > AllowedError)
                    {
                        Assert.Fail(immediatePath + ": Error = " + immediateError);
                    }
                }

                if (!skipCompositor)
                {
                    var compositedError = TestRenderHelper.CompareImages(composited!, expected);
                    if (compositedError > AllowedError)
                    {
                        Assert.Fail(compositedPath + ": Error = " + compositedError);
                    }
                }
            }
        }

        protected void CompareImagesNoRenderer([CallerMemberName] string testName = "", string? expectedName = null)
        {
            var expectedPath = Path.Combine(OutputPath, (expectedName ?? testName) + ".expected.png");
            var actualPath = Path.Combine(OutputPath, testName + ".out.png");
            TestRenderHelper.AssertCompareImages(actualPath, expectedPath);
        }
        
        private static string GetTestsDirectory() => TestRenderHelper.GetTestsDirectory();

        public void Dispose() => TestRenderHelper.EndTest();
    }
}
