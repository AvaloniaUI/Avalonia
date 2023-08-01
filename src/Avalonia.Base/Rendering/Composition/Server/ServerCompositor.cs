using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Server
{
    /// <summary>
    /// Server-side counterpart of the <see cref="Compositor"/>.
    /// 1) manages deserialization of changes received from the UI thread
    /// 2) triggers animation ticks
    /// 3) asks composition targets to render themselves
    /// </summary>
    internal partial class ServerCompositor : IRenderLoopTask
    {
        private readonly IRenderLoop _renderLoop;

        private readonly Queue<Batch> _batches = new Queue<Batch>();
        private readonly Queue<Action> _receivedJobQueue = new();
        public long LastBatchId { get; private set; }
        public Stopwatch Clock { get; } = Stopwatch.StartNew();
        public TimeSpan ServerNow { get; private set; }
        private List<ServerCompositionTarget> _activeTargets = new();
        private HashSet<IServerClockItem> _clockItems = new();
        private List<IServerClockItem> _clockItemsToUpdate = new();
        internal BatchStreamObjectPool<object?> BatchObjectPool;
        internal BatchStreamMemoryPool BatchMemoryPool;
        private object _lock = new object();
        private Thread? _safeThread;
        public PlatformRenderInterfaceContextManager RenderInterface { get; }
        internal static readonly object RenderThreadDisposeStartMarker = new();
        internal static readonly object RenderThreadJobsStartMarker = new();
        internal static readonly object RenderThreadJobsEndMarker = new();

        public ServerCompositor(IRenderLoop renderLoop, IPlatformGraphics? platformGraphics,
            BatchStreamObjectPool<object?> batchObjectPool, BatchStreamMemoryPool batchMemoryPool)
        {
            _renderLoop = renderLoop;
            RenderInterface = new PlatformRenderInterfaceContextManager(platformGraphics);
            BatchObjectPool = batchObjectPool;
            BatchMemoryPool = batchMemoryPool;
            _renderLoop.Add(this);
        }

        public void EnqueueBatch(Batch batch)
        {
            lock (_batches) 
                _batches.Enqueue(batch);
        }

        internal void UpdateServerTime() => ServerNow = Clock.Elapsed;

        List<Batch> _reusableToNotifyProcessedList = new();
        List<Batch> _reusableToNotifyRenderedList = new();
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

                using (var stream = new BatchStreamReader(batch.Changes, BatchMemoryPool, BatchObjectPool))
                {
                    while (!stream.IsObjectEof)
                    {
                        var readObject = stream.ReadObject();
                        if (readObject == RenderThreadJobsStartMarker)
                        {
                            ReadServerJobs(stream);
                            continue;
                        }

                        if (readObject == RenderThreadDisposeStartMarker)
                        {
                            ReadDisposeJobs(stream);
                            continue;
                        }
                        
                        var target = (SimpleServerObject)readObject!;
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

                _reusableToNotifyProcessedList.Add(batch);
                LastBatchId = batch.SequenceId;
            }
        }

        void ReadServerJobs(BatchStreamReader reader)
        {
            object? readObject;
            while ((readObject = reader.ReadObject()) != RenderThreadJobsEndMarker)
                _receivedJobQueue.Enqueue((Action)readObject!);
        }

        void ReadDisposeJobs(BatchStreamReader reader)
        {
            var count = reader.Read<int>();
            while (count > 0)
            {
                (reader.ReadObject() as IDisposable)?.Dispose();
                count--;
            }
        }

        void ExecuteServerJobs()
        {
            while(_receivedJobQueue.Count > 0)
                try
                {
                    _receivedJobQueue.Dequeue()();
                }
                catch
                {
                    // Ignore
                }
        }

        void NotifyBatchesProcessed()
        {
            foreach (var batch in _reusableToNotifyProcessedList) 
                batch.NotifyProcessed();

            foreach (var batch in _reusableToNotifyProcessedList)
                _reusableToNotifyRenderedList.Add(batch);

            _reusableToNotifyProcessedList.Clear();
        }
        
        void NotifyBatchesRendered()
        {
            foreach (var batch in _reusableToNotifyRenderedList) 
                batch.NotifyRendered();

            _reusableToNotifyRenderedList.Clear();
        }

        public void Render()
        {
            lock (_lock)
            {
                try
                {
                    _safeThread = Thread.CurrentThread;
                    RenderCore();
                }
                finally
                {
                    NotifyBatchesRendered();
                    _safeThread = null;
                }
                
            }
        }
        
        private void RenderCore()
        {
            UpdateServerTime();
            ApplyPendingBatches();
            NotifyBatchesProcessed();
            
            foreach(var animation in _clockItems)
                _clockItemsToUpdate.Add(animation);

            foreach (var animation in _clockItemsToUpdate)
                animation.OnTick();
            
            _clockItemsToUpdate.Clear();

            ApplyEnqueuedRenderResourceChanges();
            
            try
            {
                RenderInterface.EnsureValidBackendContext();
                ExecuteServerJobs();
                foreach (var t in _activeTargets)
                    t.Render();
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception when rendering: {Error}", e);
            }
        }

        public void AddCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Add(target);
        }

        public void RemoveCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Remove(target);
        }
        
        public void AddToClock(IServerClockItem item) =>
            _clockItems.Add(item);

        public void RemoveFromClock(IServerClockItem item) =>
            _clockItems.Remove(item);

        public IRenderTarget CreateRenderTarget(IEnumerable<object> surfaces)
        {
            using (RenderInterface.EnsureCurrent())
                return RenderInterface.CreateRenderTarget(surfaces);
        }

        public bool CheckAccess() => _safeThread == Thread.CurrentThread;
        public void VerifyAccess()
        {
            if (!CheckAccess())
                throw new InvalidOperationException("This object can be only accessed under compositor lock");
        }
    }
}
