#if AVALONIA_SKIA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Skia.RenderTests;

public class FramebufferPresentationTests : TestBase
{
    public FramebufferPresentationTests() : base("Composition/FramebufferPresentation") { }

    [Theory]
    [InlineData(false, 8)]
    [InlineData(true, 8)]
    [InlineData(true, 0)]
    public void Reused_Framebuffers_Match_Full_Repaints(bool regions, int maxRects)
    {
        using var fixture = new Fixture(regions, maxRects);
        // The reference uses the retained layer, as framebuffers without partial presentation do.
        using var reference = new Fixture(regions, maxRects);
        reference.Surface.Partial = false;
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
            fixture.Pump();
            var actual = fixture.Surface.Snapshot();
            var fullArea = 200 * 200;
            var repairArea = fixture.Surface.LastRepair.Sum(r => r.Width * r.Height);
            var frameArea = fixture.Surface.LastFrame.Sum(r => r.Width * r.Height);
            trace.Add($"{i}: frame={string.Join(';', fixture.Surface.LastFrame)} " +
                      $"repair={string.Join(';', fixture.Surface.LastRepair)}");
            sawPartialRepair |= repairArea > 0 && repairArea < fullArea;
            sawHistoricalRepair |= repairArea > frameArea && repairArea < fullArea;

            reference.Pump();
            AssertSimilar(reference.Surface.Snapshot(), actual, i);
        }
        Assert.Equal(12, fixture.Surface.Presented);
        Assert.True(sawPartialRepair, string.Join(Environment.NewLine, trace));
        Assert.True(sawHistoricalRepair, string.Join(Environment.NewLine, trace));
    }

    // Skia's raster anti-aliasing can differ by one step depending on the clip.
    // Stale or unrepaired pixels differ far more.
    private static void AssertSimilar(byte[] expected, byte[] actual, int frame)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (var c = 0; c < expected.Length; c++)
            if (Math.Abs(expected[c] - actual[c]) > 1)
                Assert.Fail($"Frame {frame}: pixel {c / 4 % 200},{c / 4 / 200} is {actual[c]} instead of {expected[c]}");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly ManualRenderTimer _timer = new();
        private readonly TestRenderRoot _root;
        public readonly CompositingRenderer Renderer;
        public readonly FramebufferSwapchain Surface = new(new PixelSize(200, 200));
        public readonly Rectangle Rectangle;
        public readonly Rectangle OtherRectangle;

        public Fixture(bool regions, int maxRects)
        {
            var compositor = new Compositor(RenderLoop.FromTimer(_timer), null, true,
                new DispatcherCompositorScheduler(), true, Dispatcher.UIThread, new CompositionOptions
                {
                    UseRegionDirtyRectClipping = regions, MaxDirtyRects = maxRects
                });
            _root = new TestRenderRoot(1, null!);
            Renderer = new CompositingRenderer(_root, compositor, () => new IPlatformRenderSurface[] { Surface });
            var canvas = new Canvas
            {
                Width = 200, Height = 200,
                // No geometry clip: Skia's raster anti-aliased clip masks depend on the clip bounds.
                Background = new SolidColorBrush(Color.FromArgb(80, 20, 40, 60)),
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
            Surface.Dispose();
        }
    }

    // Three CPU buffers handed out in a non-sequential order, like a pool of shared-memory buffers.
    private sealed class FramebufferSwapchain(PixelSize size) : IFramebufferPlatformSurface, IDisposable
    {
        private static readonly int[] s_order = { 0, 1, 2, 0, 0, 2, 1 };
        private readonly PixelSize _size = size;
        private readonly int _stride = size.Width * 4;
        private readonly IntPtr[] _memory = Enumerable.Range(0, 3)
            .Select(_ => Marshal.AllocHGlobal(size.Width * 4 * size.Height)).ToArray();
        private readonly Buffer[] _buffers = { new(), new(), new() };
        private int _frame;
        private int _lastBuffer;
        public bool Partial = true;
        public int Presented;
        public PixelRect[] LastFrame = Array.Empty<PixelRect>();
        public PixelRect[] LastRepair = Array.Empty<PixelRect>();

        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => new Target(this);

        public byte[] Snapshot()
        {
            var bytes = new byte[_stride * _size.Height];
            Marshal.Copy(_memory[_lastBuffer], bytes, 0, bytes.Length);
            return bytes;
        }

        public void Dispose()
        {
            foreach (var memory in _memory)
                Marshal.FreeHGlobal(memory);
        }

        private sealed class Buffer : IPartialPresentationBuffer
        {
            public bool IsValid { get; set; } = true;
            public bool Equals(IPartialPresentationBuffer? other) => ReferenceEquals(this, other);
        }

        private sealed class Target(FramebufferSwapchain owner) : IFramebufferRenderTarget
        {
            public bool SupportsPartialPresentation => owner.Partial;
            public PlatformRenderTargetState State => PlatformRenderTargetState.Ready;

            public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo,
                out FramebufferLockProperties properties)
            {
                var index = owner.Partial ? s_order[owner._frame++ % s_order.Length] : 0;
                var session = new Session(owner, index);
                if (!owner.Partial)
                    session.Succeeded();
                properties = new FramebufferLockProperties { PartialPresentation = owner.Partial ? session : null };
                return new LockedFramebuffer(owner._memory[index], owner._size, owner._stride, new Vector(96, 96),
                    PixelFormats.Bgra8888, AlphaFormat.Premul, () =>
                    {
                        if (!session.IsSucceeded)
                        {
                            owner._buffers[index].IsValid = false;
                            return;
                        }
                        owner._lastBuffer = index;
                        owner.Presented++;
                    });
            }

            public void Dispose()
            {
                foreach (var buffer in owner._buffers)
                    buffer.IsValid = false;
            }
        }

        private sealed class Session(FramebufferSwapchain owner, int index) : IPartialPresentationFeature
        {
            public bool IsSucceeded { get; private set; }
            public IPartialPresentationBuffer Buffer => owner._buffers[index];

            public unsafe void SetDirtyRects(ReadOnlySpan<PixelRect> frameDirtyRects,
                ReadOnlySpan<PixelRect> bufferDirtyRects)
            {
                owner.LastFrame = frameDirtyRects.ToArray();
                owner.LastRepair = bufferDirtyRects.ToArray();

                // Simulate undefined contents within the declared repair region.
                var pixels = (uint*)owner._memory[index];
                foreach (var rect in bufferDirtyRects)
                    for (var y = rect.Y; y < rect.Bottom; y++)
                    for (var x = rect.X; x < rect.Right; x++)
                        pixels[y * owner._size.Width + x] = 0xFFFF00FF;
            }

            public void Succeeded() => IsSucceeded = true;
        }
    }
}
#endif
