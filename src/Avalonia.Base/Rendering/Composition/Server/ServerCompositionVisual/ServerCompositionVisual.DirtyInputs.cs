using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    private bool _enqueuedForOwnPropertiesRecompute;

    private const CompositionVisualChangedFields CompositionFieldsMask
        = CompositionVisualChangedFields.Opacity
          | CompositionVisualChangedFields.OpacityAnimated
          | CompositionVisualChangedFields.OpacityMaskBrush
          | CompositionVisualChangedFields.Clip
          | CompositionVisualChangedFields.ClipToBounds
          | CompositionVisualChangedFields.ClipToBoundsAnimated
          | CompositionVisualChangedFields.Size
          | CompositionVisualChangedFields.SizeAnimated
          | CompositionVisualChangedFields.RenderOptions
          | CompositionVisualChangedFields.Effect;

    private const CompositionVisualChangedFields OwnBoundsUpdateFieldsMask =
        CompositionVisualChangedFields.Clip
        | CompositionVisualChangedFields.ClipToBounds
        | CompositionVisualChangedFields.ClipToBoundsAnimated
        | CompositionVisualChangedFields.Size
        | CompositionVisualChangedFields.SizeAnimated
        | CompositionVisualChangedFields.Effect;

    private const CompositionVisualChangedFields CombinedTransformFieldsMask =
        CompositionVisualChangedFields.Size
        | CompositionVisualChangedFields.SizeAnimated
        | CompositionVisualChangedFields.AnchorPoint
        | CompositionVisualChangedFields.AnchorPointAnimated
        | CompositionVisualChangedFields.CenterPoint
        | CompositionVisualChangedFields.CenterPointAnimated
        | CompositionVisualChangedFields.AdornedVisual
        | CompositionVisualChangedFields.TransformMatrix
        | CompositionVisualChangedFields.Scale
        | CompositionVisualChangedFields.ScaleAnimated
        | CompositionVisualChangedFields.RotationAngle
        | CompositionVisualChangedFields.RotationAngleAnimated
        | CompositionVisualChangedFields.Orientation
        | CompositionVisualChangedFields.OrientationAnimated
        | CompositionVisualChangedFields.Offset
        | CompositionVisualChangedFields.OffsetAnimated;

    private const CompositionVisualChangedFields ClipSizeDirtyMask =
        CompositionVisualChangedFields.Size
        | CompositionVisualChangedFields.SizeAnimated
        | CompositionVisualChangedFields.ClipToBounds
        | CompositionVisualChangedFields.Clip
        | CompositionVisualChangedFields.ClipToBoundsAnimated;

    private const CompositionVisualChangedFields ReadbackDirtyMask =
        CombinedTransformFieldsMask 
        | CompositionVisualChangedFields.Root 
        | CompositionVisualChangedFields.Visible
        | CompositionVisualChangedFields.VisibleAnimated;
        
    partial void OnFieldsDeserialized(CompositionVisualChangedFields changed)
    {
        if ((changed & CompositionFieldsMask) != 0) 
            TriggerCompositionFieldsDirty();
        if ((changed & CombinedTransformFieldsMask) != 0) 
            TriggerCombinedTransformDirty();

        if ((changed & ClipSizeDirtyMask) != 0) 
            TriggerClipSizeDirty();
        if((changed & OwnBoundsUpdateFieldsMask) != 0)
        {
            _ownBoundsDirty = true;
            EnqueueOwnPropertiesRecompute();
        }
        
        if((changed & ReadbackDirtyMask) != 0) 
            EnqueueForReadbackUpdate();

        if ((changed & (CompositionVisualChangedFields.Visible | CompositionVisualChangedFields.VisibleAnimated)) != 0)
            TriggerVisibleDirty();

        if ((changed & (CompositionVisualChangedFields.SizeAnimated | CompositionVisualChangedFields.Size)) != 0)
            SizeChanged();
    }
    

    public override void NotifyAnimatedValueChanged(CompositionProperty property)
    {
        base.NotifyAnimatedValueChanged(property);
        if (property == s_IdOfClipToBoundsProperty
            || property == s_IdOfOpacityProperty
            || property == s_IdOfSizeProperty) 
            TriggerCompositionFieldsDirty();

        if (property == s_IdOfSizeProperty
            || property == s_IdOfAnchorPointProperty
            || property == s_IdOfCenterPointProperty
            || property == s_IdOfAdornedVisualProperty
            || property == s_IdOfTransformMatrixProperty
            || property == s_IdOfScaleProperty
            || property == s_IdOfRotationAngleProperty
            || property == s_IdOfOrientationProperty
            || property == s_IdOfOffsetProperty) 
            TriggerCombinedTransformDirty();

        if (property == s_IdOfClipToBoundsProperty
            || property == s_IdOfSizeProperty
           ) TriggerClipSizeDirty();

        if (property == s_IdOfSizeProperty)
            SizeChanged();

        if (property == s_IdOfVisibleProperty)
            TriggerVisibleDirty();
    }

    protected virtual void SizeChanged()
    {
        
    }

    protected void TriggerCompositionFieldsDirty()
    {
        _compositionFieldsDirty = true;
        EnqueueOwnPropertiesRecompute();
    }
    
    protected void TriggerCombinedTransformDirty()
    {
        _combinedTransformDirty = true;
        EnqueueOwnPropertiesRecompute();
        EnqueueForReadbackUpdate();
    }
    
    protected void TriggerClipSizeDirty()
    {
        EnqueueOwnPropertiesRecompute();
        _clipSizeDirty = true;
    }
    
    protected void TriggerVisibleDirty()
    {
        EnqueueForReadbackUpdate();
        EnqueueForOwnBoundsRecompute();
    }
    
    partial void OnParentChanging()
    {
        if (Parent != null && _transformedSubTreeBounds.HasValue)
            Parent.AddExtraDirtyRect(_transformedSubTreeBounds.Value);
        AttHelper_ParentChanging();
    }
    
    partial void OnParentChanged()
    {
        if (Parent != null)
        {
            _delayPropagateNeedsBoundsUpdate = _delayPropagateIsDirtyForRender = true;
            EnqueueOwnPropertiesRecompute();
        }
        AttHelper_ParentChanged();
    }
    
    protected void AddExtraDirtyRect(LtrbRect rect)
    {
        _extraDirtyRect = _hasExtraDirtyRect ? _extraDirtyRect.Union(rect) : rect;
        _delayPropagateHasExtraDirtyRects = true;
        EnqueueOwnPropertiesRecompute();
    }
    

    protected void EnqueueForOwnBoundsRecompute()
    {
        _ownBoundsDirty = true;
        EnqueueOwnPropertiesRecompute();
    }

    protected void InvalidateContent()
    {
        _contentChanged = true;
        EnqueueForOwnBoundsRecompute();
    }

    private void EnqueueOwnPropertiesRecompute()
    {
        if(_enqueuedForOwnPropertiesRecompute)
            return;
        _enqueuedForOwnPropertiesRecompute = true;
        Compositor.EnqueueVisualForOwnPropertiesUpdatePass(this);
    }
}
