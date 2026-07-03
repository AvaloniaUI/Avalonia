#if AVALONIA_SKIA
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using SkiaSharp;
using Xunit;

namespace Avalonia.Skia.RenderTests;

/// <summary>
/// Verifies that replacing a resource inside a <see cref="Drawing"/> is reflected in what a
/// consuming control renders. The initial frame is intentionally ignored (covered elsewhere); only
/// the frame produced _after_ the change is compared against the reference image.
/// </summary>
public class DrawingContentTests()
    : TestBase(@"Media\DrawingContent")
{
    private sealed class FuncFramebufferSurface(Func<IFramebufferRenderTarget> cb) : IFramebufferPlatformSurface
    {
        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => cb();
    }

    [Fact]
    public void DrawingBrush_Reflects_Replaced_Inner_Brush()
    {
        var drawing = new GeometryDrawing
        {
            Brush = Brushes.Blue,
            Geometry = new RectangleGeometry(new Rect(0, 0, 100, 100))
        };

        var target = new Border
        {
            Width = 100,
            Height = 100,
            Background = new DrawingBrush(drawing) { Stretch = Stretch.Fill }
        };

        RenderChange(target, () => drawing.Brush = Brushes.Red);
        CompareImages(skipImmediate: true);
    }

    [Fact]
    public void DrawingImage_Reflects_Replaced_Inner_Brush()
    {
        var drawing = new GeometryDrawing
        {
            Brush = Brushes.Blue,
            Geometry = new RectangleGeometry(new Rect(0, 0, 100, 100))
        };

        var target = new Image
        {
            Width = 100,
            Height = 100,
            Stretch = Stretch.Fill,
            Source = new DrawingImage(drawing)
        };

        RenderChange(target, () => drawing.Brush = Brushes.Red);
        CompareImages(skipImmediate: true);
    }

    // Renders the target once, applies the change, renders again and writes the second frame to the
    // standard composited output path so it can be compared against the committed reference image.
    private void RenderChange(Control target, Action change, [CallerMemberName] string testName = "")
    {
        var timer = new ManualRenderTimer();
        var compositor = new Compositor(
            RenderLoop.FromTimer(timer),
            null,
            true,
            new DispatcherCompositorScheduler(), true, Dispatcher.UIThread);

        var root = new TestRenderRoot(1, null!);
        using var frameBuffer = new SKBitmap(100, 100, SKColorType.Rgba8888, SKAlphaType.Premul);

        var renderTarget = new FuncFramebufferRenderTarget(() => new LockedFramebuffer(
            frameBuffer.GetAddress(0, 0),
            new PixelSize(frameBuffer.Width, frameBuffer.Height),
            frameBuffer.RowBytes,
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Premul,
            null));

        using (var renderer = new CompositingRenderer(root, compositor, () => [new FuncFramebufferSurface(() => renderTarget)]))
        {
            root.Initialize(renderer, target);
            renderer.Start();
            Dispatcher.UIThread.RunJobs();
            timer.TriggerTick();

            change();
            Dispatcher.UIThread.RunJobs();
            timer.TriggerTick();
        }

        Directory.CreateDirectory(OutputPath);
        var path = Path.Combine(OutputPath, testName + ".composited.out.png");
        using var data = frameBuffer.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(path);
        data.SaveTo(file);
    }
}
#endif
