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
        | CompositionVisualChangedFields.Clip
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

    public override void NotifyAnimatedValueChanged(CompositionProperty offset)
    {
        base.NotifyAnimatedValueChanged(offset);
        if (offset == s_IdOfClipToBoundsProperty
            || offset == s_IdOfOpacityProperty
            || offset == s_IdOfSizeProperty)
            IsDirtyComposition = true;

        if (offset == s_IdOfSizeProperty
            || offset == s_IdOfAnchorPointProperty
            || offset == s_IdOfCenterPointProperty
            || offset == s_IdOfAdornedVisualProperty
            || offset == s_IdOfTransformMatrixProperty
            || offset == s_IdOfScaleProperty
            || offset == s_IdOfRotationAngleProperty
            || offset == s_IdOfOrientationProperty
            || offset == s_IdOfOffsetProperty)
            _combinedTransformDirty = true;
        
        if (offset == s_IdOfClipToBoundsProperty
            || offset == s_IdOfSizeProperty)
            _clipSizeDirty = true;
    }
}
