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

public class KeyRepeatTextInputTests
{
    [Fact]
    public void Repeated_character_key_produces_text_input()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow(handleKeys: false);

        window.Press(Key.A, PhysicalKey.A, "a");
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.Keys.Count >= 2);
        Assert.Equal(window.Keys.Count, window.Text.Count);
        Assert.All(window.Text, text => Assert.Equal("a", text));
    }

    [Fact]
    public void Handled_repeat_produces_no_text_input()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow(handleKeys: true);

        window.Press(Key.A, PhysicalKey.A, "a");
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.Keys.Count >= 2);
        Assert.Empty(window.Text);
    }

    [Fact]
    public void Repeated_key_without_symbol_produces_no_text_input()
    {
        if (!OperatingSystem.IsLinux())
            Assert.Skip("The Wayland worker uses Linux eventfd.");
        using var app = UnitTestApplication.Start(TestServices.RealFocus);
        using var window = new TestWindow(handleKeys: false);

        window.Press(Key.Left, PhysicalKey.ArrowLeft, null);
        Dispatcher.UIThread.RunJobs();

        Assert.True(window.Keys.Count >= 2);
        Assert.Empty(window.Text);
    }

    private sealed class TestWindow : WindowBaseImpl
    {
        private readonly ManualRawEventGrouperDispatchQueue _queue;
        private readonly IWSurfaceEventSink _events;
        public List<RawKeyEventArgs> Keys { get; } = new();
        public List<string> Text { get; } = new();

        public TestWindow(bool handleKeys) : this(new ManualRawEventGrouperDispatchQueue(), handleKeys) { }

        private TestWindow(ManualRawEventGrouperDispatchQueue queue, bool handleKeys)
            : base(new WaylandWorker(queue).Client)
        {
            _queue = queue;
            SetInputRoot(new TestRoot());
            CurrentSink = new TestSink(this);
            _events = CurrentSink;
            _events.OnKeyRepeatInfo(30, 0);
            Input = args =>
            {
                if (args is RawKeyEventArgs key)
                {
                    Keys.Add(key);
                    key.Handled = handleKeys;
                }
                else if (args is RawTextInputEventArgs text)
                    Text.Add(text.Text);
            };
        }

        public void Press(Key key, PhysicalKey physicalKey, string? keySymbol)
        {
            _events.OnKeyDown(1, key, RawInputModifiers.None, physicalKey, keySymbol);
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
