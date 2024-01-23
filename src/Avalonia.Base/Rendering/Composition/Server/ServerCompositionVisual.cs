using System;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side <see cref="CompositionVisual"/> counterpart.
    /// Is responsible for computing the transformation matrix, for applying various visual
    /// properties before calling visual-specific drawing code and for notifying the
    /// <see cref="ServerCompositionTarget"/> for new dirty rects
    /// </summary>
    partial class ServerCompositionVisual : ServerObject
    {
        private bool _isDirtyForUpdate;
        private Rect _oldOwnContentBounds;
        private bool _isBackface;
        private Rect? _transformedClipBounds;
        private Rect _combinedTransformedClipBounds;

        protected virtual void RenderCore(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
        {
        }

        public void Render(CompositorDrawingContextProxy canvas, Rect currentTransformedClip)
        {
            if (Visible == false || IsVisibleInFrame == false)
                return;
            if (Opacity == 0)
                return;

            currentTransformedClip = currentTransformedClip.Intersect(_combinedTransformedClipBounds);
            if (currentTransformedClip.Width == 0 && currentTransformedClip.Height == 0)
                return;

            Root!.RenderedVisuals++;
            Root!.DebugEvents?.IncrementRenderedVisuals();

            var boundsRect = new Rect(new Size(Size.X, Size.Y));

            if (AdornedVisual != null)
            {
                canvas.PostTransform = Matrix.Identity;
                canvas.Transform = Matrix.Identity;
                if (AdornerIsClipped)
                    canvas.PushClip(AdornedVisual._combinedTransformedClipBounds);
            }
            var transform = GlobalTransformMatrix;
            canvas.PostTransform = transform;
            canvas.Transform = Matrix.Identity;

            if (Effect != null)
                canvas.PushEffect(Effect);
            
            if (Opacity != 1)
                canvas.PushOpacity(Opacity, ClipToBounds ? boundsRect : null);
            if (ClipToBounds && !HandlesClipToBounds)
                canvas.PushClip(Root!.SnapToDevicePixels(boundsRect));
            if (Clip != null) 
                canvas.PushGeometryClip(Clip);
            if (OpacityMaskBrush != null)
                canvas.PushOpacityMask(OpacityMaskBrush, boundsRect);

            canvas.RenderOptions = RenderOptions;

            RenderCore(canvas, currentTransformedClip);
            
            // Hack to force invalidation of SKMatrix
            canvas.PostTransform = transform;
            canvas.Transform = Matrix.Identity;

            if (OpacityMaskBrush != null)
                canvas.PopOpacityMask();
            if (Clip != null)
                canvas.PopGeometryClip();
            if (ClipToBounds && !HandlesClipToBounds)
                canvas.PopClip();
            if (AdornedVisual != null && AdornerIsClipped)
                canvas.PopClip();
            if (Opacity != 1)
                canvas.PopOpacity();
            
            if (Effect != null)
                canvas.PopEffect();
        }

        protected virtual bool HandlesClipToBounds => false;

        private ReadbackData _readback0, _readback1, _readback2;

        /// <summary>
        /// Obtains "readback" data - the data that is sent from the render thread to the UI thread
        /// in non-blocking manner. Used mostly by hit-testing
        /// </summary>
        public ref ReadbackData GetReadback(int idx)
        {
            if (idx == 0)
                return ref _readback0;
            if (idx == 1)
                return ref _readback1;
            return ref _readback2;
        }

        public Matrix CombinedTransformMatrix { get; private set; } = Matrix.Identity;
        public Matrix GlobalTransformMatrix { get; private set; }

        public record struct UpdateResult(Rect? Bounds, bool InvalidatedOld, bool InvalidatedNew)
        {
            public UpdateResult() : this(null, false, false)
            {
                
            }
        }
        
        public virtual UpdateResult Update(ServerCompositionTarget root)
        {
            if (Parent == null && Root == null)
                return default;

            var wasVisible = IsVisibleInFrame;

            if(Parent != null)
            {
                RenderOptions = RenderOptions.MergeWith(Parent.RenderOptions);
            }

            // Calculate new parent-relative transform
            if (_combinedTransformDirty)
            {
                CombinedTransformMatrix = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint,
                    // HACK: Ignore RenderTransform set by the adorner layer
                    AdornedVisual != null ? Matrix.Identity : TransformMatrix,
                    Scale, RotationAngle, Orientation, Offset);
                _combinedTransformDirty = false;
            }

            var parentTransform = (AdornedVisual ?? Parent)?.GlobalTransformMatrix ?? Matrix.Identity;

            var newTransform = CombinedTransformMatrix * parentTransform;

            // Check if visual was moved and recalculate face orientation
            var positionChanged = false;
            if (GlobalTransformMatrix != newTransform)
            {
                _isBackface = Vector3.Transform(
                    new Vector3(0, 0, float.PositiveInfinity), MatrixUtils.ToMatrix4x4(GlobalTransformMatrix)).Z <= 0;
                positionChanged = true;
            }

            var oldTransformedContentBounds = TransformedOwnContentBounds;
            var oldCombinedTransformedClipBounds = _combinedTransformedClipBounds;

            if (_parent?.IsDirtyComposition == true)
            {
                IsDirtyComposition = true;
                _isDirtyForUpdate = true;
            }

            var invalidateOldBounds = _isDirtyForUpdate;
            var invalidateNewBounds = _isDirtyForUpdate;

            GlobalTransformMatrix = newTransform;

            var ownBounds = OwnContentBounds;
            
            // Since padding is applied in the current visual's coordinate space we expand bounds before transforming them
            if (Effect != null)
                ownBounds = ownBounds.Inflate(Effect.GetEffectOutputPadding());
            
            if (ownBounds != _oldOwnContentBounds || positionChanged)
            {
                _oldOwnContentBounds = ownBounds;
                if (ownBounds.Width == 0 && ownBounds.Height == 0)
                    TransformedOwnContentBounds = default;
                else
                    TransformedOwnContentBounds =
                        ownBounds.TransformToAABB(GlobalTransformMatrix);
            }

            if (_clipSizeDirty || positionChanged)
            {
                Rect? transformedVisualBounds = null;
                Rect? transformedClipBounds = null;
                
                if (ClipToBounds)
                    transformedVisualBounds = new Rect(new Size(Size.X, Size.Y)).TransformToAABB(GlobalTransformMatrix);
                
                 if (Clip != null)
                     transformedClipBounds = Clip.Bounds.TransformToAABB(GlobalTransformMatrix);

                 if (transformedVisualBounds != null && transformedClipBounds != null)
                     _transformedClipBounds = transformedVisualBounds.Value.Intersect(transformedClipBounds.Value);
                 else if (transformedVisualBounds != null)
                     _transformedClipBounds = transformedVisualBounds;
                 else if (transformedClipBounds != null)
                     _transformedClipBounds = transformedClipBounds;
                 else
                     _transformedClipBounds = null;
                 
                _clipSizeDirty = false;
            }

            _combinedTransformedClipBounds =
                (AdornerIsClipped ? AdornedVisual?._combinedTransformedClipBounds : null)
                ?? (Parent?.Effect == null ? Parent?._combinedTransformedClipBounds : null)
                ?? new Rect(Root!.Size);

            if (_transformedClipBounds != null)
                _combinedTransformedClipBounds = _combinedTransformedClipBounds.Intersect(_transformedClipBounds.Value);

            EffectiveOpacity = Opacity * (Parent?.EffectiveOpacity ?? 1);

            IsHitTestVisibleInFrame = _parent?.IsHitTestVisibleInFrame != false
                                      && Visible
                                      && !_isBackface
                                      && (_combinedTransformedClipBounds.Width != 0 || _combinedTransformedClipBounds.Height != 0);

            IsVisibleInFrame = IsHitTestVisibleInFrame
                               && _parent?.IsVisibleInFrame != false
                               && EffectiveOpacity > 0.04;

            if (wasVisible != IsVisibleInFrame || positionChanged)
            {
                invalidateOldBounds |= wasVisible;
                invalidateNewBounds |= IsVisibleInFrame;
            }

            // Invalidate new bounds
            if (invalidateNewBounds)
                AddDirtyRect(TransformedOwnContentBounds.Intersect(_combinedTransformedClipBounds));

            if (invalidateOldBounds)
                AddDirtyRect(oldTransformedContentBounds.Intersect(oldCombinedTransformedClipBounds));


            _isDirtyForUpdate = false;
            
            // Update readback indices
            var i = Root!.Readback;
            ref var readback = ref GetReadback(i.WriteIndex);
            readback.Revision = root.Revision;
            readback.Matrix = GlobalTransformMatrix;
            readback.TargetId = Root.Id;
            readback.Visible = IsHitTestVisibleInFrame;
            return new(TransformedOwnContentBounds, invalidateNewBounds, invalidateOldBounds);
        }

        protected void AddDirtyRect(Rect rc)
        {
            if (rc == default)
                return;
            Root?.AddDirtyRect(rc);
        }

        /// <summary>
        /// Data that can be read from the UI thread
        /// </summary>
        public struct ReadbackData
        {
            public Matrix Matrix;
            public ulong Revision;
            public long TargetId;
            public bool Visible;
        }

        partial void DeserializeChangesExtra(BatchStreamReader c)
        {
            ValuesInvalidated();
        }

        partial void OnRootChanging()
        {
            if (Root != null)
            {
                Root.RemoveVisual(this);
                OnDetachedFromRoot(Root);
            }
        }

        protected virtual void OnDetachedFromRoot(ServerCompositionTarget target)
        {
        }

        partial void OnRootChanged()
        {
            if (Root != null)
            {
                Root.AddVisual(this);
                OnAttachedToRoot(Root);
            }
        }

        protected virtual void OnAttachedToRoot(ServerCompositionTarget target)
        {
        }

        protected override void ValuesInvalidated()
        {
            _isDirtyForUpdate = true;
            Root?.Invalidate();
        }

        public bool IsVisibleInFrame { get; set; }
        public bool IsHitTestVisibleInFrame { get; set; }
        public double EffectiveOpacity { get; set; }
        public Rect TransformedOwnContentBounds { get; set; }
        public virtual Rect OwnContentBounds => new Rect(0, 0, Size.X, Size.Y);
    }
}
