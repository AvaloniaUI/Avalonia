using System;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public class SceneLayer
    {
        public SceneLayer(IVisual layerRoot, int distanceFromRoot)
        {
            LayerRoot = layerRoot;
            Dirty = new DirtyRects();
            DistanceFromRoot = distanceFromRoot;
        }

        public SceneLayer Clone()
        {
            return new SceneLayer(LayerRoot, DistanceFromRoot)
            {
                Opacity = Opacity,
                OpacityMask = OpacityMask,
                OpacityMaskRect = OpacityMaskRect,
                GeometryClip = GeometryClip,
            };
        }

        public IVisual LayerRoot { get; }
        public DirtyRects Dirty { get; }
        public int DistanceFromRoot { get; }
        public double Opacity { get; set; } = 1;
        public IBrush OpacityMask { get; set; }
        public Rect OpacityMaskRect { get; set; }
        public IGeometryImpl GeometryClip { get; set; }
    }
}
