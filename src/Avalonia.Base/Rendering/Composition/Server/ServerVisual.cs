using System.Numerics;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    unsafe partial class ServerCompositionVisual : ServerObject
    {
        private bool _isDirty;
        protected virtual void RenderCore(CompositorDrawingContextProxy canvas, Matrix4x4 transform)
        {
            
        }
        
        public void Render(CompositorDrawingContextProxy canvas, Matrix4x4 transform)
        {
            if(Visible == false)
                return;
            if(Opacity == 0)
                return;
            canvas.PreTransform = MatrixUtils.ToMatrix(transform);
            canvas.Transform = Matrix.Identity;
            if (Opacity != 1)
                canvas.PushOpacity(Opacity);
            if(ClipToBounds)
                canvas.PushClip(new Rect(new Size(Size.X, Size.Y)));
            if (Clip != null) 
                canvas.PushGeometryClip(Clip);
            
            //TODO: Check clip
            RenderCore(canvas, transform);
            
            canvas.PreTransform = MatrixUtils.ToMatrix(transform);
            canvas.Transform = Matrix.Identity;
            
            if (Clip != null)
                canvas.PopGeometryClip();
            if (ClipToBounds)
                canvas.PopClip();
            if(Opacity != 1)
                canvas.PopOpacity();
        }
        
        private ReadbackData _readback0, _readback1, _readback2;


        public ref ReadbackData GetReadback(int idx)
        {
            if (idx == 0)
                return ref _readback0;
            if (idx == 1)
                return ref _readback1;
            return ref _readback2;
        }
        
        public Matrix4x4 CombinedTransformMatrix { get; private set; }
        public Matrix4x4 GlobalTransformMatrix { get; private set; }

        public virtual void Update(ServerCompositionTarget root, Matrix4x4 transform)
        {
            var res = MatrixUtils.ComputeTransform(Size, AnchorPoint, CenterPoint, TransformMatrix,
                Scale, RotationAngle, Orientation, Offset);
            var i = Root!.Readback;
            ref var readback = ref GetReadback(i.WriteIndex);
            readback.Revision = i.WriteRevision;
            readback.Matrix = res;
            readback.TargetId = Root.Id;
            //TODO: check effective opacity too
            IsVisibleInFrame = Visible && Opacity > 0;
            CombinedTransformMatrix = res;
            GlobalTransformMatrix = res * transform;
            //TODO: Cache
            TransformedBounds = ContentBounds.TransformToAABB(MatrixUtils.ToMatrix(GlobalTransformMatrix));
            
            if (!IsVisibleInFrame)
                _isDirty = false;
            else if (_isDirty)
            {
                Root.AddDirtyRect(TransformedBounds);
                _isDirty = false;
            }
        }
        
        public struct ReadbackData
        {
            public Matrix4x4 Matrix;
            public bool Visible;
            public ulong Revision;
            public long TargetId;
        }

        partial void ApplyChangesExtra(CompositionVisualChanges c)
        {
            if (c.Parent.IsSet)
                Parent = c.Parent.Value;
            if (c.Root.IsSet)
                Root = c.Root.Value;
            _isDirty = true;

            if (IsVisibleInFrame)
                Root?.AddDirtyRect(TransformedBounds);
            else
                Root?.Invalidate();
        }

        public ServerCompositionTarget? Root { get; private set; }

        public ServerCompositionVisual? Parent { get; private set; }
        public bool IsVisibleInFrame { get; set; }
        public Rect TransformedBounds { get; set; }
        public virtual Rect ContentBounds => new Rect(0, 0, Size.X, Size.Y);
    }


}