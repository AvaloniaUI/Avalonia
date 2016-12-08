using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    public interface ISceneBuilder
    {
        bool Update(Scene scene, IVisual visual);
        void UpdateAll(Scene scene);
    }
}