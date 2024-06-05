using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Server;

internal class ServerCompositorAnimations
{
    private readonly HashSet<IServerClockItem> _clockItems = new();
    private readonly List<IServerClockItem> _clockItemsToUpdate = new();
    private readonly HashSet<ServerObjectAnimations> _dirtyAnimatedObjects = new();
    private readonly Queue<ServerObjectAnimations> _dirtyAnimatedObjectQueue = new();

    public void AddToClock(IServerClockItem item) =>
        _clockItems.Add(item);

    public void RemoveFromClock(IServerClockItem item) =>
        _clockItems.Remove(item);

    public void Process()
    {
        foreach (var animation in _clockItems)
            _clockItemsToUpdate.Add(animation);

        foreach (var animation in _clockItemsToUpdate)
            animation.OnTick();

        _clockItemsToUpdate.Clear();

        while (_dirtyAnimatedObjectQueue.Count > 0)
            _dirtyAnimatedObjectQueue.Dequeue().EvaluateAnimations();
        _dirtyAnimatedObjects.Clear();
    }

    public void AddDirtyAnimatedObject(ServerObjectAnimations obj)
    {
        if (_dirtyAnimatedObjects.Add(obj))
            _dirtyAnimatedObjectQueue.Enqueue(obj);
    }
}