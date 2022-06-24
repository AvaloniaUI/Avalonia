namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositionVisual
{
    protected bool IsDirtyComposition;
    private bool _combinedTransformDirty;
    private bool _clipSizeDirty;
    
    private const CompositionVisualChangedFields CompositionFieldsMask
        = CompositionVisualChangedFields.Opacity
          | CompositionVisualChangedFields.OpacityAnimated
          | CompositionVisualChangedFields.OpacityMaskBrush
          | CompositionVisualChangedFields.Clip
          | CompositionVisualChangedFields.ClipToBounds
          | CompositionVisualChangedFields.ClipToBoundsAnimated
          | CompositionVisualChangedFields.Size
          | CompositionVisualChangedFields.SizeAnimated;

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
        | CompositionVisualChangedFields.ClipToBoundsAnimated;
        
    partial void OnFieldsDeserialized(CompositionVisualChangedFields changed)
    {
        if ((changed & CompositionFieldsMask) != 0)
            IsDirtyComposition = true;
        if ((changed & CombinedTransformFieldsMask) != 0)
            _combinedTransformDirty = true;
        if ((changed & ClipSizeDirtyMask) != 0)
            _clipSizeDirty = true;
    }

    public override void NotifyAnimatedValueChanged(int offset)
    {
        base.NotifyAnimatedValueChanged(offset);
        if (offset == s_OffsetOf_clipToBounds
            || offset == s_OffsetOf_opacity
            || offset == s_OffsetOf_size)
            IsDirtyComposition = true;

        if (offset == s_OffsetOf_size
            || offset == s_OffsetOf_anchorPoint
            || offset == s_OffsetOf_centerPoint
            || offset == s_OffsetOf_adornedVisual
            || offset == s_OffsetOf_transformMatrix
            || offset == s_OffsetOf_scale
            || offset == s_OffsetOf_rotationAngle
            || offset == s_OffsetOf_orientation
            || offset == s_OffsetOf_offset)
            _combinedTransformDirty = true;
        
        if (offset == s_OffsetOf_clipToBounds
            || offset == s_OffsetOf_size)
            _clipSizeDirty = true;
    }
}