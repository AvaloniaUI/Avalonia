using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Server;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering;

public class CompositorBackdropInvalidationTests : CompositorTestsBase
{
    private static ImmutableBlurEffect Blur(double radius) => new(radius);

    private static CompositionBitmapCache MakeCache(Compositor compositor) =>
        new(compositor, new ServerCompositionBitmapCache(compositor.Server));

    private static CompositionRetainedBackdropEffectCacheMode MakeRetainedBackdrop(Compositor compositor) =>
        new(compositor, new ServerCompositionRetainedBackdropEffectCacheMode(compositor.Server));

    private static CompositionVolatileBackdropEffectCacheMode MakeVolatileBackdrop(Compositor compositor) =>
        new(compositor, new ServerCompositionVolatileBackdropEffectCacheMode(compositor.Server));

    // 0 => Default (null), 1 => retained, 2 => volatile
    private static CompositionBackdropEffectCacheMode? MakeBackdropCache(Compositor compositor, int selector) => selector switch
    {
        1 => MakeRetainedBackdrop(compositor),
        2 => MakeVolatileBackdrop(compositor),
        _ => null
    };

    // Brute-force recount of a node's "_backdropsInSubTree": backdrops strictly below the node that are
    // reachable without crossing a bitmap-cache boundary. A cache-enabled visual blocks its descendants'
    // backdrops but not its own.
    private static int ReachableBackdrops(ServerCompositionVisual node)
    {
        var count = 0;
        foreach (var child in node.Children!.List)
            count += CountFrom(child, false);
        return count;

        static int CountFrom(ServerCompositionVisual cur, bool blockedByCacheAbove)
        {
            var count = 0;
            if (!blockedByCacheAbove && cur.BackdropEffect != null)
                count++;
            var childBlocked = blockedByCacheAbove || cur.Cache != null;
            foreach (var child in cur.Children!.List)
                count += CountFrom(child, childBlocked);
            return count;
        }
    }

    [Fact]
    public void BackdropsInSubTree_Matches_BruteForce_Under_Random_Mutations()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        const int n = 10;
        var nodes = new CompositionContainerVisual[n];
        for (var i = 0; i < n; i++)
            nodes[i] = c.CreateContainerVisual();

        var parent = new Dictionary<CompositionContainerVisual, CompositionContainerVisual?>();
        foreach (var node in nodes)
            parent[node] = null;

        bool IsDescendant(CompositionContainerVisual? x, CompositionContainerVisual anc)
        {
            for (var p = x; p != null; p = parent[p])
                if (ReferenceEquals(p, anc))
                    return true;
            return false;
        }

        var rnd = new Random(12345);
        for (var step = 0; step < 150; step++)
        {
            var a = nodes[rnd.Next(n)];
            switch (rnd.Next(5))
            {
                case 0:
                case 1:
                    // Move `a` under a random target (or detach when target is null).
                    var target = rnd.Next(n + 1) == n ? null : nodes[rnd.Next(n)];
                    if (target != null && (ReferenceEquals(target, a) || IsDescendant(target, a)))
                        break;
                    if (parent[a] != null)
                        parent[a]!.Children.Remove(a);
                    parent[a] = null;
                    if (target != null)
                    {
                        target.Children.Add(a);
                        parent[a] = target;
                    }
                    break;
                case 2:
                    a.BackdropEffect = a.BackdropEffect == null ? Blur(5) : null;
                    break;
                case 3:
                    a.CacheMode = a.CacheMode == null ? MakeCache(c) : null;
                    break;
                case 4:
                    a.BackdropEffectCache = MakeBackdropCache(c, rnd.Next(3));
                    break;
            }

            s.RunJobs();

            foreach (var node in nodes)
                Assert.Equal(ReachableBackdrops(node.Server), node.Server._backdropsInSubTree);
        }
    }

    [Fact]
    public void Counter_Stops_At_Cache_Boundary_And_Reflows_On_Toggle()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var root = c.CreateContainerVisual();
        var mid = c.CreateContainerVisual();
        var leaf = c.CreateContainerVisual();
        root.Children.Add(mid);
        mid.Children.Add(leaf);
        leaf.BackdropEffect = Blur(5);
        s.RunJobs();

        // No cache: the backdrop propagates all the way up.
        Assert.Equal(1, mid.Server._backdropsInSubTree);
        Assert.Equal(1, root.Server._backdropsInSubTree);

        // Enabling a cache on `mid` turns it into a boundary: the leaf backdrop stops at `mid`.
        mid.CacheMode = MakeCache(c);
        s.RunJobs();
        Assert.Equal(1, mid.Server._backdropsInSubTree);
        Assert.Equal(0, root.Server._backdropsInSubTree);

        // Disabling the cache reflows the count above the (former) boundary.
        mid.CacheMode = null;
        s.RunJobs();
        Assert.Equal(1, mid.Server._backdropsInSubTree);
        Assert.Equal(1, root.Server._backdropsInSubTree);
    }

    [Fact]
    public void Own_Backdrop_Crosses_Own_Cache_Boundary()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var root = c.CreateContainerVisual();
        var cached = c.CreateContainerVisual();
        root.Children.Add(cached);
        cached.CacheMode = MakeCache(c);
        cached.BackdropEffect = Blur(5); // the cache host is itself a backdrop
        s.RunJobs();

        // A visual's own backdrop samples its ancestor host, so it still contributes to its parent
        // even though the visual is a cache boundary for its descendants.
        Assert.Equal(1, root.Server._backdropsInSubTree);
        Assert.Equal(0, cached.Server._backdropsInSubTree);
    }

    [Fact]
    public void Registration_Is_Order_Independent_Effect_Before_Root()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.BackdropEffect = Blur(5);
        s.RunJobs();

        // Effect set while detached: no registration yet (no target).
        Assert.Null(v.Server.BackdropState);

        ElementComposition.SetElementChildVisual(s.TopLevel, v);
        s.RunJobs();

        // Once rooted, it registers with the target.
        Assert.NotNull(v.Server.BackdropState);
        Assert.Contains(v.Server, s.Renderer.CompositionTarget.Server.BackdropVisuals);
    }

    [Fact]
    public void Detach_Clears_Registration()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.BackdropEffect = Blur(5);
        ElementComposition.SetElementChildVisual(s.TopLevel, v);
        s.RunJobs();
        Assert.NotNull(v.Server.BackdropState);

        ElementComposition.SetElementChildVisual(s.TopLevel, null);
        s.RunJobs();

        Assert.Null(v.Server.BackdropState);
        Assert.DoesNotContain(v.Server, s.Renderer.CompositionTarget.Server.BackdropVisuals);
    }

    [Fact]
    public void Clearing_Effect_Unregisters()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.BackdropEffect = Blur(5);
        ElementComposition.SetElementChildVisual(s.TopLevel, v);
        s.RunJobs();
        Assert.NotNull(v.Server.BackdropState);

        v.BackdropEffect = null;
        s.RunJobs();
        Assert.Null(v.Server.BackdropState);
        Assert.DoesNotContain(v.Server, s.Renderer.CompositionTarget.Server.BackdropVisuals);
    }

    [Fact]
    public void DropShadow_Backdrop_Registers_Nothing()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var root = c.CreateContainerVisual();
        var leaf = c.CreateSolidColorVisual();
        leaf.Size = new Vector(50, 50);
        root.Children.Add(leaf);
        ElementComposition.SetElementChildVisual(s.TopLevel, root);
        leaf.BackdropEffect = new ImmutableDropShadowEffect(0, 0, 10, Colors.Black, 1);
        s.RunJobs();
        s.RunJobs();

        // A drop-shadow is not a meaningful backdrop: no registration, no counter contribution, no draw.
        Assert.Null(leaf.Server.BackdropState);
        Assert.DoesNotContain(leaf.Server, s.Renderer.CompositionTarget.Server.BackdropVisuals);
        Assert.Equal(0, root.Server._backdropsInSubTree);
    }

    [Fact]
    public void Registry_Computes_Device_Space_Aabb_At_Root_Host()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.Offset = new Vector3D(100, 100, 0);
        v.Color = Colors.Red;
        v.BackdropEffect = Blur(5);
        ElementComposition.SetElementChildVisual(s.TopLevel, v);
        // A couple of frames: the registry uses the previous walk's bounds, so the AABB lands a frame later.
        s.RunJobs();
        s.RunJobs();

        var record = v.Server.BackdropState;
        Assert.NotNull(record);
        Assert.Null(record!.Host); // root host
        Assert.Equal(new LtrbPixelRect(100, 100, 150, 150), record.Aabb!.Value);
    }

    // cacheSelector: 0 => Default (null), 1 => retained, 2 => volatile
    [Theory]
    [InlineData(5.0, 0, true)]
    [InlineData(0.0, 0, false)]
    [InlineData(0.0, 1, true)]
    [InlineData(5.0, 2, false)]
    public void Registry_Classifies_Retained_Vs_Volatile(double radius, int cacheSelector, bool expectedRetained)
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.BackdropEffect = Blur(radius);
        v.BackdropEffectCache = MakeBackdropCache(c, cacheSelector);
        host.Children.Add(v);
        s.RunJobs();
        s.RunJobs();

        var record = v.Server.BackdropState;
        Assert.NotNull(record);
        Assert.Equal(expectedRetained, record!.IsRetained);
    }

    [Fact]
    public void Host_Change_Resets_Retained_State_And_Emits_Damage()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        // A sized, offset ancestor so that (a) enabling a cache on it produces observable target-level
        // damage and (b) its device space differs from its cache-local space, exercising the host-space
        // conversion.
        var mid = c.CreateSolidColorVisual();
        mid.Size = new Vector(200, 200);
        mid.Offset = new Vector3D(10, 10, 0);
        mid.Color = Colors.Green;
        ElementComposition.SetElementChildVisual(s.TopLevel, mid);
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.Offset = new Vector3D(30, 40, 0);
        v.Color = Colors.Red;
        v.BackdropEffect = Blur(5);
        mid.Children.Add(v);
        s.RunJobs();
        s.RunJobs();

        var record = v.Server.BackdropState!;
        Assert.Null(record.Host); // root host initially
        // Root host space is device pixels: the ancestor's offset is folded in.
        Assert.Equal(new LtrbPixelRect(40, 50, 90, 100), record.Aabb!.Value);
        var sentinel = new ServerCompositionBackdropStore();
        record.RetainedState = sentinel;
        s.Events.Reset();

        // Enable a cache on the ancestor: the backdrop's host changes to that cache.
        mid.CacheMode = MakeCache(c);
        s.RunJobs();

        Assert.Same(mid.Server.Cache, record.Host);
        Assert.Null(record.RetainedState); // reset on host change
        // Cache host space is the cached visual's local space: the ancestor's own offset is NOT folded in.
        Assert.Equal(new LtrbPixelRect(30, 40, 80, 90), record.Aabb!.Value);
        Assert.NotEmpty(s.Events.Rects); // the change produced damage via the normal walk
    }

    [Fact]
    public void SubPixel_Move_Keeps_Aabb_And_Retained_State_But_Pixel_Move_Resets()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.Offset = new Vector3D(100.2, 100.2, 0);
        v.Color = Colors.Red;
        v.BackdropEffect = Blur(5);
        ElementComposition.SetElementChildVisual(s.TopLevel, v);
        s.RunJobs();
        s.RunJobs();
        s.RunJobs();

        var record = v.Server.BackdropState!;
        var aabb0 = record.Aabb;
        Assert.NotNull(aabb0);
        var sentinel = new ServerCompositionBackdropStore();
        record.RetainedState = sentinel;

        // Sub-pixel move that snaps to the same integral AABB: retained state must survive.
        v.Offset = new Vector3D(100.4, 100.4, 0);
        s.RunJobs();
        s.RunJobs();
        Assert.Equal(aabb0!.Value, record.Aabb!.Value);
        Assert.Same(sentinel, record.RetainedState);

        // A move that changes the snapped AABB resets the retained state.
        v.Offset = new Vector3D(101.6, 101.6, 0);
        s.RunJobs();
        s.RunJobs();
        Assert.NotEqual(aabb0.Value, record.Aabb!.Value);
        Assert.Null(record.RetainedState);
    }

    [Fact]
    public void Children_Changed_Flag_Raised_On_Collection_Change_When_Detached()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var parent = c.CreateContainerVisual();
        var child = c.CreateContainerVisual();
        parent.Children.Add(child);
        s.RunJobs();

        // Detached: the update walk never visits it, so the per-frame flag is not cleared.
        Assert.True(parent.Server._childrenChanged);
    }

    [Fact]
    public void Children_Changed_Flag_Cleared_By_Update_Walk_When_Attached()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);
        s.RunJobs();

        var child = c.CreateSolidColorVisual();
        child.Size = new Vector(20, 20);
        host.Children.Add(child);
        s.RunJobs();

        Assert.False(host.Server._childrenChanged);
    }

    // A container that does not clip its children (ClipToBounds defaults to true, which would clip a
    // zero-sized panel's children — and their damage — down to nothing).
    private static CompositionContainerVisual Panel(Compositor c)
    {
        var v = c.CreateContainerVisual();
        v.ClipToBounds = false;
        return v;
    }

    // A retained backdrop with a known radius: Blur(5) ⇒ R = ceil(5)+1 = 6 host-px at scaling 1.
    private CompositionSolidColorVisual RetainedBackdrop(Compositor c, Vector size)
    {
        var v = c.CreateSolidColorVisual();
        v.Size = size;
        v.Color = Colors.Red;
        v.BackdropEffect = Blur(5);
        return v;
    }

    private static CompositionContainerVisual ContainerBackdrop(Compositor c)
    {
        var v = Panel(c);
        v.BackdropEffect = Blur(5);
        return v;
    }

    [Fact]
    public void Change_Behind_Retained_Backdrop_Accumulates_Input_And_Emits_Inflated_Output()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Offset = new Vector3D(10, 10, 0);
        behind.Color = Colors.Blue;

        var backdrop = RetainedBackdrop(c, new Vector(100, 100));

        host.Children.Add(behind);   // earlier DFS = behind the backdrop
        host.Children.Add(backdrop); // later DFS = in front, samples `behind`
        s.RunJobs();
        s.RunJobs();

        var record = backdrop.Server.BackdropState!;
        Assert.True(record.IsRetained);
        Assert.Equal(new LtrbPixelRect(0, 0, 100, 100), record.Aabb!.Value);

        record.RetainedInputDirtyArea = null;
        s.Events.Rects.Clear();

        // Move `behind`: old∪new = (10,10)-(30,70); ∩ AABB is the same; halo = ⊕6 ∩ AABB = (4,4)-(36,76).
        behind.Offset = new Vector3D(10, 50, 0);
        s.AssertRects(
            new Rect(10, 10, 20, 20),  // behind old
            new Rect(10, 50, 20, 20),  // behind new
            new Rect(4, 4, 32, 72));   // ((D ∩ AABB) ⊕ R) ∩ AABB

        Assert.Equal(new LtrbRect(10, 10, 30, 70), record.RetainedInputDirtyArea!.Value);
    }

    [Fact]
    public void Change_In_Front_Leaves_Retained_Input_Untouched()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var backdrop = RetainedBackdrop(c, new Vector(100, 100));
        var front = c.CreateSolidColorVisual();
        front.Size = new Vector(20, 20);
        front.Offset = new Vector3D(10, 10, 0);
        front.Color = Colors.Blue;

        host.Children.Add(backdrop); // earlier DFS = behind
        host.Children.Add(front);    // later DFS = in front
        s.RunJobs();
        s.RunJobs();

        var record = backdrop.Server.BackdropState!;
        record.RetainedInputDirtyArea = null;
        s.Events.Rects.Clear();

        // The change is later in DFS than the backdrop's capture, so its input is untouched.
        front.Offset = new Vector3D(10, 50, 0);
        s.AssertRects(
            new Rect(10, 10, 20, 20),
            new Rect(10, 50, 20, 20));

        Assert.Null(record.RetainedInputDirtyArea);
    }

    [Fact]
    public void Dirty_Ancestor_Fully_Invalidates_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var effectParent = Panel(c);
        effectParent.Effect = Blur(3); // a covering effect ⇒ its subtree walks under a disabled region
        host.Children.Add(effectParent);

        var x = c.CreateSolidColorVisual();
        x.Size = new Vector(20, 20);
        x.Offset = new Vector3D(200, 200, 0);
        x.Color = Colors.Blue;

        var backdrop = RetainedBackdrop(c, new Vector(100, 100));
        effectParent.Children.Add(x);
        effectParent.Children.Add(backdrop);
        s.RunJobs();
        s.RunJobs();

        var record = backdrop.Server.BackdropState!;
        Assert.Equal(new LtrbPixelRect(0, 0, 100, 100), record.Aabb!.Value);
        record.RetainedInputDirtyArea = null;

        // A dirty-for-render ancestor only adds its covering rect in its own PostSubgraph, so the working set
        // doesn't contain it at the backdrop's encounter ⇒ the whole input is treated as dirty.
        x.Offset = new Vector3D(200, 250, 0);
        s.RunJobs();

        Assert.Equal(new LtrbRect(0, 0, 100, 100), record.RetainedInputDirtyArea!.Value);
    }

    [Fact]
    public void Child_Removal_Under_Ancestor_Fully_Invalidates_Via_Children_Changed_Blanket()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var p = Panel(c);
        host.Children.Add(p);

        var mover = c.CreateSolidColorVisual();
        mover.Size = new Vector(20, 20);
        mover.Offset = new Vector3D(500, 500, 0); // outside the backdrop AABB
        mover.Color = Colors.Green;

        var backdrop = RetainedBackdrop(c, new Vector(100, 100));

        var removable = c.CreateSolidColorVisual();
        removable.Size = new Vector(20, 20);
        removable.Offset = new Vector3D(200, 200, 0);
        removable.Color = Colors.Blue;

        p.Children.Add(mover);     // earlier DFS ⇒ forces descent, its damage is in the working set
        p.Children.Add(backdrop);
        p.Children.Add(removable); // removed later ⇒ extra dirty rect emitted in p's PostSubgraph
        s.RunJobs();
        s.RunJobs();

        var record = backdrop.Server.BackdropState!;
        record.RetainedInputDirtyArea = null;

        // `mover` is well outside the AABB (empty normal capture); removing `removable` sets p's
        // children-changed flag whose blanket covers the late extra-dirty-rect ordering hole ⇒ full input.
        mover.Offset = new Vector3D(500, 520, 0);
        p.Children.Remove(removable);
        s.RunJobs();

        Assert.Equal(new LtrbRect(0, 0, 100, 100), record.RetainedInputDirtyArea!.Value);
    }

    [Fact]
    public void Descent_Gate_Visits_Clean_Backdrop_Spine_Only_With_Damage()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var mover = c.CreateSolidColorVisual();
        mover.Size = new Vector(20, 20);
        mover.Offset = new Vector3D(10, 10, 0); // overlaps the backdrop AABB
        mover.Color = Colors.Green;

        var c1 = Panel(c);
        var c2 = Panel(c);
        var backdrop = RetainedBackdrop(c, new Vector(100, 100));

        host.Children.Add(mover); // earlier DFS
        host.Children.Add(c1);    // clean spine c1 → c2 → backdrop
        c1.Children.Add(c2);
        c2.Children.Add(backdrop);
        s.RunJobs();
        s.RunJobs();
        s.RunJobs();

        var record = backdrop.Server.BackdropState!;
        record.RetainedInputDirtyArea = null;

        // Idle frame: the clean spine is not descended, nothing is captured, the render is skipped.
        s.Events.Reset();
        s.RunJobs();
        Assert.Null(record.RetainedInputDirtyArea);
        Assert.Equal(0, s.Events.VisitedVisuals);

        // Damage frame: `mover` (earlier DFS, overlapping the AABB) changes ⇒ the descent gate forces the
        // walk down the clean spine and the backdrop captures.
        mover.Offset = new Vector3D(10, 15, 0);
        s.RunJobs();
        Assert.Equal(new LtrbRect(10, 10, 30, 35), record.RetainedInputDirtyArea!.Value);
    }

    [Fact]
    public void Stale_Aabb_Fully_Invalidates_Retained_Input_The_Same_Frame()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var backdrop = ContainerBackdrop(c);
        host.Children.Add(backdrop);

        var d = c.CreateSolidColorVisual();
        d.Size = new Vector(20, 20);
        d.Color = Colors.Red;
        backdrop.Children.Add(d);
        s.RunJobs();
        s.RunJobs();

        var record = backdrop.Server.BackdropState!;
        Assert.Equal(new LtrbPixelRect(0, 0, 20, 20), record.Aabb!.Value);
        record.RetainedInputDirtyArea = null;

        // A child of the backdrop resizes during the walk ⇒ FinalizeSubtreeBounds changes the backdrop's
        // bounds after the registry snapshot. The PostSubgraph stale-AABB check re-ingests the same frame.
        d.Size = new Vector(60, 60);
        s.RunJobs();

        Assert.Equal(new LtrbPixelRect(0, 0, 60, 60), record.Aabb!.Value);
        Assert.Equal(new LtrbRect(0, 0, 60, 60), record.RetainedInputDirtyArea!.Value);

        // In-place record update ⇒ the registry does not re-detect the change next frame (no spurious repaint).
        record.RetainedInputDirtyArea = null;
        s.RunJobs();
        Assert.Null(record.RetainedInputDirtyArea);
    }

    [Fact]
    public void Stale_Aabb_SubPixel_Child_Jitter_Does_Not_Invalidate()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var backdrop = ContainerBackdrop(c);
        host.Children.Add(backdrop);

        var d = c.CreateSolidColorVisual();
        d.Size = new Vector(20, 20);
        d.Offset = new Vector3D(0.2, 0.2, 0);
        d.Color = Colors.Red;
        backdrop.Children.Add(d);
        s.RunJobs();
        s.RunJobs();

        var record = backdrop.Server.BackdropState!;
        // (0.2,0.2)-(20.2,20.2) snaps out to (0,0,21,21).
        Assert.Equal(new LtrbPixelRect(0, 0, 21, 21), record.Aabb!.Value);
        record.RetainedInputDirtyArea = null;

        // Sub-pixel jitter that snaps to the same host AABB ⇒ the pixel-aligned stale check finds no change.
        d.Offset = new Vector3D(0.4, 0.4, 0);
        s.RunJobs();

        Assert.Equal(new LtrbPixelRect(0, 0, 21, 21), record.Aabb!.Value);
        Assert.Null(record.RetainedInputDirtyArea);
    }

    [Fact]
    public void Bounds_Change_Self_Invalidation_Emits_Backdrop_Old_And_New_Subtree_Bounds()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var backdrop = ContainerBackdrop(c);
        host.Children.Add(backdrop);

        // A static child keeps the backdrop's subtree bounds strictly larger than the moving child's rects.
        var stat = c.CreateSolidColorVisual();
        stat.Size = new Vector(20, 20);
        stat.Offset = new Vector3D(50, 50, 0);
        stat.Color = Colors.Green;
        backdrop.Children.Add(stat);

        var mover = c.CreateSolidColorVisual();
        mover.Size = new Vector(20, 20);
        mover.Color = Colors.Red;
        backdrop.Children.Add(mover);
        s.RunJobs();
        s.RunJobs();

        s.Events.Rects.Clear();

        // A descendant moves beyond the backdrop's old bounds. The backdrop visual itself is untouched, yet
        // its whole old∪new subtree bounds are emitted (forced dirty-for-render), not just the child's rects.
        mover.Offset = new Vector3D(0, 80, 0);
        s.AssertRects(
            new Rect(0, 0, 70, 70),   // old subtree bounds = union(stat, mover old)
            new Rect(0, 50, 70, 50)); // new subtree bounds = union(stat, mover new)
    }

    [Fact]
    public void InvalidateRetainedBackdrops_Marks_Only_Retained_In_The_Matching_Host()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var retainedVisual = RetainedBackdrop(c, new Vector(50, 50));

        var volatileVisual = c.CreateSolidColorVisual();
        volatileVisual.Size = new Vector(50, 50);
        volatileVisual.Offset = new Vector3D(100, 0, 0);
        volatileVisual.Color = Colors.Blue;
        volatileVisual.BackdropEffect = Blur(0); // R = 0 ⇒ volatile

        // A separate cache host, to prove host filtering.
        var cacheHost = Panel(c);
        cacheHost.Offset = new Vector3D(0, 200, 0);
        cacheHost.CacheMode = MakeCache(c);

        host.Children.Add(retainedVisual);
        host.Children.Add(volatileVisual);
        host.Children.Add(cacheHost);
        s.RunJobs();
        s.RunJobs();

        var retainedRecord = retainedVisual.Server.BackdropState!;
        var volatileRecord = volatileVisual.Server.BackdropState!;
        Assert.True(retainedRecord.IsRetained);
        Assert.False(volatileRecord.IsRetained);

        var target = s.Renderer.CompositionTarget.Server;

        retainedRecord.RetainedInputDirtyArea = null;
        volatileRecord.RetainedInputDirtyArea = null;
        target.InvalidateRetainedBackdrops(null); // root host

        Assert.Equal(new LtrbRect(0, 0, 50, 50), retainedRecord.RetainedInputDirtyArea!.Value);
        Assert.Null(volatileRecord.RetainedInputDirtyArea); // volatile is untouched by the retained bypass

        // A non-matching (cache) host marks nothing in the root host.
        retainedRecord.RetainedInputDirtyArea = null;
        target.InvalidateRetainedBackdrops(cacheHost.Server.Cache);
        Assert.Null(retainedRecord.RetainedInputDirtyArea);
    }

    // A volatile backdrop with a non-zero radius: Blur(5) + volatile ⇒ R = 6 host-px, classified volatile.
    private CompositionSolidColorVisual VolatileBackdrop(Compositor c, Vector size, Vector3D offset)
    {
        var v = c.CreateSolidColorVisual();
        v.Size = size;
        v.Offset = offset;
        v.Color = Colors.Blue;
        v.BackdropEffect = Blur(5);
        v.BackdropEffectCache = MakeVolatileBackdrop(c);
        return v;
    }

    [Fact]
    public void Volatile_Expansion_Emits_Whole_Aabb_And_Reaches_Later_Retained()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Offset = new Vector3D(200, 0, 0); // overlaps the volatile, not the retained
        behind.Color = Colors.Green;

        var vol = VolatileBackdrop(c, new Vector(300, 100), new Vector3D(0, 0, 0));
        var retained = RetainedBackdrop(c, new Vector(50, 50)); // later than `vol`, overlaps its AABB

        host.Children.Add(behind);   // earliest DFS
        host.Children.Add(vol);      // volatile, R > 0
        host.Children.Add(retained); // latest DFS ⇒ receives the volatile's expansion
        s.RunJobs();
        s.RunJobs();

        var volRecord = vol.Server.BackdropState!;
        var retRecord = retained.Server.BackdropState!;
        Assert.False(volRecord.IsRetained);
        Assert.True(retRecord.IsRetained);
        Assert.Equal(new LtrbPixelRect(0, 0, 300, 100), volRecord.Aabb!.Value);

        retRecord.RetainedInputDirtyArea = null;
        s.Events.Rects.Clear();

        // The change is outside the retained AABB, so the walk captures nothing for it. The volatile pass then
        // expands the whole volatile AABB and delivers it (∩ retained AABB) to the later retained visual.
        behind.Offset = new Vector3D(200, 50, 0);
        s.AssertRects(
            new Rect(200, 0, 20, 20),   // behind old
            new Rect(200, 50, 20, 20),  // behind new
            new Rect(0, 0, 300, 100),   // whole-visual invalidation of the volatile
            new Rect(0, 0, 50, 50));    // retained halo of the delivered damage (⊕6 clamped to its AABB)

        Assert.Equal(new LtrbRect(0, 0, 50, 50), retRecord.RetainedInputDirtyArea!.Value);
    }

    [Fact]
    public void Volatile_Behind_Volatile_Chain_Converges_To_Whole_Aabbs()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Color = Colors.Green;

        // A chain where each volatile only overlaps its immediate neighbour; the fixpoint must walk the chain.
        var v1 = VolatileBackdrop(c, new Vector(100, 100), new Vector3D(0, 0, 0));
        var v2 = VolatileBackdrop(c, new Vector(100, 100), new Vector3D(80, 0, 0));
        var v3 = VolatileBackdrop(c, new Vector(100, 100), new Vector3D(160, 0, 0));

        host.Children.Add(behind);
        host.Children.Add(v1);
        host.Children.Add(v2);
        host.Children.Add(v3);
        s.RunJobs();
        s.RunJobs();

        s.Events.Rects.Clear();

        // `behind` only touches v1; the fixpoint expands v1 → (reaches v2) → v2 → (reaches v3) → v3.
        behind.Offset = new Vector3D(0, 50, 0);
        s.AssertRects(
            new Rect(0, 0, 20, 20),
            new Rect(0, 50, 20, 20),
            new Rect(0, 0, 100, 100),     // v1
            new Rect(80, 0, 100, 100),    // v2
            new Rect(160, 0, 100, 100));  // v3
    }

    [Fact]
    public void Volatile_Fixpoint_Terminates_On_Mutually_Overlapping_Volatiles()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Color = Colors.Green;

        // v1 and v2 overlap each other: without the per-frame `expanded` marks the loop would never settle.
        var v1 = VolatileBackdrop(c, new Vector(100, 100), new Vector3D(0, 0, 0));
        var v2 = VolatileBackdrop(c, new Vector(100, 100), new Vector3D(50, 0, 0));

        host.Children.Add(behind);
        host.Children.Add(v1);
        host.Children.Add(v2);
        s.RunJobs();
        s.RunJobs();

        s.Events.Rects.Clear();

        behind.Offset = new Vector3D(0, 50, 0);
        s.AssertRects(
            new Rect(0, 0, 20, 20),
            new Rect(0, 50, 20, 20),
            new Rect(0, 0, 100, 100),   // v1
            new Rect(50, 0, 100, 100)); // v2 (each expands exactly once ⇒ the loop terminates)
    }

    [Fact]
    public void In_Front_Volatile_Does_Not_Dirty_Earlier_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var retained = RetainedBackdrop(c, new Vector(50, 50)); // earlier DFS = behind
        var vol = VolatileBackdrop(c, new Vector(50, 50), new Vector3D(100, 0, 0)); // later DFS = in front

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Offset = new Vector3D(100, 0, 0); // overlaps the volatile only
        behind.Color = Colors.Green;

        host.Children.Add(retained);
        host.Children.Add(vol);
        host.Children.Add(behind);
        s.RunJobs();
        s.RunJobs();

        var retRecord = retained.Server.BackdropState!;
        retRecord.RetainedInputDirtyArea = null;
        s.Events.Rects.Clear();

        // The volatile expands, but it is later in DFS than the retained visual, so delivery skips it.
        behind.Offset = new Vector3D(100, 20, 0);
        s.AssertRects(
            new Rect(100, 0, 20, 20),
            new Rect(100, 20, 20, 20),
            new Rect(100, 0, 50, 50)); // whole-visual invalidation of the volatile only

        Assert.Null(retRecord.RetainedInputDirtyArea);
    }

    [Fact]
    public void Volatile_Inside_Cache_Delivers_To_Later_Retained_In_Parent_Host()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        var cacheHost = Panel(c);
        cacheHost.CacheMode = MakeCache(c);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Color = Colors.Green;
        var vol = VolatileBackdrop(c, new Vector(100, 100), new Vector3D(0, 0, 0)); // inside the cache
        cacheHost.Children.Add(behind);
        cacheHost.Children.Add(vol);

        var retained = RetainedBackdrop(c, new Vector(50, 50)); // in the parent (root) host, later than the cache

        host.Children.Add(cacheHost); // earlier DFS = child host boundary
        host.Children.Add(retained);  // later DFS ⇒ receives the upward propagation
        s.RunJobs();
        s.RunJobs();
        s.RunJobs();

        var retRecord = retained.Server.BackdropState!;
        Assert.Same(cacheHost.Server.Cache, vol.Server.BackdropState!.Host); // volatile hosted by the cache
        Assert.Null(retRecord.Host); // retained in the root host

        retRecord.RetainedInputDirtyArea = null;
        s.Events.Rects.Clear();

        // The change is inside the cache (invisible to the target debug proxy). The cache's coarse subtree
        // bounds propagate up to the root host and reach the later retained visual.
        behind.Offset = new Vector3D(0, 50, 0);
        s.AssertRects(
            new Rect(0, 0, 100, 100),  // cache visual's coarse subtree bounds in the parent host
            new Rect(0, 0, 50, 50));   // retained halo of the propagated damage

        Assert.Equal(new LtrbRect(0, 0, 50, 50), retRecord.RetainedInputDirtyArea!.Value);
    }

    [Fact]
    public void Retained_Inside_Cache_Captures_Cache_Local_Rect_Behind_It()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var host = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, host);

        // RenderAtScale + a non-zero draw offset (content does not start at the origin) make cache-local space
        // differ from the tracker's texture-pixel space in both scale and translation, exercising the full
        // host-local ↔ tracker-px mapping end-to-end through the update walk.
        var cacheHost = Panel(c);
        var cache = MakeCache(c);
        cache.RenderAtScale = 2;
        cacheHost.CacheMode = cache;

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(10, 10);
        behind.Offset = new Vector3D(30, 40, 0);
        behind.Color = Colors.Green;

        var retained = c.CreateSolidColorVisual();
        retained.Size = new Vector(60, 60);
        retained.Offset = new Vector3D(20, 20, 0); // ⇒ cache subtree bounds start at (20,20), draw offset = -20
        retained.Color = Colors.Red;
        retained.BackdropEffect = Blur(5);

        cacheHost.Children.Add(behind);   // earlier DFS = behind the backdrop, inside the cache
        cacheHost.Children.Add(retained); // retained backdrop inside the cache
        host.Children.Add(cacheHost);
        // Extra frames so the cache draws at least once (establishing the tracker mapping) before capture.
        s.RunJobs();
        s.RunJobs();
        s.RunJobs();

        var record = retained.Server.BackdropState!;
        Assert.Same(cacheHost.Server.Cache, record.Host);
        Assert.True(record.IsRetained);
        Assert.Equal(new LtrbPixelRect(20, 20, 80, 80), record.Aabb!.Value); // cache-local

        record.RetainedInputDirtyArea = null;

        // A partial change behind the retained backdrop, inside the cache. The captured input must be the
        // cache-LOCAL rect (behind's old∪new ∩ AABB), proving the tracker-px → host-local unmapping.
        behind.Offset = new Vector3D(30, 45, 0);
        s.RunJobs();

        Assert.Equal(new LtrbRect(30, 40, 40, 55), record.RetainedInputDirtyArea!.Value);
    }
}
