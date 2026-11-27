using System.Diagnostics;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    // Dirty flags, handled by RecomputeOwnProperties
    private bool _combinedTransformDirty;
    private bool _clipSizeDirty;
    private bool _ownBoundsDirty;
    private bool _compositionFieldsDirty;
    private bool _contentChanged;

    private bool _delayPropagateNeedsBoundsUpdate;
    private bool _delayPropagateIsDirtyForRender;
    private bool _delayPropagateHasExtraDirtyRects;
    
    // Dirty rect, re-render flags, set by PropagateFlags
    private bool _needsBoundingBoxUpdate;
    private bool _isDirtyForRender;
    private bool _isDirtyForRenderInSubgraph;

    // Transform that accounts for offset, RenderTransform and other properties of _this_ visual that is
    // used to transform to parent's coordinate space
    // Updated by RecomputeOwnProperties pass
    private Matrix? _ownTransform;
    public Matrix? OwnTransform => _ownTransform;
    
    // The bounds of this visual's own content, excluding children
    // Coordinate space: local
    // Updated by RecomputeOwnProperties pass
    private LtrbRect? _ownContentBounds;

    // Set on a bitmap cache root when its whole cache layer must be redrawn. Handled by the update walk.
    // The cache layer draws the root without its transform, clip and opacity, so changes to those do not set it.
    private bool _cacheNeedsFullInvalidate;

    // The bounds of this visual and its subtree
    // Coordinate space: local
    // Updated by: PreSubgraph, PostSubraph (recursive)
    private LtrbRect? _subTreeBounds;

    public LtrbRect? SubTreeBounds => _subTreeBounds;

    // The bounds of this visual and its subtree
    // Coordinate space: parent
    // Updated by: PostSubgraph
    private LtrbRect? _transformedSubTreeBounds;

    // Visual's own clip area
    // Coordinate space: local
    private LtrbRect? _ownClipRect;

    
    private bool _needsToAddExtraDirtyRectToDirtyRegion;
    private LtrbRect _extraDirtyRect;

    public virtual LtrbRect? ComputeOwnContentBounds() => null;
    
    public Matrix CombinedTransformMatrix { get; private set; } = Matrix.Identity;
    
    
    // WPF's cheatsheet
    //-----------------------------------------------------------------------------
    //  Node Operation   | NeedsToBe     | NeedsBBoxUpdate | HasNodeThat    | Visit
    //                   | AddedToDirty  | (parent chain)  | NeedsToBeAdded | child
    //                   | Region        |                 | ToDirtyRegion  |
    //                   |               |                 | (parent chain) |
    //=============================================================================
    //  Set transform    |   Y           |   Y             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  Set opacity      |   Y           |   N             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  Set clip         |   Y           |   Y             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  AttachRenderData |   Y           |   Y             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  FreeRenderData   |   Y           |   Y             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  InsertChild      |   N           |   Y             |   Y
    //                   |   Y(child)    |   N             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  InsertChildAt    |   N           |   Y             |   Y
    //                   |   Y(child)    |   N             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  ZOrderChild      |   N           |   N             |   Y
    //                   |   Y(child)    |   N             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  ReplaceChild     |   Y           |   Y             |   Y(N)
    //  -----------------+---------------+-----------------+-----------------------
    //  RemoveChild      |   Y           |   Y             |   Y(N)
    private void PropagateFlags(bool needsBoundingBoxUpdate, bool dirtyForRender, bool additionalDirtyRegion = false)
    {
        Root?.RequestUpdate();
        
        var parent = Parent;
        var setIsDirtyForRenderInSubgraph = additionalDirtyRegion || dirtyForRender;
        while (parent != null &&
               ((needsBoundingBoxUpdate && !parent._needsBoundingBoxUpdate) ||
                (setIsDirtyForRenderInSubgraph && !parent._isDirtyForRenderInSubgraph)))
        {
            parent._needsBoundingBoxUpdate |= needsBoundingBoxUpdate;
            parent._isDirtyForRenderInSubgraph |= setIsDirtyForRenderInSubgraph;

            parent = parent.Parent;
        }

        _needsBoundingBoxUpdate |= needsBoundingBoxUpdate;
        _isDirtyForRender |= dirtyForRender;
        
        // Extra dirty rects (for example, of removed children) are dropped below, because the bounds of a dirty
        // visual cover them. A bitmap cache root adds its bounds only to the outer layer, so its cache layer
        // loses these rects and must be redrawn in full.
        if (dirtyForRender && Cache != null && (_needsToAddExtraDirtyRectToDirtyRegion || additionalDirtyRegion))
            _cacheNeedsFullInvalidate = true;

        // If node itself is dirty for render, we don't need to keep track of extra dirty rects
        _needsToAddExtraDirtyRectToDirtyRegion = !dirtyForRender && (_needsToAddExtraDirtyRectToDirtyRegion || additionalDirtyRegion);
    }
    
    public void RecomputeOwnProperties()
    {
        var setDirtyBounds = _contentChanged || _delayPropagateNeedsBoundsUpdate;
        var setDirtyForRender = _contentChanged || _delayPropagateIsDirtyForRender;
        var setHasExtraDirtyRect = _delayPropagateHasExtraDirtyRects;
        
        _delayPropagateIsDirtyForRender =
            _delayPropagateHasExtraDirtyRects =
                _delayPropagateNeedsBoundsUpdate = false;
        
        _enqueuedForOwnPropertiesRecompute = false;
        if (_ownBoundsDirty)
        {
            _ownContentBounds = ComputeOwnContentBounds()?.NullIfZeroSize();
            // The backdrop effect is drawn in the layout rect, also when the visual has no content there.
            if (BackdropEffect != null && BackdropEffectBoundsSource == BackdropEffectBoundsSource.Layout
                                       && Size.X > 0 && Size.Y > 0)
                _ownContentBounds = LtrbRect.FullUnion(_ownContentBounds, new LtrbRect(0, 0, Size.X, Size.Y));
            // The old own content bounds are lost here, so the cache layer is redrawn as a whole.
            if (Cache != null)
                _cacheNeedsFullInvalidate = true;
            setDirtyForRender = setDirtyBounds = true;
        }

        if (_clipSizeDirty)
        {
            LtrbRect? clip = null;
            if (Clip != null)
                clip = new(Clip.Bounds);
            if (ClipToBounds)
            {
                var bounds = new LtrbRect(0, 0, Size.X, Size.Y);
                clip = clip?.IntersectOrEmpty(bounds) ?? bounds;
            }

            if (_ownClipRect != clip)
            {
                _ownClipRect = clip;
                setDirtyForRender = setDirtyBounds = true;
            }
        }

        if (_combinedTransformDirty)
        {
            _ownTransform = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint, TransformMatrix, Scale,
                RotationAngle, Orientation, Offset + Translation);
            
            setDirtyForRender = setDirtyBounds = true;
            
            AttHelper_CombinedTransformChanged();
        }

        
        setDirtyForRender |= _compositionFieldsDirty;

        _ownBoundsDirty = _clipSizeDirty = _combinedTransformDirty = _compositionFieldsDirty = false;
        PropagateFlags(setDirtyBounds, setDirtyForRender, setHasExtraDirtyRect);
    }
}
