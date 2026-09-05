using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionTarget
{
    /// <summary>
    /// Post-update volatile backdrop pass. Runs in <see cref="ServerCompositionTarget.Update"/>
    /// after <c>UpdateRoot</c>. Per layer host it collects backdrops (and child-host boundaries) in DFS order,
    /// then spins a fixpoint that expands every volatile (R&gt;0) backdrop whose AABB the host's dirty region
    /// touches to its <em>whole</em> AABB (the whole-visual invalidation rule) and delivers that damage to
    /// retained backdrops later in DFS order, applying their output-inflation rule. Damage newly added to a
    /// child host is propagated up the host tree (coarse subtree bounds) to retained backdrops later than the
    /// child-host boundary. The pass only <em>marks</em> the region and grows retained input areas — nothing
    /// draws here.
    ///
    /// Region reads/writes use the mid-pass raw working set exclusively: <c>Intersects()</c>/<c>CombinedRect</c>
    /// are invalid until <c>FinalizeFrame</c> (which runs later in <c>Render</c>), and
    /// <c>GetUninflatedDirtyRegions()</c> would set the multi-rect tracker's optimized flag and silently drop
    /// later fixpoint additions from reads.
    /// </summary>
    internal sealed class BackdropVolatilePass
    {
        internal struct VolatileEntry
        {
            public int DfsIndex;
            public ServerCompositionVisualBackdropState Registration;
            public bool Expanded;
        }

        internal struct RetainedEntry
        {
            public int DfsIndex;
            public ServerCompositionVisualBackdropState Registration;
        }

        internal sealed class HostScan
        {
            // The layer host: null is the render target ("root") host, otherwise a bitmap cache.
            public ServerCompositionVisualCache? Host;

            // For the root host this is the same DebugEvents-wrapped collector the update walk used (so additions
            // stay test-observable); for a cache host it is that cache's own collector.
            public IDirtyRectCollector Collector = null!;

            // The visual the host is rooted at: the target's root visual, or the cache-owning visual.
            public ServerCompositionVisual HostRootVisual = null!;

            // Parent host in the layer-host tree, plus this host's boundary visual DFS position / coarse bounds
            // within that parent (used by the upward propagation).
            public HostScan? Parent;
            public int BoundaryDfsIndex;
            public LtrbRect? BoundaryCoarseBounds;

            public List<VolatileEntry> Volatiles = null!;
            public List<RetainedEntry> Retained = null!;

            // Set whenever this pass adds any rect to this host; gates the upward propagation.
            public bool ReceivedNewDamage;
            public int Counter;

            // Clears all per-frame state so a pooled shell can be reused. Volatiles/Retained are assigned by
            // NewHostScan right after this and nulled on return, so they're left alone here.
            public void Reset()
            {
                Host = null;
                Collector = null!;
                HostRootVisual = null!;
                Parent = null;
                BoundaryDfsIndex = 0;
                BoundaryCoarseBounds = null;
                ReceivedNewDamage = false;
                Counter = 0;
            }
        }

        private CompositorPools _pools = null!;
        private List<HostScan> _hosts = null!;
        private List<LtrbRect> _workingBuffer = null!;
        // HostScan shells reused across frames (this pass instance is reused per target). Only the shell is
        // pooled here; its inner Volatiles/Retained lists are rented from CompositorPools per frame.
        private readonly Stack<HostScan> _hostScanPool = new();

        public void Run(ServerCompositionTarget target, IDirtyRectCollector rootCollector, double rootScaling)
        {
            if (target.BackdropVisuals.Count == 0 || target.Root == null)
                return;
            Execute(target, rootCollector, rootScaling);
        }

        private void Execute(ServerCompositionTarget target, IDirtyRectCollector rootCollector, double rootScaling)
        {
            _pools = target.Compositor.Pools;
            _hosts = _pools.BackdropHostScanListPool.Rent();
            _workingBuffer = _pools.LtrbRectListPool.Rent();
            try
            {
                var root = target.Root!;
                var rootScan = NewHostScan();
                rootScan.Host = null;
                rootScan.Collector = rootCollector;
                rootScan.HostRootVisual = root;
                _hosts.Add(rootScan);

                // Root host space is device pixels: fold the target scaling (and the root visual's own transform)
                // in, matching the registry's host-space AABBs.
                var scale = Matrix.CreateScale(rootScaling, rootScaling);
                var rootInner = root.OwnTransform is { } rootTransform ? rootTransform * scale : scale;
                Scan(rootScan, root, rootInner);

                // Process bottom-up so a child host's upward propagation is already in the parent's region before
                // the parent's own fixpoint runs. Scan() appends every child host after its parent, so reverse
                // order visits children before parents.
                for (var i = _hosts.Count - 1; i >= 0; i--)
                {
                    var host = _hosts[i];
                    RunFixpoint(host);
                    if (host.Host != null && host.ReceivedNewDamage)
                        PropagateToParent(host);
                }
            }
            finally
            {
                foreach (var host in _hosts)
                {
                    _pools.BackdropVolatileEntryListPool.Return(host.Volatiles);
                    _pools.BackdropRetainedEntryListPool.Return(host.Retained);
                    host.Volatiles = null!;
                    host.Retained = null!;
                    _hostScanPool.Push(host);
                }
                _pools.BackdropHostScanListPool.Return(ref _hosts);
                _pools.LtrbRectListPool.Return(ref _workingBuffer);
            }
        }

        private HostScan NewHostScan()
        {
            var scan = _hostScanPool.Count > 0 ? _hostScanPool.Pop() : new HostScan();
            scan.Reset();
            scan.Volatiles = _pools.BackdropVolatileEntryListPool.Rent();
            scan.Retained = _pools.BackdropRetainedEntryListPool.Rent();
            return scan;
        }

        // Downward DFS collecting DFS-ordered backdrop and child-host-boundary entries for <paramref name="scan"/>'s
        // host. <paramref name="mInner"/> maps <paramref name="node"/>'s inner (content/child) space to host space.
        private void Scan(HostScan scan, ServerCompositionVisual node, Matrix mInner)
        {
            var index = scan.Counter++;

            var registration = node.BackdropState;
            if (registration is { Aabb: not null } && ReferenceEquals(registration.Host, scan.Host))
            {
                if (registration.IsRetained)
                    scan.Retained.Add(new RetainedEntry { DfsIndex = index, Registration = registration });
                else
                    scan.Volatiles.Add(new VolatileEntry { DfsIndex = index, Registration = registration });
            }

            // A cache boundary (other than this host's own root) starts a child host: record its DFS position and
            // scan it as a separate host, but never descend into it within this host (a different backbuffer).
            if (node.Cache is { } childHost && !ReferenceEquals(node, scan.HostRootVisual))
            {
                if (node._backdropsInSubTree > 0)
                {
                    var childScan = NewHostScan();
                    childScan.Host = childHost;
                    childScan.Collector = childHost.DirtyRectCollector;
                    childScan.HostRootVisual = node;
                    childScan.Parent = scan;
                    childScan.BoundaryDfsIndex = index;
                    childScan.BoundaryCoarseBounds =
                        node.SubTreeBounds is { } bounds ? bounds.TransformToAABB(mInner) : null;
                    _hosts.Add(childScan);
                    // Cache host space is the cache visual's own inner space, so its children start at identity.
                    Scan(childScan, node, Matrix.Identity);
                }

                return;
            }

            if (node.Children is not { } children)
                return;

            foreach (var child in children.List)
            {
                // Prune spines with no backdrops in this host and no backdrop-hosting cache below.
                if (child._backdropsInSubTree > 0 || child.BackdropState != null)
                {
                    var childInner = child.OwnTransform is { } ownTransform ? ownTransform * mInner : mInner;
                    Scan(scan, child, childInner);
                }
            }
        }

        // Spins the whole-visual expansion until an iteration changes nothing. Each volatile expands at most once
        // (the Expanded mark), so the loop runs in at most O(volatiles) iterations.
        private void RunFixpoint(HostScan host)
        {
            if (host.Volatiles.Count == 0)
                return;

            var volatiles = CollectionsMarshal.AsSpan(host.Volatiles);
            var changed = true;
            while (changed)
            {
                changed = false;

                var workingSet = host.Collector.GetWorkingSet();
                if (workingSet.IsEmpty)
                    break;
                workingSet.CollectHostSpace(_workingBuffer);
                if (_workingBuffer.Count == 0)
                    break;

                for (var i = 0; i < volatiles.Length; i++)
                {
                    ref var entry = ref volatiles[i];
                    if (entry.Expanded || entry.Registration.HostSpaceSamplingRadius <= 0
                        || entry.Registration.Aabb is not { } aabbPixels)
                        continue;

                    var aabb = aabbPixels.ToLtrbRectUnscaled();
                    if (!IntersectsWorkingSet(aabb))
                        continue;

                    entry.Expanded = true;
                    host.Collector.AddRect(aabb);
                    host.ReceivedNewDamage = true;
                    DeliverToLaterRetained(host, entry.DfsIndex, aabb);
                    changed = true;
                }
            }
        }

        private bool IntersectsWorkingSet(LtrbRect aabb)
        {
            foreach (var rect in _workingBuffer)
                if (rect.IntersectOrNull(aabb) is not null)
                    return true;
            return false;
        }

        // Delivers damage produced at <paramref name="sourceDfsIndex"/> to every retained backdrop later in DFS
        // order in the host, applying the output-inflation rule (((D ∩ AABB) ⊕ R) ∩ AABB added to the region).
        private static void DeliverToLaterRetained(HostScan host, int sourceDfsIndex, LtrbRect source)
        {
            foreach (var retained in host.Retained)
            {
                if (retained.DfsIndex <= sourceDfsIndex)
                    continue;

                var record = retained.Registration;
                if (record.Aabb is not { } aabbPixels)
                    continue;

                var aabb = aabbPixels.ToLtrbRectUnscaled();
                if (source.IntersectOrNull(aabb) is not { } damage)
                    continue;

                record.RetainedInputDirtyArea = LtrbRect.FullUnion(record.RetainedInputDirtyArea, damage);

                if (record.HostSpaceSamplingRadius > 0
                    && damage.Inflate(new Thickness(record.HostSpaceSamplingRadius)).IntersectOrNull(aabb) is { } halo)
                {
                    host.Collector.AddRect(halo);
                    host.ReceivedNewDamage = true;
                }
            }
        }

        // Propagates a child host's post-update damage up to its parent host as the child-host visual's coarse
        // subtree bounds (as bitmap caches propagate today), delivered to retained backdrops later than that
        // boundary and added to the parent host's region.
        private static void PropagateToParent(HostScan host)
        {
            var parent = host.Parent;
            if (parent == null || host.BoundaryCoarseBounds is not { } coarse || coarse.IsZeroSize)
                return;

            // Two halves. The AddRect half overlaps the update walk's own cache-boundary dirty emission (the
            // boundary's coarse bounds already reach the parent region there), so it looks redundant — but the
            // DeliverToLaterRetained half is NOT: it grows the retained-input areas of parent-host backdrops that
            // sit after this boundary in DFS. Do not "simplify away" this call by leaning on the walk's emission.
            parent.Collector.AddRect(coarse);
            parent.ReceivedNewDamage = true;
            DeliverToLaterRetained(parent, host.BoundaryDfsIndex, coarse);
        }
    }
}
