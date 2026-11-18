using System;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

/// <summary>
/// The backdrop state of a backdrop visual. Recomputed once per frame before the update walk.
/// See README.md in this directory for the terms.
/// </summary>
internal sealed class ServerCompositionVisualBackdropState
{
    public ServerCompositionVisualBackdropState(ServerCompositionVisual visual) => Visual = visual;

    public ServerCompositionVisual Visual { get; }

    /// <summary>
    /// The layer that the visual is drawn into: the nearest bitmap cache in the parent chain, or null for the
    /// root layer.
    /// </summary>
    public ServerCompositionVisualCache? Layer { get; set; }

    /// <summary>The backdrop AABB, or null when the visual has no backdrop bounds.</summary>
    public LtrbPixelRect? Aabb { get; set; }

    /// <summary>
    /// Maps the visual's parent space to layer space. The update walk does not change transforms, so it is
    /// valid for the whole frame.
    /// </summary>
    public Matrix ParentToLayerTransform { get; set; } = Matrix.Identity;

    /// <summary>Retained when true, volatile when false.</summary>
    public bool IsRetained { get; set; }

    /// <summary>The sampling radius of the effect in layer space.</summary>
    public double LayerSpaceSamplingRadius { get; set; }

    /// <summary>
    /// The input dirty rect in layer space (retained only). The update walk and the volatile pass grow it,
    /// the render walk clears it after the refresh.
    /// </summary>
    public LtrbRect? InputDirtyRect { get; set; }

    /// <summary>The backdrop snapshot (retained only). Created by the render walk on first use.</summary>
    public ServerCompositionBackdropSnapshot? Snapshot { get; set; }

    /// <summary>
    /// Disposes the snapshot. The render context must be current (true in the render walk). Other callers use
    /// <see cref="DisposeSnapshotWithContext"/>.
    /// </summary>
    public void DisposeSnapshot()
    {
        Snapshot?.Dispose();
        Snapshot = null;
    }

    /// <summary>
    /// Disposes the snapshot from outside of the render walk (update walk, deserialization), where the render
    /// context is not current yet.
    /// </summary>
    public void DisposeSnapshotWithContext()
    {
        if (Snapshot == null)
            return;
        try
        {
            using (Visual.Compositor.RenderInterface.EnsureCurrent())
                DisposeSnapshot();
        }
        catch (Exception ex)
        {
            // The context can't be made current (e.g. platform graphics are not ready). Leak the snapshot
            // instead of failing the update, same as ServerCompositionTarget.ResetRenderTarget.
            Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(Visual,
                "Unable to make the render interface current to dispose a backdrop snapshot: {Error}", ex);
            Snapshot = null;
        }
    }

    /// <summary>Makes the whole AABB the input dirty rect.</summary>
    public void MarkInputFullyDirty() => InputDirtyRect = Aabb?.ToLtrbRectUnscaled();
}

partial class ServerCompositionVisual
{
    // Number of backdrop visuals below this visual that are drawn into the same layer as its children, i.e.
    // with no bitmap cache in between. Does not include this visual. Kept up to date along the parent chain,
    // read by the update walk and the volatile pass.
    internal int _backdropVisualsInSubTree;

    // Whether this visual is counted in _backdropVisualsInSubTree of its parent chain.
    private bool _ownBackdropCounted;

    // Set when the children collection was replaced this frame. Cleared by the update walk's PostSubgraph.
    internal bool _childrenChanged;

    internal ServerCompositionVisualBackdropState? BackdropState { get; set; }

    // What this visual adds to the counter of its parent: itself if it is a backdrop visual (its backdrop
    // effect is drawn into the parent's layer even when the visual has a bitmap cache), plus the backdrop
    // visuals below it unless it has a bitmap cache.
    private int BackdropContributionToParent =>
        (_ownBackdropCounted ? 1 : 0) + (Cache != null ? 0 : _backdropVisualsInSubTree);

    // Adjusts the counter from `start` up the parent chain, up to and including the first visual with a
    // bitmap cache.
    private static void PropagateBackdropCountDelta(ServerCompositionVisual? start, int delta)
    {
        if (delta == 0)
            return;
        for (var n = start; n != null; n = n.Parent)
        {
            n._backdropVisualsInSubTree += delta;
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
        var has = BackdropEffect != null;
        if (has != _ownBackdropCounted)
        {
            PropagateBackdropCountDelta(Parent, has ? 1 : -1);
            _ownBackdropCounted = has;
        }

        UpdateBackdropVisualRegistration();
    }

    private LtrbPixelRect? ComputeBackdropAabb(Matrix parentToLayerTransform)
    {
        LtrbRect? layerRect;
        if (BackdropEffectBoundsSource == BackdropEffectBoundsSource.Layout)
        {
            if (!Visible || Size.X <= 0 || Size.Y <= 0)
                return null;

            var layoutBounds = new LtrbRect(0, 0, Size.X, Size.Y);
            var localToLayer = _ownTransform is { } ownTransform
                ? ownTransform * parentToLayerTransform
                : parentToLayerTransform;
            layerRect = layoutBounds.TransformToAABB(localToLayer);
        }
        else
            layerRect = _transformedSubTreeBounds?.TransformToAABB(parentToLayerTransform);

        return layerRect is { IsZeroSize: false } bounds ? LtrbPixelRect.FromRectUnscaled(bounds) : null;
    }

    // Cache is still the old one here. Without the bitmap cache, the backdrop visuals below count for the
    // parent chain again.
    private void Backdrop_OnCacheModeChanging()
    {
        if (Cache != null)
            PropagateBackdropCountDelta(Parent, _backdropVisualsInSubTree);
    }

    // Cache is the new one here. With a bitmap cache, the backdrop visuals below no longer count for the
    // parent chain.
    private void Backdrop_OnCacheModeChanged()
    {
        if (Cache != null)
            PropagateBackdropCountDelta(Parent, -_backdropVisualsInSubTree);
    }

    // Gives the same result whether Root or BackdropEffect is set first.
    internal void UpdateBackdropVisualRegistration()
    {
        var shouldRegister = BackdropEffect != null && Root != null;
        if (shouldRegister)
        {
            if (BackdropState == null)
            {
                BackdropState = new ServerCompositionVisualBackdropState(this);
                Root!.RegisterBackdropVisual(this);
            }
        }
        else if (BackdropState != null)
            Root?.UnregisterBackdropVisual(this);
    }

    private void Backdrop_OnRootChanging()
    {
        // Root is still the old target here.
        if (BackdropState != null)
            Root?.UnregisterBackdropVisual(this);
    }

    private void Backdrop_OnRootChanged() => UpdateBackdropVisualRegistration();

    internal void NotifyChildrenChanged() => _childrenChanged = true;

    /// <summary>
    /// Recomputes the backdrop state: layer, AABB, retained or volatile. Runs in
    /// <see cref="ServerCompositionTarget.Update"/> before the update walk. It never adds dirty rects: when the
    /// layer or the AABB changes, it marks the visual dirty, and the update walk collects the old and the new
    /// bounds at the visual's place in draw order.
    /// </summary>
    internal void RecomputeBackdropState(double rootScaling)
    {
        var state = BackdropState;
        if (state == null)
            return;

        // Without an explicit backdrop cache mode: retained if the effect has a sampling radius.
        var radius = BackdropEffect.GetBackdropSamplingRadius();
        state.IsRetained = BackdropEffectCache switch
        {
            ServerCompositionRetainedBackdropEffectCache => true,
            ServerCompositionVolatileBackdropEffectCache => false,
            _ => radius > 0
        };

        // Find the layer and the transform to it.
        var transform = Matrix.Identity;
        ServerCompositionVisualCache? layer = null;
        for (var current = Parent; current != null; current = current.Parent)
        {
            if (current.Cache != null)
            {
                layer = current.Cache;
                break;
            }

            if (current._ownTransform.HasValue)
                transform *= current._ownTransform.Value;
        }

        // Layer space of the root layer is device pixels.
        if (layer == null)
            transform *= Matrix.CreateScale(rootScaling, rootScaling);

        state.ParentToLayerTransform = transform;

        // The radius is in the visual's local space. Scale it by the uniform scale of the local to layer
        // transform.
        var contentToLayer = _ownTransform.HasValue ? _ownTransform.Value * transform : transform;
        state.LayerSpaceSamplingRadius = radius * Math.Sqrt(Math.Abs(contentToLayer.GetDeterminant()));

        // Subtree bounds are the ones of the previous update walk here. The walk corrects the AABB if they
        // change (CheckBackdropStaleAabb).
        var newAabb = ComputeBackdropAabb(transform);

        var layerChanged = !ReferenceEquals(state.Layer, layer);
        var aabbChanged = state.Aabb != newAabb;

        state.Aabb = newAabb;
        state.Layer = layer;

        if (layerChanged || aabbChanged)
        {
            // The snapshot no longer matches the input: make the whole new AABB the input dirty rect. The
            // snapshot object is kept for a move or resize (it recreates its surface when the size changes).
            if (layerChanged || newAabb == null)
                state.DisposeSnapshotWithContext();
            state.InputDirtyRect = newAabb?.ToLtrbRectUnscaled();
            PropagateFlags(false, true);
        }
        else if (!state.IsRetained)
            // Volatile (e.g. the backdrop cache mode changed): the snapshot is not needed.
            state.DisposeSnapshotWithContext();
    }
}
