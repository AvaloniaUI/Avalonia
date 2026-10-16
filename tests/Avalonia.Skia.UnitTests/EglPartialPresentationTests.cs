using System;
using System.Collections.Generic;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Moq;
using Xunit;
using static Avalonia.OpenGL.Egl.EglConsts;

namespace Avalonia.Skia.UnitTests;

public class EglPartialPresentationTests
{
    [Fact]
    public void Ages_Resolve_Synthetic_Handles_Without_Assuming_Buffer_Order()
    {
        var history = new EglBufferAgeTracker();
        var a = history.Acquire(0);
        history.Presented(a);
        var b = history.Acquire(0);
        history.Presented(b);
        Assert.Same(a, history.Acquire(2));
        Assert.Same(b, history.Acquire(1));
        history.Presented(a);
        Assert.Same(a, history.Acquire(1));
        Assert.Same(b, history.Acquire(2));
        Assert.NotSame(a, history.Acquire(0));
        Assert.NotSame(a, history.Acquire(17));
    }

    [Fact]
    public void Eviction_Invalidates_Only_Handles_Absent_From_The_Whole_Window()
    {
        var history = new EglBufferAgeTracker();
        var a = history.Acquire(0);
        history.Presented(a);
        var b = history.Acquire(0);
        for (var i = 0; i < 16; i++)
            history.Presented(b);
        Assert.False(a.IsValid);
        Assert.True(b.IsValid);
        history.Invalidate();
        Assert.False(b.IsValid);
    }

    [Theory]
    [InlineData(true, false, "swap-khr")]
    [InlineData(false, true, "swap-ext")]
    [InlineData(false, false, "swap")]
    public void Declares_Repair_Before_Swap_And_Presents_Only_Frame_Dirty_Rects(bool khr, bool ext, string swap)
    {
        var native = new Native();
        var retired = false;
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, true, khr, ext, () => retired = true);
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(200, 100));
        frame.SetDirtyRects(new[] { new PixelRect(10, 20, 5, 6) }, new[] { new PixelRect(0, 10, 80, 70) });
        Assert.Equal(new[] { "behavior", "age", "set-dirty-rects" }, native.Calls);
        Assert.Equal(new[] { 0, 20, 80, 70 }, native.BufferDirtyRects);
        Present(frame);
        Assert.Equal(swap, native.Calls[^1]);
        if (khr || ext)
            Assert.Equal(new[] { 10, 74, 5, 6 }, native.FrameDirtyRects);
        Assert.False(retired);
        native.Age = 1;
        Assert.Same(frame.Buffer, state.BeginDraw((IntPtr)2, new PixelSize(200, 100)).Buffer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Disposal_Without_Presentation_Skips_Swap_And_Retires_The_Surface(bool succeeded)
    {
        var native = new Native();
        var retired = false;
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, true, true, false, () => retired = true);
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        frame.SetDirtyRects(new[] { new PixelRect(0, 0, 100, 100) }, new[] { new PixelRect(0, 0, 100, 100) });
        if (succeeded)
            frame.Succeeded();
        frame.Dispose();
        Assert.True(retired);
        Assert.False(frame.Buffer.IsValid);
        Assert.False(frame.CanPresent);
        Assert.Throws<ObjectDisposedException>(() => Present(frame));
        Assert.DoesNotContain("swap-khr", native.Calls);
    }

    [Fact]
    public void Unsuccessful_Sessions_Cannot_Present()
    {
        var native = new Native();
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, false, true, false, () => { });
        using var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        Assert.False(frame.CanPresent);
        Assert.Throws<InvalidOperationException>(() => frame.Present());
        Assert.Equal(new[] { "age" }, native.Calls);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Uses_Regular_Swap_When_Dirty_Rects_Are_Not_Declared(bool khr, bool ext)
    {
        var native = new Native();
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, true, khr, ext, () => { });
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        frame.SetDirtyRects(new[] { new PixelRect(0, 0, 100, 100) }, new[] { new PixelRect(0, 0, 100, 100) });
        Assert.True(Present(frame));

        native.Calls.Clear();
        native.Age = 1;
        Assert.True(Present(state.BeginDraw((IntPtr)2, new PixelSize(100, 100))));
        Assert.Equal(new[] { "age", "swap" }, native.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Native_Failures_Invalidate_The_Buffer(bool failSwap)
    {
        var native = new Native();
        var retired = false;
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, true, true, false, () => retired = true);
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        native.FailSetDirtyRects = !failSwap;
        native.FailSwap = failSwap;
        if (failSwap)
        {
            frame.SetDirtyRects(new[] { new PixelRect(0, 0, 100, 100) }, new[] { new PixelRect(0, 0, 100, 100) });
            Assert.False(Present(frame));
        }
        else
            Assert.Throws<OpenGlException>(() => frame.SetDirtyRects(
                new[] { new PixelRect(0, 0, 100, 100) }, new[] { new PixelRect(0, 0, 100, 100) }));
        Assert.True(retired);
        Assert.False(frame.Buffer.IsValid);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public void Unsuccessful_Disposal_Without_A_Native_Dirty_Rect_Declaration_Only_Invalidates_History(bool partialUpdate, bool setDirtyRects)
    {
        var native = new Native();
        var retired = false;
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, partialUpdate, true, false,
            () => retired = true);
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        if (setDirtyRects)
            frame.SetDirtyRects(new[] { new PixelRect(0, 0, 10, 10) }, new[] { new PixelRect(0, 0, 10, 10) });
        frame.Dispose();
        frame.Dispose();
        Assert.False(retired);
        Assert.False(frame.Buffer.IsValid);
        Assert.DoesNotContain("swap-khr", native.Calls);
        native.Age = 1;
        Assert.NotSame(frame.Buffer, state.BeginDraw((IntPtr)2, new PixelSize(100, 100)).Buffer);
    }

    [Fact]
    public void Failed_Swap_Retires_The_Surface_Even_Without_Partial_Update()
    {
        var native = new Native { FailSwap = true, Error = EGL_BAD_NATIVE_WINDOW };
        var retired = false;
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, false, true, false,
            () => retired = true);
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        Assert.False(Present(frame));
        Assert.True(retired);
        Assert.False(frame.Buffer.IsValid);
    }

    [Fact]
    public void Unexpected_Swap_Errors_Still_Propagate()
    {
        var native = new Native { FailSwap = true, Error = EGL_CONTEXT_LOST };
        var retired = false;
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, false, true, false,
            () => retired = true);
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        var exception = Assert.Throws<OpenGlException>(() => Present(frame));
        Assert.Equal(EGL_CONTEXT_LOST, exception.ErrorCode);
        Assert.True(retired);
        Assert.False(frame.Buffer.IsValid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Repeated_Zero_Age_Prefers_A_Layer_But_Intermittent_Zero_Age_Does_Not(bool reusable)
    {
        var native = new Native();
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, false, true, false, () => { });
        for (var i = 0; i < 8; i++)
        {
            Assert.False(state.PreferRetainedLayer);
            native.Age = reusable && i % 2 == 1 ? 1 : 0;
            Assert.True(Present(state.BeginDraw((IntPtr)2, new PixelSize(100, 100))));
        }
        Assert.Equal(!reusable, state.PreferRetainedLayer);
    }

    [Theory]
    [InlineData(false, 7)]
    [InlineData(true, 7)]
    [InlineData(false, 8)]
    [InlineData(true, 8)]
    public void Surface_Changes_Reset_Zero_Age_Count_Without_Clearing_Layer_Preference(bool replaceSurface, int precedingFrames)
    {
        var native = new Native();
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, false, true, false, () => { });
        for (var i = 0; i < precedingFrames; i++)
            Assert.True(Present(state.BeginDraw((IntPtr)2, new PixelSize(100, 100))));
        Assert.Equal(precedingFrames == 8, state.PreferRetainedLayer);

        var surface = (IntPtr)(replaceSurface ? 3 : 2);
        var size = replaceSurface ? new PixelSize(100, 100) : new PixelSize(120, 100);
        for (var i = 0; i < 8; i++)
        {
            var frame = state.BeginDraw(surface, size);
            Assert.Equal(precedingFrames == 8, state.PreferRetainedLayer);
            Assert.True(Present(frame));
        }
        Assert.True(state.PreferRetainedLayer);

        state.Invalidate();
        native.Age = 1;
        Assert.True(Present(state.BeginDraw(surface, size)));
        Assert.True(state.PreferRetainedLayer);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Empty_Dirty_Rects_Use_A_Zero_Area_Rectangle_Instead_Of_The_Full_Surface(bool clippedAway)
    {
        var native = new Native();
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, true, true, false, () => { });
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        var dirtyRects = clippedAway ? new[] { new PixelRect(200, 200, 10, 10) } : Array.Empty<PixelRect>();
        frame.SetDirtyRects(dirtyRects, dirtyRects);
        Assert.True(Present(frame));
        Assert.Equal(new[] { 0, 0, 0, 0 }, native.BufferDirtyRects);
        Assert.Equal(new[] { 0, 0, 0, 0 }, native.FrameDirtyRects);
    }

    [Fact]
    public void Flipped_Sessions_Use_The_Matching_Dirty_Rect_Origin()
    {
        var native = new Native();
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, true, true, false, () => { });
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100), isYFlipped: true);
        frame.SetDirtyRects(new[] { new PixelRect(10, 20, 5, 6) }, new[] { new PixelRect(10, 20, 5, 6) });
        Assert.True(Present(frame));
        Assert.Equal(new[] { 10, 20, 5, 6 }, native.BufferDirtyRects);
        Assert.Equal(new[] { 10, 20, 5, 6 }, native.FrameDirtyRects);
    }

    [Theory]
    [InlineData(false, EGL_BUFFER_DESTROYED)]
    [InlineData(true, 0x3094)] // EGL_BUFFER_PRESERVED
    public void Skips_Dirty_Rect_Declaration_When_Unsupported_Or_Preserved(bool partialUpdate, int behavior)
    {
        var native = new Native { Behavior = behavior };
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, partialUpdate, false, false, () => { });
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        frame.SetDirtyRects(new[] { new PixelRect(0, 0, 10, 10) }, new[] { new PixelRect(0, 0, 10, 10) });
        Present(frame);
        Assert.DoesNotContain("set-dirty-rects", native.Calls);
    }

    [Fact]
    public void Rejects_Second_Dirty_Rect_Declaration_And_Invalidates_On_Resize()
    {
        var native = new Native();
        var state = new EglPartialPresentation(native.Interface, (IntPtr)1, true, true, false, () => { });
        var frame = state.BeginDraw((IntPtr)2, new PixelSize(100, 100));
        frame.SetDirtyRects(new[] { new PixelRect(0, 0, 100, 100) }, new[] { new PixelRect(0, 0, 100, 100) });
        Assert.Throws<InvalidOperationException>(() => frame.SetDirtyRects(Array.Empty<PixelRect>(), Array.Empty<PixelRect>()));
        Present(frame);
        native.Age = 1;
        var resized = state.BeginDraw((IntPtr)2, new PixelSize(120, 100));
        Assert.False(frame.Buffer.IsValid);
        Assert.NotSame(frame.Buffer, resized.Buffer);
    }

    [Fact]
    public void Converts_And_Clamps_At_The_Egl_Boundary()
    {
        int[] storage = Array.Empty<int>();
        var count = EglPartialPresentation.WriteRects(new[]
        {
            new PixelRect(-5, 90, 20, 30), new PixelRect(300, 300, 20, 20)
        }, new PixelSize(200, 100), ref storage);
        Assert.Equal(1, count);
        Assert.Equal(new[] { 0, 0, 15, 10 }, storage.AsSpan(0, count * 4).ToArray());
    }

    private static bool Present(EglPartialPresentation.Session frame)
    {
        using (frame)
        {
            frame.Succeeded();
            return frame.Present();
        }
    }

    private sealed class Native
    {
        private readonly Mock<IEglInterface> _mock = new(MockBehavior.Strict);
        public IEglInterface Interface => _mock.Object;
        public readonly List<string> Calls = new();
        public int Age;
        public int Behavior = EGL_BUFFER_DESTROYED;
        public bool FailSetDirtyRects;
        public bool FailSwap;
        public int Error = EGL_BAD_SURFACE;
        public int[]? BufferDirtyRects;
        public int[]? FrameDirtyRects;

        public Native()
        {
            _mock.Setup(x => x.QuerySurface((IntPtr)1, It.IsAny<IntPtr>(), It.IsAny<int>(), out It.Ref<int>.IsAny))
                .Returns(new QuerySurfaceCall(QuerySurface));
            _mock.Setup(x => x.SetDamageRegionKhr((IntPtr)1, It.IsAny<IntPtr>(), It.IsAny<int[]>(), It.IsAny<int>()))
                .Returns((IntPtr display, IntPtr surface, int[] rects, int count) =>
                {
                    Calls.Add("set-dirty-rects");
                    BufferDirtyRects = rects.AsSpan(0, count * 4).ToArray();
                    return FailSetDirtyRects ? 0u : 1u;
                });
            _mock.Setup(x => x.SwapBuffersWithDamageKhr((IntPtr)1, It.IsAny<IntPtr>(), It.IsAny<int[]>(), It.IsAny<int>()))
                .Returns((IntPtr display, IntPtr surface, int[] rects, int count) => SwapWithDirtyRects("swap-khr", rects, count));
            _mock.Setup(x => x.SwapBuffersWithDamageExt((IntPtr)1, It.IsAny<IntPtr>(), It.IsAny<int[]>(), It.IsAny<int>()))
                .Returns((IntPtr display, IntPtr surface, int[] rects, int count) => SwapWithDirtyRects("swap-ext", rects, count));
            _mock.Setup(x => x.SwapBuffersChecked((IntPtr)1, It.IsAny<IntPtr>())).Returns(() =>
                {
                    Calls.Add("swap");
                    return FailSwap ? 0u : 1u;
                });
            _mock.Setup(x => x.GetError()).Returns(() => Error);
        }

        private delegate uint QuerySurfaceCall(IntPtr display, IntPtr surface, int attribute, out int value);

        private uint QuerySurface(IntPtr display, IntPtr surface, int attribute, out int value)
        {
            Calls.Add(attribute == EGL_BUFFER_AGE_EXT ? "age" : "behavior");
            value = attribute == EGL_BUFFER_AGE_EXT ? Age : Behavior;
            return 1;
        }

        private uint SwapWithDirtyRects(string name, int[] rects, int count)
        {
            Calls.Add(name);
            FrameDirtyRects = rects.AsSpan(0, count * 4).ToArray();
            return FailSwap ? 0u : 1u;
        }
    }
}
