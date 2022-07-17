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
            if(Visible == false || IsVisibleInFrame == false)
                return;
            if(Opacity == 0)
                return;

            currentTransformedClip = currentTransformedClip.Intersect(_combinedTransformedClipBounds);
            if(currentTransformedClip.IsEmpty)
                return;

            Root!.RenderedVisuals++;
            
            var transform = GlobalTransformMatrix;
            canvas.PostTransform = MatrixUtils.ToMatrix(transform);
            canvas.Transform = Matrix.Identity;
            if (Opacity != 1)
                canvas.PushOpacity(Opacity);
            var boundsRect = new Rect(new Size(Size.X, Size.Y));
            if(ClipToBounds)
                canvas.PushClip(Root!.SnapToDevicePixels(boundsRect));
            if (Clip != null) 
                canvas.PushGeometryClip(Clip);
            if(OpacityMaskBrush != null)
                canvas.PushOpacityMask(OpacityMaskBrush, boundsRect);
            
            RenderCore(canvas, currentTransformedClip);
            
            // Hack to force invalidation of SKMatrix
            canvas.PostTransform = MatrixUtils.ToMatrix(transform);
            canvas.Transform = Matrix.Identity;

            if (OpacityMaskBrush != null)
                canvas.PopOpacityMask();
            if (Clip != null)
                canvas.PopGeometryClip();
            if (ClipToBounds)
                canvas.PopClip();
            if(Opacity != 1)
                canvas.PopOpacity();
        }
        
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
        
        public Matrix4x4 CombinedTransformMatrix { get; private set; } = Matrix4x4.Identity;
        public Matrix4x4 GlobalTransformMatrix { get; private set; }

        public virtual void Update(ServerCompositionTarget root)
        {
            if(Parent == null && Root == null)
                return;
            
            var wasVisible = IsVisibleInFrame;
            
            // Calculate new parent-relative transform
            if (_combinedTransformDirty)
            {
                CombinedTransformMatrix = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint,
                    // HACK: Ignore RenderTransform set by the adorner layer
                    AdornedVisual != null ? Matrix4x4.Identity : TransformMatrix,
                    Scale, RotationAngle, Orientation, Offset);
                _combinedTransformDirty = false;
            }

            var parentTransform = (AdornedVisual ?? Parent)?.GlobalTransformMatrix ?? Matrix4x4.Identity;

            var newTransform = CombinedTransformMatrix * parentTransform;
            
            // Check if visual was moved and recalculate face orientation
            var positionChanged = false;
            if (GlobalTransformMatrix != newTransform)
            {
                _isBackface = Vector3.Transform(
                    new Vector3(0, 0, float.PositiveInfinity), GlobalTransformMatrix).Z <= 0;
                positionChanged = true;
            }

            var oldTransformedContentBounds = TransformedOwnContentBounds;
            var oldCombinedTransformedClipBounds = _combinedTransformedClipBounds;

            var dirtyOldBounds = false; 
            if (_parent?.IsDirtyComposition == true)
            {
                IsDirtyComposition = true;
                _isDirtyForUpdate = true;
                dirtyOldBounds = true;
            }

            GlobalTransformMatrix = newTransform;
            
            var ownBounds = OwnContentBounds;
            if (ownBounds != _oldOwnContentBounds || positionChanged)
            {
                _oldOwnContentBounds = ownBounds;
                if (ownBounds.IsEmpty)
                    TransformedOwnContentBounds = default;
                else
                    TransformedOwnContentBounds =
                        ownBounds.TransformToAABB(MatrixUtils.ToMatrix(GlobalTransformMatrix));
            }

            if (_clipSizeDirty || positionChanged)
            {
                _transformedClipBounds = ClipToBounds
                    ? new Rect(new Size(Size.X, Size.Y))
                        .TransformToAABB(MatrixUtils.ToMatrix(GlobalTransformMatrix))
                    : null;
                
                _clipSizeDirty = false;
            }
            
            _combinedTransformedClipBounds = Parent?._combinedTransformedClipBounds ?? new Rect(Root!.Size);
            if (_transformedClipBounds != null)
                _combinedTransformedClipBounds = _combinedTransformedClipBounds.Intersect(_transformedClipBounds.Value);
            
            EffectiveOpacity = Opacity * (Parent?.EffectiveOpacity ?? 1);

            IsVisibleInFrame = Visible && EffectiveOpacity > 0.04 && !_isBackface &&
                               !_combinedTransformedClipBounds.IsEmpty;
            
            if (wasVisible != IsVisibleInFrame)
                _isDirtyForUpdate = true;
            
            // Invalidate previous rect and queue new rect based on visibility
            if (positionChanged)
            {
                if (wasVisible)
                    dirtyOldBounds = true;

                if (IsVisibleInFrame)
                    _isDirtyForUpdate = true;
            }
            
            // Invalidate new bounds
            if (IsVisibleInFrame && _isDirtyForUpdate)
            {
                dirtyOldBounds = true;
                AddDirtyRect(TransformedOwnContentBounds.Intersect(_combinedTransformedClipBounds));
            }

            if (dirtyOldBounds && wasVisible)
                AddDirtyRect(oldTransformedContentBounds.Intersect(oldCombinedTransformedClipBounds));


            _isDirtyForUpdate = false;
            
            // Update readback indices
            var i = Root!.Readback;
            ref var readback = ref GetReadback(i.WriteIndex);
            readback.Revision = root.Revision;
            readback.Matrix = CombinedTransformMatrix;
            readback.TargetId = Root.Id;
            readback.Visible = IsVisibleInFrame;
        }

        void AddDirtyRect(Rect rc)
        {
            if(rc == Rect.Empty)
                return;
            Root?.AddDirtyRect(rc);
        }
        
        /// <summary>
        /// Data that can be read from the UI thread
        /// </summary>
        public struct ReadbackData
        {
            public Matrix4x4 Matrix;
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
                Root.RemoveVisual(this);
        }
        
        partial void OnRootChanged()
        {
            if (Root != null)
                Root.AddVisual(this);
        }
        
        protected override void ValuesInvalidated()
        {
            _isDirtyForUpdate = true;
            Root?.Invalidate();
        }

        public bool IsVisibleInFrame { get; set; }
        public double EffectiveOpacity { get; set; }
        public Rect TransformedOwnContentBounds { get; set; }
        public virtual Rect OwnContentBounds => new Rect(0, 0, Size.X, Size.Y);
    }


}