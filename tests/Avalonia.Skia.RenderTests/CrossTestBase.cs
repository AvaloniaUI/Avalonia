#nullable enable

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Skia.RenderTests;
using Avalonia.Skia.RenderTests.CrossUI;
using CrossUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests;
#else
namespace Avalonia.Direct2D1.RenderTests;
#endif

class CrossFactAttribute : FactAttribute
{
    
}

class CrossTheoryAttribute : TheoryAttribute
{
    
}

public class CrossTestBase : IDisposable
{
    private readonly string _groupName;
    public CrossTestBase(string groupName)
    {
        TestRenderHelper.BeginTest();
        _groupName = groupName;
    }

    protected void RenderAndCompare(CrossControl root, [CallerMemberName] string? testName = null, double dpi = 96)
    {
        ArgumentException.ThrowIfNullOrEmpty(testName, nameof(testName));

        var dir = Path.Combine(TestRenderHelper.GetTestsDirectory(), "TestFiles", "CrossTests", _groupName);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        var flavor =
#if AVALONIA_SKIA
            "skia";
#else
            "d2d";
#endif
        var pathBase = Path.Combine(dir, testName);
        var renderPath = pathBase + "." + flavor + ".out.png";
        var compareWith = pathBase + ".wpf.png";
        var control = new AvaloniaCrossControl(root);
        TestRenderHelper.RenderToFile(control, renderPath, false, dpi);

        TestRenderHelper.AssertCompareImages(renderPath, compareWith);
    }

    public void Dispose()
    {
        TestRenderHelper.EndTest();
    }
}
