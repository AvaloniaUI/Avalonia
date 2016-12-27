using System;
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
                Opacity = Opacity
            };
        }

        public IVisual LayerRoot { get; }
        public DirtyRects Dirty { get; }
        public int DistanceFromRoot { get; }
        public double Opacity { get; set; } = 1;
    }
}
