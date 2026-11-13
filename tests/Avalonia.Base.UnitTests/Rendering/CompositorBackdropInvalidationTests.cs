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

    private static CompositionRetainedBackdropEffectCache MakeRetainedCacheMode(Compositor compositor) =>
        new(compositor, new ServerCompositionRetainedBackdropEffectCache(compositor.Server));

    private static CompositionVolatileBackdropEffectCache MakeVolatileCacheMode(Compositor compositor) =>
        new(compositor, new ServerCompositionVolatileBackdropEffectCache(compositor.Server));

    // 0: default (null), 1: retained, 2: volatile
    private static CompositionBackdropEffectCache? MakeBackdropCacheMode(Compositor compositor, int selector) => selector switch
    {
        1 => MakeRetainedCacheMode(compositor),
        2 => MakeVolatileCacheMode(compositor),
        _ => null
    };

    // Recounts "_backdropVisualsInSubTree" the slow way: the backdrop visuals below the visual with no bitmap
    // cache in between. A visual with a bitmap cache hides the backdrop visuals below it, but not itself.
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
                    a.BackdropEffectCache = MakeBackdropCacheMode(c, rnd.Next(3));
                    break;
            }

            s.RunJobs();

            foreach (var node in nodes)
                Assert.Equal(ReachableBackdrops(node.Server), node.Server._backdropVisualsInSubTree);
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

        // No bitmap cache: the backdrop visual is counted all the way up the parent chain.
        Assert.Equal(1, mid.Server._backdropVisualsInSubTree);
        Assert.Equal(1, root.Server._backdropVisualsInSubTree);

        // With a bitmap cache on `mid`, the count stops at `mid`.
        mid.CacheMode = MakeCache(c);
        s.RunJobs();
        Assert.Equal(1, mid.Server._backdropVisualsInSubTree);
        Assert.Equal(0, root.Server._backdropVisualsInSubTree);

        // Without the bitmap cache, the visuals above `mid` count it again.
        mid.CacheMode = null;
        s.RunJobs();
        Assert.Equal(1, mid.Server._backdropVisualsInSubTree);
        Assert.Equal(1, root.Server._backdropVisualsInSubTree);
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
        cached.BackdropEffect = Blur(5); // the visual with the bitmap cache is a backdrop visual itself
        s.RunJobs();

        // The visual's own backdrop effect is drawn into the outer layer, so the visual still counts for its
        // parent, even though its bitmap cache hides the backdrop visuals below it.
        Assert.Equal(1, root.Server._backdropVisualsInSubTree);
        Assert.Equal(0, cached.Server._backdropVisualsInSubTree);
    }

    [Fact]
    public void Backdrop_Visual_List_Entry_Is_Order_Independent_Effect_Before_Root()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.BackdropEffect = Blur(5);
        s.RunJobs();

        // Effect set while detached: not in a backdrop visual list yet (no target).
        Assert.Null(v.Server.BackdropState);

        ElementComposition.SetElementChildVisual(s.TopLevel, v);
        s.RunJobs();

        // Once attached, it is in the target's list.
        Assert.NotNull(v.Server.BackdropState);
        Assert.Contains(v.Server, s.Renderer.CompositionTarget.Server.BackdropVisuals);
    }

    [Fact]
    public void Detach_Removes_From_Backdrop_Visual_List()
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
    public void DropShadow_Backdrop_Registers()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var root = c.CreateContainerVisual();
        var leaf = c.CreateSolidColorVisual();
        leaf.Size = new Vector(50, 50);
        root.Children.Add(leaf);
        ElementComposition.SetElementChildVisual(s.TopLevel, root);
        leaf.BackdropEffect = new ImmutableDropShadowEffect(4, 4, 10, Colors.Black, 1);
        s.RunJobs();
        s.RunJobs();

        Assert.NotNull(leaf.Server.BackdropState);
        Assert.Contains(leaf.Server, s.Renderer.CompositionTarget.Server.BackdropVisuals);
        Assert.Equal(1, root.Server._backdropVisualsInSubTree);
    }

    [Fact]
    public void Backdrop_State_Has_Device_Space_Aabb_In_Root_Layer()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.Offset = new Vector3D(100, 100, 0);
        v.Color = Colors.Red;
        v.BackdropEffect = Blur(5);
        ElementComposition.SetElementChildVisual(s.TopLevel, v);
        // Two frames: the AABB is computed from the bounds of the previous update walk.
        s.RunJobs();
        s.RunJobs();

        var state = v.Server.BackdropState;
        Assert.NotNull(state);
        Assert.Null(state!.Layer); // root layer
        Assert.Equal(new LtrbPixelRect(100, 100, 150, 150), state.Aabb!.Value);
    }

    [Fact]
    public void Layout_Bounds_Are_Default_And_Ignore_Descendant_Bounds()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var backdropVisual = Panel(c);
        backdropVisual.Size = new Vector(40, 50);
        backdropVisual.BackdropEffect = Blur(5);

        var child = c.CreateSolidColorVisual();
        child.Size = new Vector(100, 120);
        backdropVisual.Children.Add(child);
        ElementComposition.SetElementChildVisual(s.TopLevel, backdropVisual);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        Assert.Equal(BackdropEffectBoundsSource.Layout, backdropVisual.BackdropEffectBoundsSource);
        Assert.Equal(new LtrbPixelRect(0, 0, 40, 50), state.Aabb!.Value);

        // A bounds change of a child does not change the AABB of a layout-bounds backdrop visual.
        state.InputDirtyRect = null;
        child.Size = new Vector(130, 140);
        s.RunJobs();

        Assert.Equal(new LtrbPixelRect(0, 0, 40, 50), state.Aabb!.Value);
        Assert.Null(state.InputDirtyRect);
    }

    [Fact]
    public void Layout_Rect_Is_Own_Content_Of_A_Layout_Bounds_Backdrop_Visual()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        // No content and no children.
        var empty = c.CreateContainerVisual();
        empty.Size = new Vector(40, 50);
        panel.Children.Add(empty);
        s.RunJobs();
        Assert.Null(empty.Server.SubTreeBounds);

        empty.BackdropEffect = Blur(5);
        s.RunJobs();
        Assert.Equal(new LtrbRect(0, 0, 40, 50), empty.Server.SubTreeBounds);

        // With subtree bounds the layout rect does not count.
        empty.BackdropEffectBoundsSource = BackdropEffectBoundsSource.Subtree;
        s.RunJobs();
        Assert.Null(empty.Server.SubTreeBounds);

        empty.BackdropEffectBoundsSource = BackdropEffectBoundsSource.Layout;
        empty.Size = new Vector(60, 50);
        s.RunJobs();
        Assert.Equal(new LtrbRect(0, 0, 60, 50), empty.Server.SubTreeBounds);

        empty.BackdropEffect = null;
        s.RunJobs();
        Assert.Null(empty.Server.SubTreeBounds);
    }

    [Fact]
    public void Subtree_Bounds_Use_Descendant_Bounds()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var backdropVisual = Panel(c);
        backdropVisual.Size = new Vector(40, 50);
        backdropVisual.BackdropEffect = Blur(5);
        backdropVisual.BackdropEffectBoundsSource = BackdropEffectBoundsSource.Subtree;

        var child = c.CreateSolidColorVisual();
        child.Size = new Vector(100, 120);
        backdropVisual.Children.Add(child);
        ElementComposition.SetElementChildVisual(s.TopLevel, backdropVisual);
        s.RunJobs();
        s.RunJobs();

        Assert.Equal(new LtrbPixelRect(0, 0, 100, 120), backdropVisual.Server.BackdropState!.Aabb!.Value);
    }

    [Fact]
    public void Switching_Bounds_Mode_Recomputes_Aabb_And_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var backdropVisual = Panel(c);
        backdropVisual.Size = new Vector(40, 50);
        backdropVisual.BackdropEffect = Blur(5);

        var child = c.CreateSolidColorVisual();
        child.Size = new Vector(20, 30);
        child.Offset = new Vector3D(60, 70, 0);
        backdropVisual.Children.Add(child);
        ElementComposition.SetElementChildVisual(s.TopLevel, backdropVisual);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        Assert.Equal(new LtrbPixelRect(0, 0, 40, 50), state.Aabb!.Value);
        state.InputDirtyRect = null;

        backdropVisual.BackdropEffectBoundsSource = BackdropEffectBoundsSource.Subtree;
        s.RunJobs();

        Assert.Equal(new LtrbPixelRect(60, 70, 80, 100), state.Aabb!.Value);
        Assert.Equal(new LtrbRect(60, 70, 80, 100), state.InputDirtyRect!.Value);
    }

    // cacheSelector: 0: default (null), 1: retained, 2: volatile
    [Theory]
    [InlineData(5.0, 0, true)]
    [InlineData(0.0, 0, false)]
    [InlineData(0.0, 1, true)]
    [InlineData(5.0, 2, false)]
    public void Backdrop_State_Selects_Retained_Or_Volatile(double radius, int cacheSelector, bool expectedRetained)
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);
        var v = c.CreateSolidColorVisual();
        v.Size = new Vector(50, 50);
        v.BackdropEffect = Blur(radius);
        v.BackdropEffectCache = MakeBackdropCacheMode(c, cacheSelector);
        panel.Children.Add(v);
        s.RunJobs();
        s.RunJobs();

        var state = v.Server.BackdropState;
        Assert.NotNull(state);
        Assert.Equal(expectedRetained, state!.IsRetained);
    }

    [Fact]
    public void Layer_Change_Drops_Snapshot_And_Adds_Dirty_Rects()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        // A parent with a size and an offset, so that a bitmap cache on it adds dirty rects to the root layer,
        // and so that its layer space differs from device pixels.
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

        var state = v.Server.BackdropState!;
        Assert.Null(state.Layer); // root layer at first
        // Layer space of the root layer is device pixels: includes the parent's offset.
        Assert.Equal(new LtrbPixelRect(40, 50, 90, 100), state.Aabb!.Value);
        var sentinel = new ServerCompositionBackdropSnapshot();
        state.Snapshot = sentinel;
        s.Events.Reset();

        // A bitmap cache on the parent: the backdrop visual is now in the bitmap cache layer.
        mid.CacheMode = MakeCache(c);
        s.RunJobs();

        Assert.Same(mid.Server.Cache, state.Layer);
        Assert.Null(state.Snapshot); // dropped on a layer change
        // Layer space of the bitmap cache layer is the cached visual's local space: no parent offset.
        Assert.Equal(new LtrbPixelRect(30, 40, 80, 90), state.Aabb!.Value);
        Assert.NotEmpty(s.Events.Rects); // the update walk collected dirty rects for the change
    }

    [Fact]
    public void Pixel_Move_Keeps_Retained_State_And_Reingests_Whole_Aabb()
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

        var state = v.Server.BackdropState!;
        var aabb0 = state.Aabb;
        Assert.NotNull(aabb0);
        var sentinel = new ServerCompositionBackdropSnapshot();
        state.Snapshot = sentinel;

        // A sub-pixel move that gives the same pixel-aligned AABB: nothing changes.
        v.Offset = new Vector3D(100.4, 100.4, 0);
        s.RunJobs();
        s.RunJobs();
        Assert.Equal(aabb0!.Value, state.Aabb!.Value);
        Assert.Same(sentinel, state.Snapshot);

        // A move that changes the AABB keeps the snapshot, and the whole new AABB is the input dirty rect.
        state.InputDirtyRect = null;
        v.Offset = new Vector3D(101.6, 101.6, 0);
        s.RunJobs();
        Assert.NotEqual(aabb0.Value, state.Aabb!.Value);
        Assert.Same(sentinel, state.Snapshot);
        Assert.Equal(state.Aabb!.Value.ToLtrbRectUnscaled(), state.InputDirtyRect);
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

        // Detached: the update walk does not visit it, so the flag stays set.
        Assert.True(parent.Server._childrenChanged);
    }

    [Fact]
    public void Children_Changed_Flag_Cleared_By_Update_Walk_When_Attached()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);
        s.RunJobs();

        var child = c.CreateSolidColorVisual();
        child.Size = new Vector(20, 20);
        panel.Children.Add(child);
        s.RunJobs();

        Assert.False(panel.Server._childrenChanged);
    }

    // A container that does not clip its children. ClipToBounds is true by default, which clips the children
    // of a zero-sized container, and their dirty rects, to nothing.
    private static CompositionContainerVisual Panel(Compositor c)
    {
        var v = c.CreateContainerVisual();
        v.ClipToBounds = false;
        return v;
    }

    // A retained backdrop visual. Blur(5) has a sampling radius of ceil(5)+1 = 6 at scaling 1.
    private CompositionSolidColorVisual RetainedBackdropVisual(Compositor c, Vector size)
    {
        var v = c.CreateSolidColorVisual();
        v.Size = size;
        v.Color = Colors.Red;
        v.BackdropEffect = Blur(5);
        return v;
    }

    private static CompositionContainerVisual ContainerBackdropVisual(Compositor c)
    {
        var v = Panel(c);
        v.BackdropEffect = Blur(5);
        v.BackdropEffectBoundsSource = BackdropEffectBoundsSource.Subtree;
        return v;
    }

    [Fact]
    public void Change_Behind_Retained_Backdrop_Accumulates_Input_And_Emits_Inflated_Output()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Offset = new Vector3D(10, 10, 0);
        behind.Color = Colors.Blue;

        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));

        panel.Children.Add(behind);   // earlier in draw order: behind the backdrop visual
        panel.Children.Add(backdropVisual); // later in draw order: in front, `behind` is in its backdrop
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        Assert.True(state.IsRetained);
        Assert.Equal(new LtrbPixelRect(0, 0, 100, 100), state.Aabb!.Value);

        state.InputDirtyRect = null;
        s.Events.Rects.Clear();

        // `behind` moves: old and new bounds span (10,10)-(30,70), all inside the AABB. Inflated by 6: (4,4)-(36,76).
        behind.Offset = new Vector3D(10, 50, 0);
        s.AssertRects(
            new Rect(10, 10, 20, 20),  // `behind`, old
            new Rect(10, 50, 20, 20),  // `behind`, new
            new Rect(4, 4, 32, 72));   // output dirty rect

        Assert.Equal(new LtrbRect(10, 10, 30, 70), state.InputDirtyRect!.Value);
    }

    [Fact]
    public void Change_In_Front_Leaves_Retained_Input_Untouched()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));
        var front = c.CreateSolidColorVisual();
        front.Size = new Vector(20, 20);
        front.Offset = new Vector3D(10, 10, 0);
        front.Color = Colors.Blue;

        panel.Children.Add(backdropVisual); // earlier in draw order: behind
        panel.Children.Add(front);    // later in draw order: in front
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        state.InputDirtyRect = null;
        s.Events.Rects.Clear();

        // The change is in front of the backdrop visual, so it is not input.
        front.Offset = new Vector3D(10, 50, 0);
        s.AssertRects(
            new Rect(10, 10, 20, 20),
            new Rect(10, 50, 20, 20));

        Assert.Null(state.InputDirtyRect);
    }

    [Fact]
    public void Outer_Dirty_Rects_Do_Not_Reach_Retained_Backdrop_Inside_Clean_Bitmap_Cache()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Offset = new Vector3D(10, 10, 0);
        behind.Color = Colors.Blue;

        var cached = c.CreateContainerVisual();
        cached.Size = new Vector(100, 100);
        cached.CacheMode = MakeCache(c);
        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));
        cached.Children.Add(backdropVisual);

        panel.Children.Add(behind);
        panel.Children.Add(cached);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        Assert.Same(cached.Server.Cache, state.Layer);
        state.InputDirtyRect = null;
        s.Events.Rects.Clear();

        // The backdrop visual reads the bitmap cache layer: a change outside of the bitmap cache is not its input.
        behind.Offset = new Vector3D(10, 50, 0);
        s.AssertRects(
            new Rect(10, 10, 20, 20),
            new Rect(10, 50, 20, 20));

        Assert.Null(state.InputDirtyRect);
    }

    [Fact]
    public void Dirty_Parent_Fully_Invalidates_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var effectParent = Panel(c);
        effectParent.Effect = Blur(3); // an effect on the parent: its subtree is walked with dirty regions disabled
        panel.Children.Add(effectParent);

        var x = c.CreateSolidColorVisual();
        x.Size = new Vector(20, 20);
        x.Offset = new Vector3D(200, 200, 0);
        x.Color = Colors.Blue;

        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));
        effectParent.Children.Add(x);
        effectParent.Children.Add(backdropVisual);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        Assert.Equal(new LtrbPixelRect(0, 0, 100, 100), state.Aabb!.Value);
        state.InputDirtyRect = null;

        // The dirty rect of a dirty parent is collected in its PostSubgraph, so it is not in the working set when
        // the walk reaches the backdrop visual. The whole input is dirty.
        x.Offset = new Vector3D(200, 250, 0);
        s.RunJobs();

        Assert.Equal(new LtrbRect(0, 0, 100, 100), state.InputDirtyRect!.Value);
    }

    [Fact]
    public void Child_Removal_In_Parent_Chain_Fully_Invalidates_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var p = Panel(c);
        panel.Children.Add(p);

        var mover = c.CreateSolidColorVisual();
        mover.Size = new Vector(20, 20);
        mover.Offset = new Vector3D(500, 500, 0); // outside of the backdrop AABB
        mover.Color = Colors.Green;

        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));

        var removable = c.CreateSolidColorVisual();
        removable.Size = new Vector(20, 20);
        removable.Offset = new Vector3D(200, 200, 0);
        removable.Color = Colors.Blue;

        p.Children.Add(mover);     // earlier in draw order: its dirty rects are in the working set
        p.Children.Add(backdropVisual);
        p.Children.Add(removable); // removed below: its dirty rect is collected in p's PostSubgraph
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        state.InputDirtyRect = null;

        // `mover` is outside of the AABB, so it adds nothing. Removing `removable` sets _childrenChanged on p,
        // which makes the whole input dirty.
        mover.Offset = new Vector3D(500, 520, 0);
        p.Children.Remove(removable);
        s.RunJobs();

        Assert.Equal(new LtrbRect(0, 0, 100, 100), state.InputDirtyRect!.Value);
    }

    [Fact]
    public void Descent_Check_Visits_Clean_Visuals_Only_With_Dirty_Rects()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var mover = c.CreateSolidColorVisual();
        mover.Size = new Vector(20, 20);
        mover.Offset = new Vector3D(10, 10, 0); // overlaps the backdrop AABB
        mover.Color = Colors.Green;

        var c1 = Panel(c);
        var c2 = Panel(c);
        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));

        panel.Children.Add(mover); // earlier in draw order
        panel.Children.Add(c1);    // clean visuals: c1, c2, backdrop
        c1.Children.Add(c2);
        c2.Children.Add(backdropVisual);
        s.RunJobs();
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        state.InputDirtyRect = null;

        // Idle frame: the clean visuals are not visited, nothing is collected, nothing is drawn.
        s.Events.Reset();
        s.RunJobs();
        Assert.Null(state.InputDirtyRect);
        Assert.Equal(0, s.Events.VisitedVisuals);

        // `mover` is earlier in draw order and overlaps the AABB. When it changes, the descent check makes the
        // walk visit the clean visuals, and the input dirty rect is collected.
        mover.Offset = new Vector3D(10, 15, 0);
        s.RunJobs();
        Assert.Equal(new LtrbRect(10, 10, 30, 35), state.InputDirtyRect!.Value);
    }

    [Fact]
    public void Dirty_Parent_Alone_Fully_Invalidates_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        // The parent's own content is behind the backdrop visual, so its opacity change is input.
        var ancestor = c.CreateSolidColorVisual();
        ancestor.Size = new Vector(100, 100);
        ancestor.Color = Colors.Blue;
        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));
        ancestor.Children.Add(backdropVisual);
        panel.Children.Add(ancestor);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        state.InputDirtyRect = null;

        // Nothing else is dirty this frame, so the working set is empty when the walk reaches the parent.
        ancestor.Opacity = 0.5f;
        s.RunJobs();

        Assert.Equal(new LtrbRect(0, 0, 100, 100), state.InputDirtyRect!.Value);
    }

    [Fact]
    public void Child_Removal_Alone_Fully_Invalidates_Later_Sibling_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var p = Panel(c);
        panel.Children.Add(p);

        var removable = c.CreateSolidColorVisual();
        removable.Size = new Vector(20, 20);
        removable.Offset = new Vector3D(10, 10, 0); // behind the backdrop visual
        removable.Color = Colors.Blue;
        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));

        p.Children.Add(removable);
        p.Children.Add(backdropVisual);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        state.InputDirtyRect = null;

        // The dirty rect of the removal is collected in p's PostSubgraph, after the walk has passed the
        // backdrop visual.
        p.Children.Remove(removable);
        s.RunJobs();

        Assert.Equal(new LtrbRect(0, 0, 100, 100), state.InputDirtyRect!.Value);
    }

    private (CompositionContainerVisual cached, CompositionSolidColorVisual removable,
        ServerCompositionVisualBackdropState state)
        BuildCachedBackdrop(CompositorTestServices s)
    {
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var cached = c.CreateContainerVisual();
        cached.Size = new Vector(100, 100);
        cached.CacheMode = MakeCache(c);
        var removable = c.CreateSolidColorVisual();
        removable.Size = new Vector(20, 20);
        removable.Offset = new Vector3D(10, 10, 0);
        removable.Color = Colors.Blue;
        var backdropVisual = RetainedBackdropVisual(c, new Vector(100, 100));
        cached.Children.Add(removable);
        cached.Children.Add(backdropVisual);
        panel.Children.Add(cached);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        Assert.Same(cached.Server.Cache, state.Layer);
        state.InputDirtyRect = null;
        return (cached, removable, state);
    }

    [Fact]
    public void Moving_A_Cache_Does_Not_Invalidate_Retained_Backdrop_Inside_It()
    {
        using var s = new CompositorTestServices();
        var (cached, _, state) = BuildCachedBackdrop(s);

        // The contents of the bitmap cache layer are the same. Only the place where it is drawn changes.
        cached.Offset = new Vector3D(30, 0, 0);
        s.RunJobs();

        Assert.Null(state.InputDirtyRect);
    }

    [Fact]
    public void Child_Removal_Inside_Cache_Fully_Invalidates_Later_Sibling_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var (cached, removable, state) = BuildCachedBackdrop(s);

        cached.Children.Remove(removable);
        s.RunJobs();

        Assert.Equal(new LtrbRect(0, 0, 100, 100), state.InputDirtyRect!.Value);
    }

    [Fact]
    public void Stale_Aabb_Fully_Invalidates_Retained_Input_The_Same_Frame()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var backdropVisual = ContainerBackdropVisual(c);
        panel.Children.Add(backdropVisual);

        var d = c.CreateSolidColorVisual();
        d.Size = new Vector(20, 20);
        d.Color = Colors.Red;
        backdropVisual.Children.Add(d);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        Assert.Equal(new LtrbPixelRect(0, 0, 20, 20), state.Aabb!.Value);
        state.InputDirtyRect = null;
        var sentinel = new ServerCompositionBackdropSnapshot();
        state.Snapshot = sentinel;

        // A child of the backdrop visual gets a new size: FinalizeSubtreeBounds changes the bounds that the AABB
        // was computed from. CheckBackdropStaleAabb corrects the AABB in the same frame.
        d.Size = new Vector(60, 60);
        s.RunJobs();

        Assert.Equal(new LtrbPixelRect(0, 0, 60, 60), state.Aabb!.Value);
        Assert.Equal(new LtrbRect(0, 0, 60, 60), state.InputDirtyRect!.Value);
        Assert.Same(sentinel, state.Snapshot); // the snapshot is kept

        // The state is up to date, so the next backdrop state recompute sees no change.
        state.InputDirtyRect = null;
        s.RunJobs();
        Assert.Null(state.InputDirtyRect);
    }

    [Fact]
    public void Stale_Aabb_SubPixel_Child_Jitter_Does_Not_Invalidate()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var backdropVisual = ContainerBackdropVisual(c);
        panel.Children.Add(backdropVisual);

        var d = c.CreateSolidColorVisual();
        d.Size = new Vector(20, 20);
        d.Offset = new Vector3D(0.2, 0.2, 0);
        d.Color = Colors.Red;
        backdropVisual.Children.Add(d);
        s.RunJobs();
        s.RunJobs();

        var state = backdropVisual.Server.BackdropState!;
        // (0.2,0.2)-(20.2,20.2) rounds out to (0,0,21,21).
        Assert.Equal(new LtrbPixelRect(0, 0, 21, 21), state.Aabb!.Value);
        state.InputDirtyRect = null;

        // A sub-pixel move that gives the same pixel-aligned AABB: no change.
        d.Offset = new Vector3D(0.4, 0.4, 0);
        s.RunJobs();

        Assert.Equal(new LtrbPixelRect(0, 0, 21, 21), state.Aabb!.Value);
        Assert.Null(state.InputDirtyRect);
    }

    [Fact]
    public void Bounds_Change_Self_Invalidation_Emits_Backdrop_Old_And_New_Subtree_Bounds()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var backdropVisual = ContainerBackdropVisual(c);
        panel.Children.Add(backdropVisual);

        // A static child keeps the subtree bounds of the backdrop visual larger than the rects of the moving child.
        var stat = c.CreateSolidColorVisual();
        stat.Size = new Vector(20, 20);
        stat.Offset = new Vector3D(50, 50, 0);
        stat.Color = Colors.Green;
        backdropVisual.Children.Add(stat);

        var mover = c.CreateSolidColorVisual();
        mover.Size = new Vector(20, 20);
        mover.Color = Colors.Red;
        backdropVisual.Children.Add(mover);
        s.RunJobs();
        s.RunJobs();

        s.Events.Rects.Clear();

        // A child moves outside of the old bounds of the backdrop visual. The old and the new subtree bounds of
        // the backdrop visual are collected, not only the rects of the child.
        mover.Offset = new Vector3D(0, 80, 0);
        s.AssertRects(
            new Rect(0, 0, 70, 70),   // old subtree bounds: union(stat, mover old)
            new Rect(0, 50, 70, 50)); // new subtree bounds: union(stat, mover new)
    }

    [Fact]
    public void InvalidateRetainedBackdropInputs_Marks_Only_Retained_In_The_Matching_Layer()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var retainedVisual = RetainedBackdropVisual(c, new Vector(50, 50));

        var volatileVisual = c.CreateSolidColorVisual();
        volatileVisual.Size = new Vector(50, 50);
        volatileVisual.Offset = new Vector3D(100, 0, 0);
        volatileVisual.Color = Colors.Blue;
        volatileVisual.BackdropEffect = Blur(0); // no sampling radius: volatile

        // A bitmap cache layer, to check that only the given layer is affected.
        var cachedVisual = Panel(c);
        cachedVisual.Offset = new Vector3D(0, 200, 0);
        cachedVisual.CacheMode = MakeCache(c);

        panel.Children.Add(retainedVisual);
        panel.Children.Add(volatileVisual);
        panel.Children.Add(cachedVisual);
        s.RunJobs();
        s.RunJobs();

        var retainedState = retainedVisual.Server.BackdropState!;
        var volatileState = volatileVisual.Server.BackdropState!;
        Assert.True(retainedState.IsRetained);
        Assert.False(volatileState.IsRetained);

        var target = s.Renderer.CompositionTarget.Server;

        retainedState.InputDirtyRect = null;
        volatileState.InputDirtyRect = null;
        target.InvalidateRetainedBackdropInputs(null); // root layer

        Assert.Equal(new LtrbRect(0, 0, 50, 50), retainedState.InputDirtyRect!.Value);
        Assert.Null(volatileState.InputDirtyRect); // volatile: not affected

        // A bitmap cache layer marks nothing in the root layer.
        retainedState.InputDirtyRect = null;
        target.InvalidateRetainedBackdropInputs(cachedVisual.Server.Cache);
        Assert.Null(retainedState.InputDirtyRect);
    }

    // A volatile backdrop visual with a sampling radius of 6 (Blur(5), explicitly volatile).
    private CompositionSolidColorVisual VolatileBackdropVisual(Compositor c, Vector size, Vector3D offset)
    {
        var v = c.CreateSolidColorVisual();
        v.Size = size;
        v.Offset = offset;
        v.Color = Colors.Blue;
        v.BackdropEffect = Blur(5);
        v.BackdropEffectCache = MakeVolatileCacheMode(c);
        return v;
    }

    [Fact]
    public void Volatile_Whole_Aabb_Invalidation_Reaches_Retained_In_Front()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Offset = new Vector3D(200, 0, 0); // overlaps the volatile one, not the retained one
        behind.Color = Colors.Green;

        var vol = VolatileBackdropVisual(c, new Vector(300, 100), new Vector3D(0, 0, 0));
        var retained = RetainedBackdropVisual(c, new Vector(50, 50)); // in front of `vol`, overlaps its AABB

        panel.Children.Add(behind);   // first in draw order
        panel.Children.Add(vol);      // volatile, with a sampling radius
        panel.Children.Add(retained); // last in draw order: gets the AABB of the volatile one as input
        s.RunJobs();
        s.RunJobs();

        var volState = vol.Server.BackdropState!;
        var retState = retained.Server.BackdropState!;
        Assert.False(volState.IsRetained);
        Assert.True(retState.IsRetained);
        Assert.Equal(new LtrbPixelRect(0, 0, 300, 100), volState.Aabb!.Value);

        retState.InputDirtyRect = null;
        s.Events.Rects.Clear();

        // The change is outside of the retained AABB, so the update walk collects nothing for it. The volatile
        // pass adds the whole volatile AABB, and its part inside the retained AABB is input for `retained`.
        behind.Offset = new Vector3D(200, 50, 0);
        s.AssertRects(
            new Rect(200, 0, 20, 20),   // `behind`, old
            new Rect(200, 50, 20, 20),  // `behind`, new
            new Rect(0, 0, 300, 100),   // whole-AABB invalidation of the volatile one
            new Rect(0, 0, 50, 50));    // output dirty rect of the retained one (inflated by 6, clipped to its AABB)

        Assert.Equal(new LtrbRect(0, 0, 50, 50), retState.InputDirtyRect!.Value);
    }

    [Fact]
    public void Volatile_Whole_Aabb_Invalidation_Reaches_Retained_Through_Output_Dirty_Rect_Of_Another()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Offset = new Vector3D(10, 10, 0); // overlaps only the volatile one
        behind.Color = Colors.Green;

        var vol = VolatileBackdropVisual(c, new Vector(100, 100), new Vector3D(0, 0, 0)); // x 0..100
        var r1 = RetainedBackdropVisual(c, new Vector(100, 100));
        r1.Offset = new Vector3D(95, 0, 0); // x 95..195: overlaps the volatile one in 95..100
        var r2 = RetainedBackdropVisual(c, new Vector(50, 50));
        r2.Offset = new Vector3D(102, 0, 0); // x 102..152: does not touch the volatile one, inside r1

        panel.Children.Add(behind);
        panel.Children.Add(vol);
        panel.Children.Add(r1);
        panel.Children.Add(r2);
        s.RunJobs();
        s.RunJobs();

        var r1State = r1.Server.BackdropState!;
        var r2State = r2.Server.BackdropState!;
        r1State.InputDirtyRect = null;
        r2State.InputDirtyRect = null;

        behind.Offset = new Vector3D(10, 50, 0);
        s.RunJobs();

        // r1 gets the part of the volatile AABB inside its own: x 95..100. Its output dirty rect: x 95..106.
        Assert.Equal(new LtrbRect(95, 0, 100, 100), r1State.InputDirtyRect!.Value);
        // r2 is in front of r1, so the output dirty rect of r1 in x 102..106 is input of r2.
        Assert.True(r2State.InputDirtyRect.HasValue, "r2 received no input damage");
        Assert.Equal(new LtrbRect(102, 0, 106, 50), r2State.InputDirtyRect!.Value);
    }

    [Fact]
    public void Volatile_Behind_Volatile_Chain_Converges_To_Whole_Aabbs()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Color = Colors.Green;

        // Each volatile one overlaps only the next one, so the pass has to repeat.
        var v1 = VolatileBackdropVisual(c, new Vector(100, 100), new Vector3D(0, 0, 0));
        var v2 = VolatileBackdropVisual(c, new Vector(100, 100), new Vector3D(80, 0, 0));
        var v3 = VolatileBackdropVisual(c, new Vector(100, 100), new Vector3D(160, 0, 0));

        panel.Children.Add(behind);
        panel.Children.Add(v1);
        panel.Children.Add(v2);
        panel.Children.Add(v3);
        s.RunJobs();
        s.RunJobs();

        s.Events.Rects.Clear();

        // `behind` touches only v1. Invalidating v1 reaches v2, and v2 reaches v3.
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
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Color = Colors.Green;

        // v1 and v2 overlap each other: without the Invalidated flags the loop would not end.
        var v1 = VolatileBackdropVisual(c, new Vector(100, 100), new Vector3D(0, 0, 0));
        var v2 = VolatileBackdropVisual(c, new Vector(100, 100), new Vector3D(50, 0, 0));

        panel.Children.Add(behind);
        panel.Children.Add(v1);
        panel.Children.Add(v2);
        s.RunJobs();
        s.RunJobs();

        s.Events.Rects.Clear();

        behind.Offset = new Vector3D(0, 50, 0);
        s.AssertRects(
            new Rect(0, 0, 20, 20),
            new Rect(0, 50, 20, 20),
            new Rect(0, 0, 100, 100),   // v1
            new Rect(50, 0, 100, 100)); // v2 (each one is invalidated once, so the loop ends)
    }

    [Fact]
    public void In_Front_Volatile_Does_Not_Dirty_Earlier_Retained_Input()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var retained = RetainedBackdropVisual(c, new Vector(50, 50)); // earlier in draw order: behind
        var vol = VolatileBackdropVisual(c, new Vector(50, 50), new Vector3D(100, 0, 0)); // later in draw order: in front

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Offset = new Vector3D(100, 0, 0); // overlaps the volatile one only
        behind.Color = Colors.Green;

        panel.Children.Add(retained);
        panel.Children.Add(vol);
        panel.Children.Add(behind);
        s.RunJobs();
        s.RunJobs();

        var retState = retained.Server.BackdropState!;
        retState.InputDirtyRect = null;
        s.Events.Rects.Clear();

        // The volatile one is invalidated, but it is in front of the retained one, so it is not its input.
        behind.Offset = new Vector3D(100, 20, 0);
        s.AssertRects(
            new Rect(100, 0, 20, 20),
            new Rect(100, 20, 20, 20),
            new Rect(100, 0, 50, 50)); // whole-AABB invalidation of the volatile one only

        Assert.Null(retState.InputDirtyRect);
    }

    [Fact]
    public void Volatile_Inside_Bitmap_Cache_Reaches_Retained_In_Front_In_Outer_Layer()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        var cachedVisual = Panel(c);
        cachedVisual.CacheMode = MakeCache(c);

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(20, 20);
        behind.Color = Colors.Green;
        var vol = VolatileBackdropVisual(c, new Vector(100, 100), new Vector3D(0, 0, 0)); // inside the bitmap cache
        cachedVisual.Children.Add(behind);
        cachedVisual.Children.Add(vol);

        var retained = RetainedBackdropVisual(c, new Vector(50, 50)); // in the root layer, in front of the cached visual

        panel.Children.Add(cachedVisual); // earlier in draw order
        panel.Children.Add(retained);  // later in draw order: gets the bounds of the cached visual as input
        s.RunJobs();
        s.RunJobs();
        s.RunJobs();

        var retState = retained.Server.BackdropState!;
        Assert.Same(cachedVisual.Server.Cache, vol.Server.BackdropState!.Layer); // in the bitmap cache layer
        Assert.Null(retState.Layer); // retained one in the root layer

        retState.InputDirtyRect = null;
        s.Events.Rects.Clear();

        // The change is inside the bitmap cache, which the debug proxy of the target does not see. The bounds
        // of the cached visual become dirty in the root layer and reach the retained one in front of it.
        behind.Offset = new Vector3D(0, 50, 0);
        s.AssertRects(
            new Rect(0, 0, 100, 100),  // bounds of the cached visual in the root layer
            new Rect(0, 0, 50, 50));   // output dirty rect of the retained one

        Assert.Equal(new LtrbRect(0, 0, 50, 50), retState.InputDirtyRect!.Value);
    }

    [Fact]
    public void Retained_Inside_Bitmap_Cache_Collects_Layer_Space_Rect_Behind_It()
    {
        using var s = new CompositorTestServices();
        var c = s.Compositor;
        var panel = Panel(c);
        ElementComposition.SetElementChildVisual(s.TopLevel, panel);

        // RenderAtScale and a draw offset (the content does not start at the origin) make layer space differ
        // from layer pixels in scale and in offset, so the update walk has to use the whole mapping.
        var cachedVisual = Panel(c);
        var cache = MakeCache(c);
        cache.RenderAtScale = 2;
        cachedVisual.CacheMode = cache;

        var behind = c.CreateSolidColorVisual();
        behind.Size = new Vector(10, 10);
        behind.Offset = new Vector3D(30, 40, 0);
        behind.Color = Colors.Green;

        var retained = c.CreateSolidColorVisual();
        retained.Size = new Vector(60, 60);
        retained.Offset = new Vector3D(20, 20, 0); // cached subtree bounds start at (20,20), draw offset = -20
        retained.Color = Colors.Red;
        retained.BackdropEffect = Blur(5);

        cachedVisual.Children.Add(behind);   // behind the backdrop visual, inside the bitmap cache
        cachedVisual.Children.Add(retained); // retained backdrop visual inside the bitmap cache
        panel.Children.Add(cachedVisual);
        // More frames, so that the bitmap cache draws once and has a mapping before the input dirty rect is collected.
        s.RunJobs();
        s.RunJobs();
        s.RunJobs();

        var state = retained.Server.BackdropState!;
        Assert.Same(cachedVisual.Server.Cache, state.Layer);
        Assert.True(state.IsRetained);
        Assert.Equal(new LtrbPixelRect(20, 20, 80, 80), state.Aabb!.Value); // layer space

        state.InputDirtyRect = null;

        // A change behind the retained backdrop visual, inside the bitmap cache. The input dirty rect has to be
        // in layer space (old and new bounds of `behind` inside the AABB), not in layer pixels.
        behind.Offset = new Vector3D(30, 45, 0);
        s.RunJobs();

        Assert.Equal(new LtrbRect(30, 40, 40, 55), state.InputDirtyRect!.Value);
    }
}
