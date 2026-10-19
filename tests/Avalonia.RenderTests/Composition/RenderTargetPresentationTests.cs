#if AVALONIA_SKIA
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.RenderTests;

public class RenderTargetPresentationTests : TestBase
{
    public RenderTargetPresentationTests() : base("Composition/RenderTargetPresentation") { }

    [Theory]
    [InlineData(SurfaceOrientation.Rotation90, false)]
    [InlineData(SurfaceOrientation.Rotation180, false)]
    [InlineData(SurfaceOrientation.Rotation270, false)]
    public void Rotated_Output_Matches_The_Unrotated_Scene(SurfaceOrientation orientation, bool regions)
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        var sceneSize = new PixelSize(200, 160);
        using var fixture = new Fixture(true, regions, 8, sceneSize, orientation);
        using var reference = new Fixture(false, regions, 8, sceneSize);
        var events = new DebugEvents();
        fixture.Renderer.CompositionTarget.Server.DebugEvents = events;
        var fullRenderCount = 0;

        for (var i = 0; i < 10; i++)
        {
            fixture.Rectangle.RenderTransform = new TranslateTransform(i * 3, 0);
            fixture.Rectangle.Fill = i % 2 == 0 ? Brushes.Red : Brushes.Blue;
            reference.Rectangle.RenderTransform = new TranslateTransform(i * 3, 0);
            reference.Rectangle.Fill = fixture.Rectangle.Fill;
            fixture.Pump();
            reference.Pump();
            AssertRotatedPixels(reference.Surface.Snapshot(), fixture.Surface.Snapshot(), sceneSize, orientation);
            if (i == 0)
                fullRenderCount = events.RenderedVisuals;
            else
                // Only scene dirty rectangles need to update the retained layer.
                Assert.InRange(events.RenderedVisuals, 1, fullRenderCount - 1);
        }
    }

    [Fact]
    public void Changing_Rotation_Repaints_The_Output()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        var size = new PixelSize(200, 160);
        using var fixture = new Fixture(true, false, 8, size);
        using var reference = new Fixture(false, false, 8, size);
        for (var i = 0; i < 9; i++)
        {
            fixture.Rectangle.Fill = i % 2 == 0 ? Brushes.Red : Brushes.Blue;
            reference.Rectangle.Fill = fixture.Rectangle.Fill;
            fixture.Surface.Orientation = i >= 3 && i < 6 ? SurfaceOrientation.Rotation180 : SurfaceOrientation.Rotation0;
            fixture.Pump();
            reference.Pump();
            AssertRotatedPixels(reference.Surface.Snapshot(), fixture.Surface.Snapshot(), size,
                fixture.Surface.Orientation);
        }
    }

    [Theory]
    [InlineData(SurfaceOrientation.Rotation90)]
    [InlineData(SurfaceOrientation.Rotation180)]
    [InlineData(SurfaceOrientation.Rotation270)]
    public void Dirty_Rect_Overlay_Rotates_With_The_Scene(SurfaceOrientation orientation)
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        var size = new PixelSize(200, 160);
        using var fixture = new Fixture(true, false, 8, size, orientation);
        using var reference = new Fixture(false, false, 8, size, orientation);
        fixture.Renderer.Diagnostics.DebugOverlays = RendererDebugOverlays.DirtyRects;
        fixture.Pump();
        reference.Pump();
        fixture.Rectangle.Fill = Brushes.Blue;
        reference.Rectangle.Fill = Brushes.Blue;
        fixture.Pump();
        reference.Pump();

        var scene = reference.Surface.Snapshot();
        var output = fixture.Surface.Snapshot();
        var dirtyRect = fixture.Renderer.CompositionTarget.Server.DirtyRects.CombinedRect.ToRect();
        var outputWidth = orientation == SurfaceOrientation.Rotation180 ? size.Width : size.Height;
        var overlayPixels = 0;
        for (var y = 0; y < size.Height; y++)
        for (var x = 0; x < size.Width; x++)
        {
            var (outputX, outputY) = RotatePixel(x, y, size, orientation);
            var offset = (outputY * outputWidth + outputX) * 4;
            if (!scene.AsSpan(offset, 4).SequenceEqual(output.AsSpan(offset, 4)))
            {
                Assert.True(dirtyRect.Contains(new Point(x, y)), $"Overlay painted outside the rotated dirty rectangle at {x},{y}.");
                overlayPixels++;
            }
        }
        Assert.True(overlayPixels > 0);

        fixture.Renderer.Diagnostics.DebugOverlays = RendererDebugOverlays.None;
        fixture.Pump();
        Assert.Equal(scene, fixture.Surface.Snapshot());
    }

    private static (int X, int Y) RotatePixel(int x, int y, PixelSize size, SurfaceOrientation orientation) => orientation switch
    {
        SurfaceOrientation.Rotation90 => (size.Height - 1 - y, x),
        SurfaceOrientation.Rotation180 => (size.Width - 1 - x, size.Height - 1 - y),
        SurfaceOrientation.Rotation270 => (y, size.Width - 1 - x),
        _ => (x, y)
    };

    private static void AssertRotatedPixels(byte[] scene, byte[] output, PixelSize size, SurfaceOrientation orientation)
    {
        var outputWidth = orientation is SurfaceOrientation.Rotation90 or SurfaceOrientation.Rotation270
            ? size.Height : size.Width;
        Assert.Equal(scene.Length, output.Length);
        for (var y = 0; y < size.Height; y++)
        for (var x = 0; x < size.Width; x++)
        {
            var (outputX, outputY) = RotatePixel(x, y, size, orientation);
            Assert.True(scene.AsSpan((y * size.Width + x) * 4, 4)
                    .SequenceEqual(output.AsSpan((outputY * outputWidth + outputX) * 4, 4)),
                $"Rotated pixel differs at scene position {x},{y}, output position {outputX},{outputY}.");
        }
    }

    private sealed class DebugEvents : ICompositionTargetDebugEvents
    {
        public int RenderedVisuals { get; set; }
        public int VisitedVisuals { get; set; }
        public void RectInvalidated(LtrbRect rect) { }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ManualRenderTimer _timer = new();
        private readonly IPlatformGraphics _gpu = MesaSoftwareRenderer.Gl;
        private readonly Compositor _compositor;
        private readonly TestRenderRoot _root;
        public readonly CompositingRenderer Renderer;
        public readonly SwapchainSurface Surface;
        public readonly Rectangle Rectangle;
        public readonly Rectangle OtherRectangle;

        public Fixture(bool direct, bool regions, int maxRects, PixelSize? sceneSize = null,
            SurfaceOrientation orientation = SurfaceOrientation.Rotation0)
        {
            var size = sceneSize ?? new PixelSize(200, 200);
            var outputSize = orientation is SurfaceOrientation.Rotation90 or SurfaceOrientation.Rotation270
                ? new PixelSize(size.Height, size.Width) : size;
            Surface = new SwapchainSurface(direct, outputSize) { Orientation = orientation };
            _compositor = new Compositor(RenderLoop.FromTimer(_timer), _gpu, true,
                new DispatcherCompositorScheduler(), true, Dispatcher.UIThread, new CompositionOptions
                {
                    UseRegionDirtyRectClipping = regions, MaxDirtyRects = maxRects
                });
            _root = new TestRenderRoot(1, null!);
            Renderer = new CompositingRenderer(_root, _compositor, () => new IPlatformRenderSurface[] { Surface });
            var canvas = new Canvas
            {
                Width = size.Width, Height = size.Height,
                Background = new SolidColorBrush(Color.FromArgb(80, 20, 40, 60)),
                Clip = new EllipseGeometry(new Rect(size.ToSize(1))),
                Children =
                {
                    (Rectangle = new Rectangle
                    {
                        Width = 40, Height = 40, RadiusX = 9, RadiusY = 9,
                        Fill = Brushes.Red, Opacity = 0.6,
                        [Canvas.LeftProperty] = 10, [Canvas.TopProperty] = 40
                    }),
                    (OtherRectangle = new Rectangle
                    {
                        Width = 25, Height = 30, Fill = Brushes.Green, Opacity = 0.7,
                        [Canvas.LeftProperty] = 130, [Canvas.TopProperty] = 90
                    })
                }
            };
            _root.Initialize(Renderer, canvas);
            Renderer.Start();
        }

        public void Pump()
        {
            Dispatcher.UIThread.RunJobs();
            _timer.TriggerTick();
        }

        public void Dispose()
        {
            _root.Child = null;
            Dispatcher.UIThread.RunJobs();
            Renderer.Dispose();
            var renderInterface = _compositor.Server.RenderInterface;
            using (renderInterface.EnsureCurrent())
                _compositor.Server.ResetAllGpuResources();
            if (!_gpu.UsesSharedContext)
                renderInterface.GpuContext?.Dispose();
            Surface.Dispose();
        }
    }

    private sealed class SwapchainSurface : IGlPlatformSurface, IDisposable
    {
        private readonly bool _direct;
        private readonly IWriteableBitmapImpl[] _bitmaps;
        private int _lastBuffer;
        public SurfaceOrientation Orientation;

        public SwapchainSurface(bool direct, PixelSize? size = null)
        {
            _direct = direct;
            var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
            _bitmaps = Enumerable.Range(0, 3).Select(_ => factory.CreateWriteableBitmap(
                size ?? new PixelSize(200, 200), new Vector(96, 96), PixelFormats.Rgba8888, AlphaFormat.Premul)).ToArray();
        }

        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context) => new Target(this, context);

        public byte[] Snapshot() => Snapshot(_lastBuffer);

        private byte[] Snapshot(int index)
        {
            using var fb = _bitmaps[index].Lock();
            var bytes = new byte[fb.RowBytes * fb.Size.Height];
            Marshal.Copy(fb.Address, bytes, 0, bytes.Length);
            return bytes;
        }

        public void Dispose()
        {
            foreach (var bitmap in _bitmaps)
                bitmap.Dispose();
        }

        private sealed class Target : IGlPlatformSurfaceRenderTarget
        {
            private static readonly int[] s_order = { 0, 1, 2, 0, 0, 2, 1 };
            private readonly SwapchainSurface _owner;
            private readonly IGlPlatformSurfaceRenderTarget[] _targets;
            private int _frame;

            public Target(SwapchainSurface owner, IGlContext context)
            {
                _owner = owner;
                _targets = owner._bitmaps.Select(bitmap =>
                    new FboReadbackGlPlatformSurface(bitmap, 1).CreateGlRenderTarget(context)).ToArray();
            }

            public RenderTargetProperties Properties => new()
            {
                IsSuitableForDirectRendering = _owner._direct,
                RequiredRotationForOrientation = _owner.Orientation
            };
            public PlatformRenderTargetState State => PlatformRenderTargetState.Ready;

            public IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo info)
            {
                var index = s_order[_frame++ % s_order.Length];
                return new Session(this, index, _targets[index].BeginDraw(info));
            }

            public void Dispose()
            {
                foreach (var target in _targets)
                    target.Dispose();
            }

            private sealed class Session : IGlPlatformSurfaceRenderingSession
            {
                private readonly Target _target;
                private readonly int _index;
                private readonly IGlPlatformSurfaceRenderingSession _inner;

                public Session(Target target, int index, IGlPlatformSurfaceRenderingSession inner)
                {
                    _target = target;
                    _index = index;
                    _inner = inner;
                }

                public IGlContext Context => _inner.Context;
                public PixelSize Size => _inner.Size;
                public double Scaling => _inner.Scaling;
                public bool IsYFlipped => _inner.IsYFlipped;

                public void Dispose()
                {
                    _inner.Dispose();
                    _target._owner._lastBuffer = _index;
                }
            }
        }
    }
}
#endif
