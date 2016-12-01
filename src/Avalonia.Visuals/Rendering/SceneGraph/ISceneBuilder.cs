using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public interface ISceneBuilder
    {
        bool Update(Scene scene, IVisual visual, LayerDirtyRects dirty);
        void UpdateAll(Scene scene, LayerDirtyRects dirty);
    }
}