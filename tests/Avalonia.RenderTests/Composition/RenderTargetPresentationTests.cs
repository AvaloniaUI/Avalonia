#if AVALONIA_SKIA
using System;
using System.Collections.Generic;
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
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia.RenderTests;

public class RenderTargetPresentationTests : TestBase
{
    public RenderTargetPresentationTests() : base("Composition/RenderTargetPresentation") { }

    [Theory]
    [InlineData(true, false, 8)]
    [InlineData(false, false, 8)]
    [InlineData(true, true, 8)]
    [InlineData(false, true, 8)]
    [InlineData(true, true, 0)]
    [InlineData(false, true, 0)]
    public void Reused_Buffers_Match_Full_Repaints(bool direct, bool regions, int maxRects)
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var fixture = new Fixture(direct, regions, maxRects);
        using var reference = new Fixture(direct, regions, maxRects);
        var sawPartialRepair = false;
        var sawHistoricalRepair = false;
        var trace = new List<string>();
        for (var i = 0; i < 12; i++)
        {
            fixture.Rectangle.RenderTransform = new TranslateTransform(i * 3, 0);
            fixture.Rectangle.Fill = i % 2 == 0 ? Brushes.Red : Brushes.Blue;
            fixture.OtherRectangle.Fill = i % 2 == 0 ? Brushes.Green : Brushes.Yellow;
            reference.Rectangle.RenderTransform = new TranslateTransform(i * 3, 0);
            reference.Rectangle.Fill = fixture.Rectangle.Fill;
            reference.OtherRectangle.Fill = fixture.OtherRectangle.Fill;
            reference.Surface.FreshBuffer = true;
            fixture.Pump();
            var actual = fixture.Surface.Snapshot();
            if (!regions)
                Assert.Single(fixture.Surface.LastRepair);
            var fullArea = 200 * 200;
            var repairArea = fixture.Surface.LastRepair.Sum(r => r.Width * r.Height);
            var frameArea = fixture.Surface.LastFrame.Sum(r => r.Width * r.Height);
            trace.Add($"{i}: frame={string.Join(';', fixture.Surface.LastFrame)} repair={string.Join(';', fixture.Surface.LastRepair)}");
            sawPartialRepair |= repairArea > 0 && repairArea < fullArea;
            sawHistoricalRepair |= repairArea > frameArea && repairArea < fullArea;

            // A separate target with unfamiliar identities supplies the full-repaint reference.
            reference.Pump();
            Assert.Equal(new[] { new PixelRect(0, 0, 200, 200) }, reference.Surface.LastRepair);
            Assert.Equal(reference.Surface.Snapshot(), actual);
        }
        Assert.True(sawPartialRepair, string.Join(Environment.NewLine, trace));
        Assert.True(sawHistoricalRepair, string.Join(Environment.NewLine, trace));
    }

    [Fact]
    public void Switching_To_A_Retained_Layer_Matches_Full_Repaints()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var fixture = new Fixture(true, false, 8);
        using var reference = new Fixture(false, false, 8);
        for (var i = 0; i < 8; i++)
        {
            fixture.Surface.PreferLayer = i >= 3;
            fixture.Rectangle.RenderTransform = new TranslateTransform(i * 3, 0);
            fixture.Rectangle.Fill = i % 2 == 0 ? Brushes.Red : Brushes.Blue;
            reference.Rectangle.RenderTransform = new TranslateTransform(i * 3, 0);
            reference.Rectangle.Fill = fixture.Rectangle.Fill;
            reference.Surface.FreshBuffer = true;
            fixture.Pump();
            reference.Pump();
            Assert.Equal(reference.Surface.Snapshot(), fixture.Surface.Snapshot());
        }
    }

    [Fact]
    public void Failure_Discards_On_Disposal_And_Retries_With_The_Full_Frame_Dirty()
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var fixture = new Fixture(true, true, 8);
        fixture.Pump();
        var presented = fixture.Surface.Presented;
        fixture.Surface.FailNextSetDirtyRects = true;
        fixture.Rectangle.Fill = Brushes.Blue;
        fixture.Pump();
        Assert.Equal(1, fixture.Surface.Discarded);
        Assert.Equal(presented, fixture.Surface.Presented);
        fixture.Pump();
        Assert.True(fixture.Surface.Presented > presented);
        Assert.Equal(new[] { new PixelRect(0, 0, 200, 200) }, fixture.Surface.LastFrame);
        Assert.Equal(new[] { new PixelRect(0, 0, 200, 200) }, fixture.Surface.LastRepair);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Skia_Forwards_Success_Only_When_Disposing_The_Drawing_Context(bool succeeded)
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var context = (IGlContext)MesaSoftwareRenderer.Gl.CreateContext();
        using var current = context.MakeCurrent();
        using var gpu = new GlSkiaGpu(context, null, false);
        using var surface = new SwapchainSurface(true);
        using var target = new SkiaGpuRenderTarget(gpu,
            gpu.TryCreateRenderTarget(new[] { surface })!);
        using (var drawing = target.CreateDrawingContext(
                   new(new PixelSize(200, 200), 1, CompositionTransparencyLevel.None), out _))
        {
            drawing.Clear(Colors.Red);
            if (succeeded)
                drawing.GetFeature<IPartialPresentationFeature>()!.Succeeded();
            Assert.Equal(0, surface.SuccessSignals);
            Assert.Equal(0, surface.Presented);
        }
        Assert.Equal(succeeded ? 1 : 0, surface.SuccessSignals);
        Assert.Equal(succeeded ? 1 : 0, surface.Presented);
        Assert.Equal(succeeded ? 0 : 1, surface.Discarded);
    }

    [Theory]
    [InlineData(RendererDebugOverlays.DirtyRects)]
    [InlineData(RendererDebugOverlays.Fps)]
    [InlineData(RendererDebugOverlays.LayoutTimeGraph)]
    [InlineData(RendererDebugOverlays.RenderTimeGraph)]
    public void Overlays_Use_A_Layer_And_Partial_Presentation_Resumes_With_Clean_Buffers(RendererDebugOverlays overlays)
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var fixture = new Fixture(true, false, 8);
        using var reference = new Fixture(true, false, 8);
        var events = new DebugEvents();
        fixture.Renderer.CompositionTarget.Server.DebugEvents = events;

        // Populate all three buffers before temporarily opting out of partial presentation.
        for (var i = 0; i < 3; i++)
        {
            fixture.Rectangle.Fill = i % 2 == 0 ? Brushes.Blue : Brushes.Red;
            reference.Rectangle.Fill = fixture.Rectangle.Fill;
            fixture.Pump();
            reference.Pump();
        }
        var declarations = fixture.Surface.DirtyRectDeclarations;
        fixture.Renderer.Diagnostics.DebugOverlays = overlays;
        fixture.Pump();
        var fullRenderCount = events.RenderedVisuals;
        for (var i = 0; i < 3; i++)
        {
            fixture.Rectangle.RenderTransform = new TranslateTransform((i + 1) * 3, 0);
            fixture.Pump();
            Assert.Equal(declarations, fixture.Surface.DirtyRectDeclarations);
            // Only scene dirty rectangles update the layer, even though the output is copied in full.
            Assert.InRange(events.RenderedVisuals, 1, fullRenderCount - 1);
        }

        fixture.Renderer.Diagnostics.DebugOverlays = RendererDebugOverlays.None;
        for (var i = 0; i < 4; i++)
        {
            fixture.Rectangle.Fill = i % 2 == 0 ? Brushes.Red : Brushes.Blue;
            reference.Rectangle.Fill = fixture.Rectangle.Fill;
            reference.Rectangle.RenderTransform = new TranslateTransform(9, 0);
            reference.Surface.FreshBuffer = true;
            fixture.Pump();
            reference.Pump();
            Assert.Equal(reference.Surface.Snapshot(), fixture.Surface.Snapshot());
        }
        Assert.True(fixture.Surface.DirtyRectDeclarations > declarations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Retained_Layer_Is_Presented_Without_New_Scene_Dirty_Rects(bool regions)
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        using var fixture = new Fixture(true, regions, 8);
        using var reference = new Fixture(false, regions, 8);
        // The overlay disables partial presentation. An empty tracker draws no overlay rectangles.
        fixture.Renderer.Diagnostics.DebugOverlays = RendererDebugOverlays.DirtyRects;
        var events = new DebugEvents();
        var target = fixture.Renderer.CompositionTarget.Server;
        target.DebugEvents = events;
        fixture.Pump();
        reference.Pump();
        var expected = reference.Surface.Snapshot();
        var presented = fixture.Surface.Presented;

        for (var i = 0; i < 3; i++)
        {
            Assert.True(target.DirtyRects.IsEmpty);
            events.RenderedVisuals = 0;
            target.RequestFullRedraw();
            fixture.Pump();

            Assert.Equal(++presented, fixture.Surface.Presented);
            Assert.Equal(0, fixture.Surface.DirtyRectDeclarations);
            Assert.Equal(0, events.RenderedVisuals);
            Assert.Equal(expected, fixture.Surface.Snapshot());
        }
    }

    [Theory]
    [InlineData(SurfaceOrientation.Rotation90, false, false)]
    [InlineData(SurfaceOrientation.Rotation180, false, false)]
    [InlineData(SurfaceOrientation.Rotation270, false, false)]
    [InlineData(SurfaceOrientation.Rotation90, true, false)]
    [InlineData(SurfaceOrientation.Rotation180, true, false)]
    [InlineData(SurfaceOrientation.Rotation270, true, false)]
    [InlineData(SurfaceOrientation.Rotation90, true, true)]
    [InlineData(SurfaceOrientation.Rotation180, true, true)]
    [InlineData(SurfaceOrientation.Rotation270, true, true)]
    public void Rotated_Output_Matches_The_Unrotated_Scene(SurfaceOrientation orientation, bool partial, bool regions)
    {
        if (!MesaSoftwareRenderer.GlEnabled)
            return;
        var sceneSize = new PixelSize(200, 160);
        using var fixture = new Fixture(true, regions, 8, sceneSize, orientation);
        using var reference = new Fixture(false, regions, 8, sceneSize);
        fixture.Surface.SupportsPartialPresentation = partial;
        var events = new DebugEvents();
        fixture.Renderer.CompositionTarget.Server.DebugEvents = events;
        var fullRenderCount = 0;
        var sawPartialCopy = false;

        for (var i = 0; i < 10; i++)
        {
            fixture.Rectangle.RenderTransform = new TranslateTransform(i * 3, 0);
            fixture.Rectangle.Fill = i % 2 == 0 ? Brushes.Red : Brushes.Blue;
            reference.Rectangle.RenderTransform = new TranslateTransform(i * 3, 0);
            reference.Rectangle.Fill = fixture.Rectangle.Fill;
            reference.Surface.FreshBuffer = true;
            fixture.Pump();
            reference.Pump();
            AssertRotatedPixels(reference.Surface.Snapshot(), fixture.Surface.Snapshot(), sceneSize, orientation);
            if (i == 0)
                fullRenderCount = events.RenderedVisuals;
            else
                // Buffer reuse must not make the scene render outside its own dirty rectangles.
                Assert.InRange(events.RenderedVisuals, 1, fullRenderCount - 1);
            if (partial)
            {
                if (!regions)
                    Assert.Single(fixture.Surface.LastRepair);
                sawPartialCopy |= fixture.Surface.LastRepair.Sum(r => r.Width * r.Height)
                                  < sceneSize.Width * sceneSize.Height;
            }
        }
        Assert.Equal(partial, sawPartialCopy);
        Assert.Equal(partial ? fixture.Surface.Presented : 0, fixture.Surface.DirtyRectDeclarations);
    }

    [Fact]
    public void Changing_Rotation_Discards_Previous_Buffer_History()
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
            reference.Surface.FreshBuffer = true;
            fixture.Surface.Orientation = i >= 3 && i < 6 ? SurfaceOrientation.Rotation180 : SurfaceOrientation.Rotation0;
            fixture.Pump();
            reference.Pump();
            if (i is 3 or 6)
                Assert.Equal(new[] { new PixelRect(size) }, fixture.Surface.LastFrame);
            AssertRotatedPixels(reference.Surface.Snapshot(), fixture.Surface.Snapshot(), size,
                fixture.Surface.Orientation);
        }
    }

    [Theory]
    [InlineData(SurfaceOrientation.Rotation90)]
    [InlineData(SurfaceOrientation.Rotation180)]
    [InlineData(SurfaceOrientation.Rotation270)]
    public void Dirty_Rect_Overlay_Rotates_With_The_Scene_And_Disables_Partial_Presentation(SurfaceOrientation orientation)
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
        Assert.Equal(0, fixture.Surface.DirtyRectDeclarations);

        fixture.Renderer.Diagnostics.DebugOverlays = RendererDebugOverlays.None;
        fixture.Pump();
        Assert.Equal(1, fixture.Surface.DirtyRectDeclarations);
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
        public bool FreshBuffer;
        public bool PreferLayer;
        public bool FailNextSetDirtyRects;
        public bool SupportsPartialPresentation = true;
        public SurfaceOrientation Orientation;
        public int DirtyRectDeclarations;
        public int Presented;
        public int Discarded;
        public int SuccessSignals;
        public PixelRect[] LastFrame = Array.Empty<PixelRect>();
        public PixelRect[] LastRepair = Array.Empty<PixelRect>();

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

        private sealed class Buffer : IPartialPresentationBuffer
        {
            public bool IsValid { get; set; } = true;
            public bool Equals(IPartialPresentationBuffer? other) => ReferenceEquals(this, other);
        }

        private sealed class Target : IGlPlatformSurfaceRenderTarget
        {
            private static readonly int[] s_order = { 0, 1, 2, 0, 0, 2, 1 };
            private readonly SwapchainSurface _owner;
            private readonly IGlPlatformSurfaceRenderTarget[] _targets;
            private readonly Buffer[] _buffers = { new(), new(), new() };
            private int _frame;
            private bool _corrupted;

            public Target(SwapchainSurface owner, IGlContext context)
            {
                _owner = owner;
                _targets = owner._bitmaps.Select(bitmap =>
                    new FboReadbackGlPlatformSurface(bitmap, 1).CreateGlRenderTarget(context)).ToArray();
            }

            public RenderTargetProperties Properties => new()
            {
                SupportsPartialPresentation = _owner.SupportsPartialPresentation,
                IsSuitableForDirectRendering = _owner._direct && !_owner.PreferLayer,
                RequiresRotationForOrientation = _owner.Orientation
            };
            public PlatformRenderTargetState State => _corrupted ? PlatformRenderTargetState.Corrupted : PlatformRenderTargetState.Ready;

            public IGlPlatformSurfaceRenderingSession BeginDraw(IRenderTarget.RenderTargetSceneInfo info)
            {
                var index = s_order[_frame++ % s_order.Length];
                if (_owner.FreshBuffer)
                {
                    _owner.FreshBuffer = false;
                    _buffers[index].IsValid = false;
                    _buffers[index] = new Buffer();
                }
                return new Session(this, index, _targets[index].BeginDraw(info));
            }

            public void Dispose()
            {
                foreach (var buffer in _buffers)
                    buffer.IsValid = false;
                foreach (var target in _targets)
                    target.Dispose();
            }

            private sealed class Session : IGlPlatformSurfaceRenderingSessionWithPartialPresentation, IPartialPresentationFeature
            {
                private readonly Target _target;
                private readonly int _index;
                private readonly IGlPlatformSurfaceRenderingSession _inner;
                private bool _succeeded;
                private byte[]? _before;
                private PixelRect[] _repair = Array.Empty<PixelRect>();

                public Session(Target target, int index, IGlPlatformSurfaceRenderingSession inner)
                {
                    _target = target;
                    _index = index;
                    _inner = inner;
                    _succeeded = !target._owner.SupportsPartialPresentation;
                }

                public IPartialPresentationFeature? PartialPresentation => _target._owner.SupportsPartialPresentation ? this : null;
                public IPartialPresentationBuffer Buffer => _target._buffers[_index];
                public IGlContext Context => _inner.Context;
                public PixelSize Size => _inner.Size;
                public double Scaling => _inner.Scaling;
                public bool IsYFlipped => _inner.IsYFlipped;

                public unsafe void SetDirtyRects(ReadOnlySpan<PixelRect> frameDirtyRects, ReadOnlySpan<PixelRect> bufferDirtyRects)
                {
                    if (_target._owner.FailNextSetDirtyRects)
                    {
                        _target._owner.FailNextSetDirtyRects = false;
                        throw new InvalidOperationException("Test SetDirtyRects failure");
                    }
                    _target._owner.DirtyRectDeclarations++;
                    _target._owner.LastFrame = frameDirtyRects.ToArray();
                    _target._owner.LastRepair = bufferDirtyRects.ToArray();
                    _repair = bufferDirtyRects.ToArray();
                    _before = _target._owner.Snapshot(_index);

                    // Simulate undefined contents within the declared repair region.
                    var gl = Context.GlInterface;
                    var scissor = (delegate* unmanaged[Stdcall]<int, int, int, int, void>)gl.GetProcAddress("glScissor");
                    gl.Enable(GL_SCISSOR_TEST);
                    gl.ClearColor(1, 0, 1, 1);
                    foreach (var rect in bufferDirtyRects)
                    {
                        scissor(rect.X, rect.Y, rect.Width, rect.Height);
                        gl.Clear(GL_COLOR_BUFFER_BIT);
                    }
                    gl.Disable(GL_SCISSOR_TEST);
                }

                public void Succeeded()
                {
                    _succeeded = true;
                    _target._owner.SuccessSignals++;
                }

                public void Dispose()
                {
                    _inner.Dispose();
                    if (!_succeeded)
                    {
                        _target._corrupted = true;
                        _target._owner.Discarded++;
                        foreach (var buffer in _target._buffers)
                            buffer.IsValid = false;
                    }
                    else
                    {
                        _target._owner._lastBuffer = _index;
                        _target._owner.Presented++;
                        if (_before != null)
                        {
                            var after = _target._owner.Snapshot();
                            using var fb = _target._owner._bitmaps[_index].Lock();
                            for (var y = 0; y < fb.Size.Height; y++)
                            for (var x = 0; x < fb.Size.Width; x++)
                            {
                                if (_repair.Any(r => r.Contains(new PixelPoint(x, y))))
                                    continue;
                                var offset = y * fb.RowBytes + x * 4;
                                Assert.True(_before.AsSpan(offset, 4).SequenceEqual(after.AsSpan(offset, 4)),
                                    $"Output outside declared buffer dirty rectangles changed at {x},{y}.");
                            }
                        }
                    }
                }
            }
        }
    }
}
#endif
