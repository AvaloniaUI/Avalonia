using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Server;

partial class ServerCompositor
{
    private readonly Queue<IServerRenderResource> _renderResourcesInvalidationQueue = new();
    private readonly HashSet<IServerRenderResource> _renderResourcesInvalidationSet = new();
    // TODO: parallel processing maybe
    private readonly Queue<ServerCompositionVisual> _visualOwnPropertiesRecomputePass = new();
    private readonly Queue<ServerCompositionVisual> _visualReadbackUpdatePassQueue = new();
    private readonly Queue<ServerCompositionVisual> _adornerUpdateQueue = new();
    
    private void ApplyEnqueuedRenderResourceChangesPass()
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

    private void VisualOwnPropertiesUpdatePass()
    {
        while (_visualOwnPropertiesRecomputePass.TryDequeue(out var obj)) 
            obj.RecomputeOwnProperties();
    }

    public void EnqueueVisualForOwnPropertiesUpdatePass(ServerCompositionVisual visual) =>
        _visualOwnPropertiesRecomputePass.Enqueue(visual);

    
    private void VisualReadbackUpdatePass()
    {
        if(_visualReadbackUpdatePassQueue.Count == 0)
            return;
        
        // visual.HitTest is waiting for this lock to be released, so we need to be quick
        // this is why we have a queue in the first place
        Readback.BeginWrite();
        try
        {
            var read = Readback.ReadRevision;
            var write = Readback.WriteRevision;
            while (_visualReadbackUpdatePassQueue.TryDequeue(out var obj))
                obj.UpdateReadback(write, read);
        }
        finally
        {
            Readback.EndWrite();
        }
    }
    
    public void EnqueueVisualForReadbackUpdatePass(ServerCompositionVisual visual) =>
        _visualReadbackUpdatePassQueue.Enqueue(visual);
    
    
    public void EnqueueAdornerUpdate(ServerCompositionVisual visual) => _adornerUpdateQueue.Enqueue(visual);

    private void AdornerUpdatePass()
    {
        while (_adornerUpdateQueue.Count > 0)
        {
            var adorner = _adornerUpdateQueue.Dequeue();
            adorner.UpdateAdorner();
        }
    }


}