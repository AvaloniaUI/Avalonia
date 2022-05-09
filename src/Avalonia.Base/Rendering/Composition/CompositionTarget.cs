using System.Collections.Generic;
using System.Numerics;
using Avalonia.Collections.Pooled;

namespace Avalonia.Rendering.Composition
{
    public partial class CompositionTarget
    {
        partial void OnRootChanged()
        {
            if (Root != null)
                Root.Root = this;
        }

        partial void OnRootChanging()
        {
            if (Root != null)
                Root.Root = null;
        }
        
        public PooledList<CompositionVisual>? TryHitTest(Vector2 point)
        {
            Server.Readback.NextRead();
            if (Root == null)
                return null;
            var res = new PooledList<CompositionVisual>();
            HitTestCore(Root, point, res);
            return res;
        }

        public Vector2? TryTransformToVisual(CompositionVisual visual, Vector2 point)
        {
            if (visual.Root != this)
                return null;
            var v = visual;
            var m = Matrix3x2.Identity;
            while (v != null)
            {
                if (!TryGetInvertedTransform(v, out var cm))
                    return null;
                m = m * cm;
                v = v.Parent;
            }

            return Vector2.Transform(point, m);
        }

        bool TryGetInvertedTransform(CompositionVisual visual, out Matrix3x2 matrix)
        {
            var m = visual.TryGetServerTransform();
            if (m == null)
            {
                matrix = default;
                return false;
            }

            // TODO: Use Matrix3x3
            var m32 = new Matrix3x2(m.Value.M11, m.Value.M12, m.Value.M21, m.Value.M22, m.Value.M41, m.Value.M42);
            
            return Matrix3x2.Invert(m32, out matrix);
        }

        bool TryTransformTo(CompositionVisual visual, ref Vector2 v)
        {
            if (TryGetInvertedTransform(visual, out var m))
            {
                v = Vector2.Transform(v, m);
                return true;
            }

            return false;
        }
        
        bool HitTestCore(CompositionVisual visual, Vector2 point, PooledList<CompositionVisual> result)
        {
            //TODO: Check readback too
            if (visual.Visible == false)
                return false;
            if (!TryTransformTo(visual, ref point))
                return false;
            if (point.X >= 0 && point.Y >= 0 && point.X <= visual.Size.X && point.Y <= visual.Size.Y)
            {
                bool success = false;
                // Hit-test the current node
                if (visual.HitTest(point))
                {
                    result.Add(visual);
                    success = true;
                }
                
                // Inspect children too
                if(visual is CompositionContainerVisual cv)
                    for (var c = cv.Children.Count - 1; c >= 0; c--)
                    {
                        var ch = cv.Children[c];
                        var hit = HitTestCore(ch, point, result);
                        if (hit)
                            return true;
                    }

                return success;
            }

            return false;
        }
    }
}