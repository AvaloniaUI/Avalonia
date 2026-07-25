using System;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// Per-backdrop state for a <see cref="ServerCompositionVisual"/> with a supported backdrop effect,
/// recomputed once per frame before the update walk. Holds the pixel-aligned AABB in host space, the layer
/// host it belongs to, the composed visual-&gt;host transform used to build the AABB and the retained/volatile
/// classification.
/// </summary>
internal sealed class ServerCompositionVisualBackdropState
{
    public ServerCompositionVisualBackdropState(ServerCompositionVisual visual) => Visual = visual;

    public ServerCompositionVisual Visual { get; }

    /// <summary>Nearest ancestor bitmap cache, or null for the render target ("root") host.</summary>
    public ServerCompositionVisualCache? Host { get; set; }

    /// <summary>Pixel-aligned AABB in host space for this frame, or null when the visual has no bounds.</summary>
    public LtrbPixelRect? Aabb { get; set; }

    /// <summary>
    /// Composed transform mapping the visual's <see cref="ServerCompositionVisual._transformedSubTreeBounds"/>
    /// (i.e. parent-local space) into host space. Stashed for the walk-time stale-AABB check; ancestor
    /// transforms are only rewritten by the pre-walk passes, so it stays valid for the whole frame.
    /// </summary>
    public Matrix VisualToHostTransform { get; set; } = Matrix.Identity;

    /// <summary>Retained when true, volatile when false.</summary>
    public bool IsRetained { get; set; }

    /// <summary>The effect's input sampling radius expressed in host-space pixels (R, DIPs × host scale).</summary>
    public double HostSpaceSamplingRadius { get; set; }

    /// <summary>
    /// Retained backdrop's accumulated pending input damage, in host space. Grown by the update walk's
    /// capture (working set ∩ AABB) and set to the whole AABB on full invalidation. Consumed by the render pass.
    /// </summary>
    public LtrbRect? RetainedInputDirtyArea { get; set; }

    /// <summary>The retained capture store, created lazily by the render pass.</summary>
    public ServerCompositionBackdropStore? RetainedState { get; set; }

    /// <summary>
    /// Disposes the retained store (freeing its layer) and clears the slot. Idempotent. Only safe when the
    /// render context is already current (i.e. from the render pass); off-context callers must use
    /// <see cref="DisposeRetainedStateWithContext"/>.
    /// </summary>
    public void DisposeAndClearRetainedState()
    {
        RetainedState?.Dispose();
        RetainedState = null;
    }

    /// <summary>
    /// Disposes the retained store with the render interface made current, for callers running outside the
    /// render pass (the update walk / deserialization) where the GPU context isn't already current. No-op
    /// when there is nothing to dispose.
    /// </summary>
    public void DisposeRetainedStateWithContext()
    {
        if (RetainedState == null)
            return;
        try
        {
            using (Visual.Compositor.RenderInterface.EnsureCurrent())
                DisposeAndClearRetainedState();
        }
        catch (Exception ex)
        {
            // The context couldn't be made current (e.g. platform graphics not ready). Abandon the resource
            // rather than crashing the update; mirrors ServerCompositionTarget.ResetRenderTarget's fallback.
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(Visual,
                "Unable to make the render interface current to dispose a backdrop cache: {Error}", ex);
            RetainedState = null;
        }
    }

    /// <summary>Marks the entire input (the whole AABB) as pending, overriding any partial capture.</summary>
    public void MarkRetainedInputFull() => RetainedInputDirtyArea = Aabb?.ToLtrbRectUnscaled();
}

partial class ServerCompositionVisual
{
    // Number of backdrop visuals in this node's subtree that are visible to this node's layer host,
    // i.e. reachable without crossing a bitmap-cache boundary. Excludes this node itself. Maintained
    // eagerly along the parent chain; consumed by the update pass.
    internal int _backdropsInSubTree;

    // Tracks whether this node's own backdrop is currently accounted for in the parent chain counters.
    private bool _ownBackdropCounted;

    // Per-frame flag raised when this node's children collection was replaced during deserialization.
    // Cleared in the update walk's PostSubgraph.
    internal bool _childrenChanged;

    internal ServerCompositionVisualBackdropState? BackdropState { get; set; }

    // The count this node contributes to its parent chain: its own backdrop (which always crosses this
    // node's own cache) plus, unless this node is a cache boundary, the backdrops reaching it from below.
    private int BackdropContributionToParent =>
        (_ownBackdropCounted ? 1 : 0) + (Cache != null ? 0 : _backdropsInSubTree);

    // Walks up from `start`, adjusting the counter; propagation stops at (does not cross) cache boundaries.
    private static void PropagateBackdropCountDelta(ServerCompositionVisual? start, int delta)
    {
        if (delta == 0)
            return;
        for (var n = start; n != null; n = n.Parent)
        {
            n._backdropsInSubTree += delta;
            if (n.Cache != null)
                break;
        }
    }

    private void Backdrop_OnParentChanging() =>
        // Parent is still the old parent here.
        PropagateBackdropCountDelta(Parent, -BackdropContributionToParent);

    private void Backdrop_OnParentChanged() =>
        // Parent is the new parent here.
        PropagateBackdropCountDelta(Parent, BackdropContributionToParent);

    partial void OnBackdropEffectChanged()
    {
        var has = BackdropEffect.IsSupportedBackdropEffect();
        if (has != _ownBackdropCounted)
        {
            PropagateBackdropCountDelta(Parent, has ? 1 : -1);
            _ownBackdropCounted = has;
        }

        UpdateBackdropRegistration();
    }

    // Called from OnCacheModeChanging, before Cache is reset to null. If this node was a cache boundary
    // its descendant backdrops were stopping here; removing the boundary lets them propagate upwards again.
    private void Backdrop_OnCacheModeChanging()
    {
        if (Cache != null)
            PropagateBackdropCountDelta(Parent, _backdropsInSubTree);
    }

    // Called from OnCacheModeChanged, after Cache is assigned. If this node became a cache boundary its
    // descendant backdrops must stop here instead of reaching ancestors.
    private void Backdrop_OnCacheModeChanged()
    {
        if (Cache != null)
            PropagateBackdropCountDelta(Parent, -_backdropsInSubTree);
    }

    // Idempotent, order-independent (Root-before-Effect and Effect-before-Root both converge).
    internal void UpdateBackdropRegistration()
    {
        var shouldRegister = BackdropEffect.IsSupportedBackdropEffect() && Root != null;
        if (shouldRegister)
        {
            if (BackdropState == null)
            {
                BackdropState = new ServerCompositionVisualBackdropState(this);
                Root!.RegisterBackdrop(this);
            }
        }
        else if (BackdropState != null)
            Root?.UnregisterBackdrop(this);
    }

    private void Backdrop_OnRootChanging()
    {
        // Root still points at the old target; drop any registration tied to it.
        if (BackdropState != null)
            Root?.UnregisterBackdrop(this);
    }

    private void Backdrop_OnRootChanged() => UpdateBackdropRegistration();

    internal void NotifyChildrenChanged() => _childrenChanged = true;

    /// <summary>
    /// Recomputes this backdrop's registry record: its layer host, host-space AABB and classification.
    /// Runs in <see cref="ServerCompositionTarget.Update"/> before the update walk. Never adds damage rects;
    /// on a host or pixel-aligned AABB change it resets the retained state and marks the visual
    /// dirty-for-render so the walk emits <c>old ∪ new</c> at the visual's own DFS position.
    /// </summary>
    internal void RecomputeBackdropRegistration(double rootScaling)
    {
        var record = BackdropState;
        if (record == null)
            return;

        // The UI preference wins; a null (Default) mode falls back to R > 0.
        var radius = BackdropEffect.GetBackdropSamplingRadius();
        record.IsRetained = BackdropEffectCache switch
        {
            ServerCompositionRetainedBackdropEffectCacheMode => true,
            ServerCompositionVolatileBackdropEffectCacheMode => false,
            _ => radius > 0
        };

        // Walk up to the nearest cache host, accumulating the transform into host space.
        var transform = Matrix.Identity;
        ServerCompositionVisualCache? host = null;
        for (var ancestor = Parent; ancestor != null; ancestor = ancestor.Parent)
        {
            if (ancestor.Cache != null)
            {
                host = ancestor.Cache;
                break;
            }

            if (ancestor._ownTransform.HasValue)
                transform *= ancestor._ownTransform.Value;
        }

        // For the root host, host space is device pixels: apply the target scaling.
        if (host == null)
            transform *= Matrix.CreateScale(rootScaling, rootScaling);

        record.VisualToHostTransform = transform;

        // R is defined in DIPs; express it in host-space pixels using the uniform scale of the full
        // content→host transform (this visual's own transform folded in, matching how the AABB is sized).
        var contentToHost = _ownTransform.HasValue ? _ownTransform.Value * transform : transform;
        record.HostSpaceSamplingRadius = radius * Math.Sqrt(Math.Abs(contentToHost.GetDeterminant()));

        // Build the pixel-aligned host-space AABB from the last completed walk's transformed subtree bounds.
        LtrbPixelRect? newAabb = null;
        if (_transformedSubTreeBounds.HasValue)
        {
            var hostRect = _transformedSubTreeBounds.Value.TransformToAABB(transform);
            if (!hostRect.IsZeroSize)
                newAabb = LtrbPixelRect.FromRectUnscaled(hostRect);
        }

        var hostChanged = !ReferenceEquals(record.Host, host);
        var aabbChanged = record.Aabb != newAabb;

        record.Aabb = newAabb;
        record.Host = host;

        if (hostChanged || aabbChanged)
        {
            // Host/size change ⇒ the retained texture is no longer valid; drop it so the render pass
            // reallocates and re-ingests. Reset the input to the whole (new) AABB for that re-ingest.
            record.DisposeRetainedStateWithContext();
            record.RetainedInputDirtyArea = newAabb?.ToLtrbRectUnscaled();
            PropagateFlags(false, true);
        }
        else if (!record.IsRetained)
            // Classified volatile this frame (e.g. after a cache-hint change): the retained texture is dead weight.
            record.DisposeRetainedStateWithContext();
    }
}
