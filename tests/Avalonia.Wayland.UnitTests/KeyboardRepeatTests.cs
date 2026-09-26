using System;
using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.Wayland.Server;
using Avalonia.Wayland.Server.Persistent;
using Xunit;

namespace Avalonia.Wayland.UnitTests;

public class KeyboardRepeatTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Disabled_owner_does_not_repeat_after_key_release_or_leave(bool leave)
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow();
        window.Press();
        window.Enable(false);
        if (leave)
            window.Events.OnKeyboardLeave();
        else
            window.Release();
        window.DispatchPendingInput();
        window.Enable(true);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(window.Keys);
    }

    [Fact]
    public void Disabling_and_reenabling_owner_stops_repeat_without_release()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow();
        window.Press();
        window.Enable(false);
        window.Enable(true);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(window.Keys);
    }

    [Fact]
    public void Disabled_owner_does_not_receive_repeat_ticks()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow();
        window.Press();
        window.Enable(false);
        Dispatcher.UIThread.RunJobs();
        Assert.Single(window.Keys);
    }

    [Fact]
    public void Key_down_while_disabled_does_not_start_repeat()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow();
        window.Enable(false);
        window.Press();
        window.Enable(true);
        Dispatcher.UIThread.RunJobs();
        Assert.Empty(window.Keys);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Enabled_owner_repeats_until_release_or_leave(bool leave)
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow();
        window.Press();
        Dispatcher.UIThread.RunJobs();
        Assert.True(window.Keys.Count >= 2);
        if (leave)
            window.Events.OnKeyboardLeave();
        else
            window.Release();
        window.DispatchPendingInput();
        var count = window.Keys.Count;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(count, window.Keys.Count);
    }

    [Fact]
    public void Disposing_sink_stops_repeat()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow();
        window.Press();
        window.Dispose();
        Dispatcher.UIThread.RunJobs();
        Assert.Single(window.Keys);
    }

    private sealed class TestWindow : WindowBaseImpl
    {
        private readonly ManualRawEventGrouperDispatchQueue _queue;
        public List<RawKeyEventArgs> Keys { get; } = new();
        public IWSurfaceEventSink Events { get; }

        public TestWindow() : this(new ManualRawEventGrouperDispatchQueue()) { }

        private TestWindow(ManualRawEventGrouperDispatchQueue queue)
            : base(new WaylandWorker(queue).Client)
        {
            _queue = queue;
            SetInputRoot(new TestRoot());
            CurrentSink = new TestSink(this);
            Events = CurrentSink;
            Events.OnKeyRepeatInfo(30, 0);
            Input = args =>
            {
                if (args is RawKeyEventArgs key)
                    Keys.Add(key);
            };
        }

        public void Enable(bool enabled) => IsEnabled = enabled;
        public void Press()
        {
            Events.OnKeyDown(1, Key.O, RawInputModifiers.Control, PhysicalKey.O, "o");
            DispatchPendingInput();
        }
        public void Release() => Events.OnKeyUp(2, Key.O, RawInputModifiers.None, PhysicalKey.O, "o");
        public void DispatchPendingInput()
        {
            while (_queue.HasJobs)
                _queue.DispatchNext();
        }

        public override IPlatformRenderSurface[] Surfaces => Array.Empty<IPlatformRenderSurface>();
        public override Size MaxAutoSizeHint => new(800, 600);
        internal override WXdgShellSurfaceProxy? SurfaceProxy => null;
        public override void Show(bool activate, bool isDialog) { }
        public override IPopupImpl? CreatePopup() => null;

        private sealed class TestSink(TestWindow parent) : Sink(parent)
        {
            protected override void DisconnectFromSurface() { }
        }
    }
}
