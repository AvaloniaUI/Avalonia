using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositor
{
    private readonly Queue<IServerRenderResource> _renderResourcesInvalidationQueue = new();
    private readonly HashSet<IServerRenderResource> _renderResourcesInvalidationSet = new();
    
    public void ApplyEnqueuedRenderResourceChanges()
    {
        while (_renderResourcesInvalidationQueue.TryDequeue(out var obj)) 
            obj.QueuedInvalidate();
        _renderResourcesInvalidationSet.Clear();
    }

    public void EnqueueRenderResourceForInvalidation(IServerRenderResource resource)
    {
        if (_renderResourcesInvalidationSet.Add(resource))
            _renderResourcesInvalidationQueue.Enqueue(resource);
    }
}