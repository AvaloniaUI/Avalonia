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
            var glesPath = Path.Combine(OutputPath, testName + ".composited.gles.out.png");
            var vulkanPath = Path.Combine(OutputPath, testName + ".composited.vulkan.out.png");
            await TestRenderHelper.RenderToFile(target, immediatePath, true, dpi);
            await TestRenderHelper.RenderToFile(target, compositedPath, false, dpi);
            if (MesaSoftwareRenderer.GlEnabled)
                await TestRenderHelper.RenderCompositedToFile(target, glesPath, MesaSoftwareRenderer.Gl, dpi);
            if (MesaSoftwareRenderer.VulkanEnabled)
                await TestRenderHelper.RenderCompositedToFile(target, vulkanPath, MesaSoftwareRenderer.Vulkan, dpi);
        }

        /// <param name="testName">The base name of the compared images.</param>
        /// <param name="skipImmediate">Skips the immediate renderer output comparison.</param>
        /// <param name="skipCompositor">Skips all composited output comparisons.</param>
        /// <param name="skipGpu">
        /// Skips Mesa GPU output comparisons. For tests that render via a custom flow that only
        /// produces the regular composited output.
        /// </param>
        /// <param name="gpuAllowedError">
        /// Error tolerance override for Mesa GPU outputs, for tests with known minor
        /// rasterization differences between GPU and CPU rendering.
        /// </param>
        protected void CompareImages([CallerMemberName] string testName = "",
            bool skipImmediate = false, bool skipCompositor = false, bool skipGpu = false,
            double? gpuAllowedError = null)
        {
            var expectedPath = Path.Combine(OutputPath, testName + ".expected.png");

            using (var expected = TestRenderHelper.LoadImage(expectedPath))
            {
                void Compare(string outputType, double allowedError)
                {
                    var actualPath = Path.Combine(OutputPath, testName + "." + outputType + ".out.png");
                    using var actual = TestRenderHelper.LoadImage(actualPath);
                    var error = TestRenderHelper.CompareImages(actual, expected);
                    if (error > allowedError)
                    {
                        Assert.Fail(actualPath + ": Error = " + error);
                    }
                }

                if (!skipImmediate)
                    Compare("immediate", AllowedError);

                if (!skipCompositor)
                {
                    Compare("composited", AllowedError);
                    if (!skipGpu)
                    {
                        if (MesaSoftwareRenderer.GlEnabled)
                            Compare("composited.gles", gpuAllowedError ?? AllowedError);
                        if (MesaSoftwareRenderer.VulkanEnabled)
                            Compare("composited.vulkan", gpuAllowedError ?? AllowedError);
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
