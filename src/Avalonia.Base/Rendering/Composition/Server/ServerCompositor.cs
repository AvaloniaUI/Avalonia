using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    internal class ServerCompositor : IRenderLoopTask
    {
        private readonly IRenderLoop _renderLoop;
        private readonly Queue<Batch> _batches = new Queue<Batch>(); 
        public long LastBatchId { get; private set; }
        public Stopwatch Clock { get; } = Stopwatch.StartNew();
        public TimeSpan ServerNow { get; private set; }
        private List<ServerCompositionTarget> _activeTargets = new();
        private HashSet<IAnimationInstance> _activeAnimations = new();
        private List<IAnimationInstance> _animationsToUpdate = new();
        private BatchStreamObjectPool<object?> _batchObjectPool;
        private BatchStreamMemoryPool _batchMemoryPool;
        private object _lock = new object();
        public IPlatformGpuContext? GpuContext { get; }

        public ServerCompositor(IRenderLoop renderLoop, IPlatformGpu? platformGpu,
            BatchStreamObjectPool<object?> batchObjectPool, BatchStreamMemoryPool batchMemoryPool)
        {
            GpuContext = platformGpu?.PrimaryContext;
            _renderLoop = renderLoop;
            _batchObjectPool = batchObjectPool;
            _batchMemoryPool = batchMemoryPool;
            _renderLoop.Add(this);
        }

        public void EnqueueBatch(Batch batch)
        {
            lock (_batches) 
                _batches.Enqueue(batch);
        }

        internal void UpdateServerTime() => ServerNow = Clock.Elapsed;

        List<Batch> _reusableToCompleteList = new();
        void ApplyPendingBatches()
        {
            while (true)
            {
                Batch batch;
                lock (_batches)
                {
                    if(_batches.Count == 0)
                        break;
                    batch = _batches.Dequeue();
                }

                using (var stream = new BatchStreamReader(batch.Changes, _batchMemoryPool, _batchObjectPool))
                {
                    while (!stream.IsObjectEof)
                    {
                        var target = (ServerObject)stream.ReadObject()!;
                        target.DeserializeChanges(stream, batch);
#if DEBUG_COMPOSITOR_SERIALIZATION
                        if (stream.ReadObject() != BatchStreamDebugMarkers.ObjectEndMarker)
                            throw new InvalidOperationException(
                                $"Object {target.GetType()} failed to deserialize properly on object stream");
                        if(stream.Read<Guid>() != BatchStreamDebugMarkers.ObjectEndMagic)
                            throw new InvalidOperationException(
                                $"Object {target.GetType()} failed to deserialize properly on data stream");
#endif
                    }
                }

                _reusableToCompleteList.Add(batch);
                LastBatchId = batch.SequenceId;
            }
        }

        void CompletePendingBatches()
        {
            foreach(var batch in _reusableToCompleteList)
                batch.Complete();
            _reusableToCompleteList.Clear();
        }

        bool IRenderLoopTask.NeedsUpdate => false;

        void IRenderLoopTask.Update(TimeSpan time)
        {
        }

        public void Render()
        {
            lock (_lock)
            {
                RenderCore();
            }
        }
        
        private void RenderCore()
        {
            ApplyPendingBatches();
            
            foreach(var animation in _activeAnimations)
                _animationsToUpdate.Add(animation);
            
            foreach(var animation in _animationsToUpdate)
                animation.Invalidate();
            
            _animationsToUpdate.Clear();
            
            foreach (var t in _activeTargets)
                t.Render();
            
            CompletePendingBatches();
        }

        public void AddCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Add(target);
        }

        public void RemoveCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Remove(target);
        }
        
        public void AddToClock(IAnimationInstance animationInstance) =>
            _activeAnimations.Add(animationInstance);

        public void RemoveFromClock(IAnimationInstance animationInstance) =>
            _activeAnimations.Remove(animationInstance);
    }
}