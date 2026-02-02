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

    // The bounds of this visual and its subtree
    // Coordinate space: local
    // Updated by: PreSubgraph, PostSubraph (recursive)
    private LtrbRect? _subTreeBounds;

    public LtrbRect? SubTreeBounds => _subTreeBounds;
    
    // The bounds of this visual and its subtree
    // Coordinate space: parent
    // Updated by: PostSubgraph
    private LtrbRect? _transformedSubTreeBounds;

    // The bounds backdrop effect in this Visual's subtree, we use it for dirty rects only
    // Coordinate space: local
    // Updated by: PreSubgraph, PostSubraph (recursive)
    private LtrbRect? _subTreeBackdropBounds;
    
    // The bounds backdrop effect in this Visual's subtree, we use it for dirty rects only
    // Coordinate space: parent
    // Updated by: PreSubgraph, PostSubraph (recursive)
    private LtrbRect? _transformedSubTreeBackdropBounds;

    // Visual's own clip area
    // Coordinate space: local
    private LtrbRect? _ownClipRect;

    
    private bool _hasExtraDirtyRect;
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
    private void PropagateFlags(bool fNeedsBoundingBoxUpdate, bool fDirtyForRender, bool fAdditionalDirtyRegion = false)
    {
        Root?.RequestUpdate();
        
        var parent = Parent;
        var setIsDirtyForRenderInSubgraph = fAdditionalDirtyRegion || fDirtyForRender;
        while (parent != null &&
               ((fNeedsBoundingBoxUpdate && !parent._needsBoundingBoxUpdate) ||
                (setIsDirtyForRenderInSubgraph && !parent._isDirtyForRenderInSubgraph)))
        {
            parent._needsBoundingBoxUpdate |= fNeedsBoundingBoxUpdate;
            parent._isDirtyForRenderInSubgraph |= setIsDirtyForRenderInSubgraph;

            parent = parent.Parent;
        }

        _needsBoundingBoxUpdate |= fNeedsBoundingBoxUpdate;
        _isDirtyForRender |= fDirtyForRender;
        
        // If node itself is dirty for render, we don't need to keep track of extra dirty rects
        _hasExtraDirtyRect = !fDirtyForRender && (_hasExtraDirtyRect || fAdditionalDirtyRegion);
    }
    
    public void RecomputeOwnProperties()
    {
        var setDirtyBounds = _contentChanged || _delayPropagateNeedsBoundsUpdate;
        var setDirtyForRender = _contentChanged || _delayPropagateIsDirtyForRender;
        var setHasExtraDirtyRect = _delayPropagateHasExtraDirtyRects;
        
        _delayPropagateIsDirtyForRender =
            _delayPropagateHasExtraDirtyRects =
                _delayPropagateIsDirtyForRender = false;
        
        _enqueuedForOwnPropertiesRecompute = false;
        if (_ownBoundsDirty)
        {
            _ownContentBounds = ComputeOwnContentBounds()?.NullIfZeroSize();
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
                RotationAngle, Orientation, Offset);
            
            setDirtyForRender = setDirtyBounds = true;
            
            ActHelper_CombinedTransformChanged();
        }

        
        setDirtyForRender |= _compositionFieldsDirty;

        _ownBoundsDirty = _clipSizeDirty = _combinedTransformDirty = _compositionFieldsDirty = false;
        PropagateFlags(setDirtyBounds, setDirtyForRender, setHasExtraDirtyRect);
    }
}