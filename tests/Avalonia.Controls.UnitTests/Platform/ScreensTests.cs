using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;
#nullable enable

namespace Avalonia.Controls.UnitTests.Platform;

public class ScreensTests : ScopedTestBase
{
    [Fact]
    public void Should_Preserve_Old_Screens_On_Changes()
    {
        using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

        var screens = new TestScreens();
        var totalScreens = new HashSet<TestScreen>();

        Assert.Equal(0, screens.ScreenCount);
        Assert.Empty(screens.AllScreens);

        // Push 2 screens.
        screens.PushNewScreens([1, 2]);
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.Equal(2, screens.ScreenCount);
        totalScreens.Add(Assert.IsType<TestScreen>(screens.GetScreen(1)));
        totalScreens.Add(Assert.IsType<TestScreen>(screens.GetScreen(2)));

        // Push 3 screens, while removing one old.
        screens.PushNewScreens([2, 3, 4]);
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.Equal(3, screens.ScreenCount);
        Assert.Null(screens.GetScreen(1));
        totalScreens.Add(Assert.IsType<TestScreen>(screens.GetScreen(2)));
        totalScreens.Add(Assert.IsType<TestScreen>(screens.GetScreen(3)));
        totalScreens.Add(Assert.IsType<TestScreen>(screens.GetScreen(4)));

        Assert.Equal(3, screens.AllScreens.Count);
        Assert.Equal(3, screens.ScreenCount);
        Assert.Equal(4, totalScreens.Count);
        
        Assert.Collection(
            totalScreens,
            s1 => Assert.True(s1.Generation < 0), // this screen was removed.
            s2 => Assert.Equal(2, s2.Generation), // this screen survived first OnChange event, instance should be preserved.
            s3 => Assert.Equal(1, s3.Generation),
            s4 => Assert.Equal(1, s4.Generation));
    }

    [Fact]
    public void Should_Preserve_Old_Screens_On_Changes_Same_Instance()
    {
        using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

        var screens = new TestScreens();

        Assert.Equal(0, screens.ScreenCount);
        Assert.Empty(screens.AllScreens);

        screens.PushNewScreens([1]);
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var screen = screens.GetScreen(1);

        Assert.NotNull(screen);
        Assert.Equal(1, screen.Generation);
        Assert.Equal(new IntPtr(1), screen.TryGetPlatformHandle()!.Handle);

        screens.PushNewScreens([1]);
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.Equal(2, screen.Generation);
        Assert.Equal(new IntPtr(1), screen.TryGetPlatformHandle()!.Handle);
        Assert.Same(screens.GetScreen(1), screen);
    }

    [Fact]
    public void Should_Raise_Event_And_Update_Screens_On_Changed()
    {
        using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

        var hasChangedTimes = 0;
        var screens = new TestScreens();
        screens.Changed = () => hasChangedTimes += 1;

        Assert.Equal(0, screens.ScreenCount);
        Assert.Empty(screens.AllScreens);

        screens.PushNewScreens([1, 2]);
        screens.PushNewScreens([1, 2]); // OnChanged can be triggered multiple times by different events
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.Equal(2, screens.ScreenCount);
        Assert.NotEmpty(screens.AllScreens);

        Assert.Equal(1, hasChangedTimes);
    }

    [Fact]
    [UnconditionalSuppressMessage("Usage", "xUnit1031:Do not use blocking task operations in test method", Justification = "Explicit threading test")]
    public void Should_Raise_Event_When_Screen_Changed_From_Another_Thread()
    {
        using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

        Dispatcher.UIThread.VerifyAccess();
        var hasChangedTimes = 0;
        var screens = new TestScreens();
        screens.Changed = () =>
        {
            Dispatcher.UIThread.VerifyAccess();
            hasChangedTimes += 1;
        };

        ThreadRunHelper.RunOnDedicatedThread(() => screens.PushNewScreens([1, 2])).GetAwaiter().GetResult();
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.Equal(1, hasChangedTimes);
    }

    
    [Fact]
    public void Should_Trigger_Changed_When_Screen_Removed()
    {
        using var _ = UnitTestApplication.Start(TestServices.MockThreadingInterface);

        var screens = new TestScreens();
        screens.PushNewScreens([1, 2]);
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var hasChangedTimes = 0;
        var screen = screens.GetScreen(2);
        Assert.NotNull(screen);

        screens.Changed = () =>
        {
            Assert.True(screen.Generation < 0);
            hasChangedTimes += 1;
        };

        screens.PushNewScreens([1]);
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.Equal(1, hasChangedTimes);
    }
    
    private class TestScreens : ScreensBase<int, TestScreen>
    {
        private IReadOnlyList<int> _keys = [];
        private int _count;

        public void PushNewScreens(IReadOnlyList<int> keys)
        {
            _count = keys.Count;
            _keys = keys;
            OnChanged();
        }

        public TestScreen? GetScreen(int key) => TryGetScreen(key, out var screen) ? screen : null;

        protected override int GetScreenCount() => _count;

        protected override IReadOnlyList<int> GetAllScreenKeys() => _keys;

        protected override TestScreen CreateScreenFromKey(int key) => new(key);
        protected override void ScreenChanged(TestScreen screen) => screen.Generation++;
        protected override void ScreenRemoved(TestScreen screen) => screen.Generation = -1000;
    }

    public class TestScreen(int key) : PlatformScreen(new PlatformHandle(new IntPtr(key), "TestHandle"))
    {
        public int Generation { get; set; }
    }
}
