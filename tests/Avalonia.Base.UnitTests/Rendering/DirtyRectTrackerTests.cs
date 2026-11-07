using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Server;
using Moq;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class DirtyRectTrackerTests
{
    private static readonly List<LtrbRect> Buffer = new();

    private static List<LtrbRect> Collect(IDirtyRectTracker tracker)
    {
        Buffer.Clear();
        tracker.CollectWorkingSet(Buffer);
        return new List<LtrbRect>(Buffer);
    }

    private static IPlatformRenderInterface RegionPlatform()
    {
        var platform = new Mock<IPlatformRenderInterface>();
        platform.Setup(x => x.SupportsRegions).Returns(true);
        platform.Setup(x => x.CreateRegion()).Returns(() => Mock.Of<IPlatformRenderInterfaceRegion>());
        return platform.Object;
    }

    [Fact]
    public void Single_Working_Set_Is_The_Running_Union_And_Reflects_Later_Adds()
    {
        var tracker = new SingleDirtyRectTracker();
        Assert.Empty(Collect(tracker)); // nothing accumulated yet

        tracker.AddRect(new LtrbRect(10, 10, 20, 20));
        Assert.Equal(new[] { new LtrbRect(10, 10, 20, 20) }, Collect(tracker));

        // Mid-pass read is side-effect-free: a later add grows the same union, re-read sees it.
        tracker.AddRect(new LtrbRect(40, 40, 60, 60));
        Assert.Equal(new[] { new LtrbRect(10, 10, 60, 60) }, Collect(tracker));
        Assert.Equal(new[] { new LtrbRect(10, 10, 60, 60) }, Collect(tracker)); // repeatable

        tracker.Initialize(default);
        Assert.Empty(Collect(tracker));
    }

    [Fact]
    public void Multi_Working_Set_Reads_Raw_Regions_Without_Optimizing()
    {
        var tracker = new MultiDirtyRectTracker(RegionPlatform(), maxDirtyRects: 8, maxOverhead: 1000);
        tracker.Initialize(new LtrbRect(0, 0, 1000, 1000));
        Assert.Empty(Collect(tracker));

        tracker.AddRect(new LtrbRect(10, 10, 20, 20));
        tracker.AddRect(new LtrbRect(100, 100, 120, 120));
        var afterTwo = Collect(tracker);
        Assert.Equal(2, afterTwo.Count);
        Assert.Contains(new LtrbRect(10, 10, 20, 20), afterTwo);
        Assert.Contains(new LtrbRect(100, 100, 120, 120), afterTwo);

        // Reading must not freeze the region (GetUninflatedDirtyRegions sets _optimized and drops later
        // adds): a subsequent add is still reflected.
        tracker.AddRect(new LtrbRect(500, 500, 520, 520));
        Assert.Equal(3, Collect(tracker).Count);
    }

    [Fact]
    public void Multi_Working_Set_Merges_When_Over_Capacity()
    {
        var tracker = new MultiDirtyRectTracker(RegionPlatform(), maxDirtyRects: 2, maxOverhead: 100000);
        tracker.Initialize(new LtrbRect(0, 0, 1000, 1000));
        tracker.AddRect(new LtrbRect(0, 0, 10, 10));
        tracker.AddRect(new LtrbRect(100, 100, 110, 110));
        tracker.AddRect(new LtrbRect(200, 200, 210, 210));

        // Capture granularity deliberately matches render granularity: never more than the working array.
        Assert.True(Collect(tracker).Count <= 2);
    }

    [Fact]
    public void Region_Working_Set_Reads_Raw_List_And_Resets()
    {
        var tracker = new RegionDirtyRectTracker(RegionPlatform());
        Assert.Empty(Collect(tracker));

        tracker.AddRect(new LtrbRect(10, 10, 20, 20));
        tracker.AddRect(new LtrbRect(30, 30, 40, 40));
        Assert.Equal(2, Collect(tracker).Count);

        tracker.Initialize(default);
        Assert.Empty(Collect(tracker));
    }

    [Fact]
    public void Root_Trackers_Expose_An_Identity_Mapped_Working_Set()
    {
        var single = new SingleDirtyRectTracker();
        single.AddRect(new LtrbRect(10, 10, 20, 20));

        var ws = ((IDirtyRectCollector)single).GetWorkingSet();
        Assert.False(ws.IsEmpty);
        Assert.True(ws.Mapping.IsIdentity);

        var host = new List<LtrbRect>();
        ws.CollectHostSpace(host);
        Assert.Equal(new[] { new LtrbRect(10, 10, 20, 20) }, host);
    }

    [Theory]
    [InlineData(0.0, 0.0, 1.0, 1.0)]
    [InlineData(-30.0, -40.0, 2.0, 2.0)]
    [InlineData(5.0, -7.0, 1.5, 3.0)]
    public void Host_Space_Mapping_Matches_The_Cache_Proxy_And_Round_Trips(double ox, double oy, double sx, double sy)
    {
        var mapping = new DirtyRectSpaceMapping(new Vector(ox, oy), sx, sy);
        var host = new LtrbRect(10, 20, 50, 80);

        // Forward direction reproduces the cache collector proxy's per-corner formula exactly.
        var tracker = mapping.HostToTracker(host);
        Assert.Equal((host.Left + ox) * sx, tracker.Left, 6);
        Assert.Equal((host.Top + oy) * sy, tracker.Top, 6);
        Assert.Equal((host.Right + ox) * sx, tracker.Right, 6);
        Assert.Equal((host.Bottom + oy) * sy, tracker.Bottom, 6);

        // Inverse round-trips.
        var back = mapping.TrackerToHost(tracker);
        Assert.Equal(host.Left, back.Left, 6);
        Assert.Equal(host.Top, back.Top, 6);
        Assert.Equal(host.Right, back.Right, 6);
        Assert.Equal(host.Bottom, back.Bottom, 6);
    }

    [Fact]
    public void Host_Space_Mapping_Is_Unusable_Before_A_Cache_Has_A_Scale()
    {
        Assert.False(new DirtyRectSpaceMapping(default, 0, 0).IsUsable);
        Assert.True(DirtyRectSpaceMapping.Identity.IsUsable);
        Assert.True(DirtyRectSpaceMapping.Identity.IsIdentity);
    }
}
