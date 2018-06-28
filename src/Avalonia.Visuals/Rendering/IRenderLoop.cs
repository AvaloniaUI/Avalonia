namespace Avalonia.Rendering
{
    public interface IRenderLoop
    {
        void Add(IRenderLoopTask i);
        void Remove(IRenderLoopTask i);
    }
}