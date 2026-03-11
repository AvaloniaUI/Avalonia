using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia.Logging;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;

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

        private readonly Queue<CompositionBatch> _batches = new Queue<CompositionBatch>();
        private readonly Queue<Action> _receivedJobQueue = new();
        private readonly Queue<Action> _receivedPostTargetJobQueue = new();
        public long LastBatchId { get; private set; }
        public Stopwatch Clock { get; } = Stopwatch.StartNew();
        public TimeSpan ServerNow { get; private set; }
        private readonly List<ServerCompositionTarget> _activeTargets = new();
        internal BatchStreamObjectPool<object?> BatchObjectPool;
        internal BatchStreamMemoryPool BatchMemoryPool;
        public CompositorPools Pools { get; } = new();
        private readonly object _lock = new object();
        private Thread? _safeThread;
        private bool _uiThreadIsInsideRender;
        public PlatformRenderInterfaceContextManager RenderInterface { get; }
        internal static readonly object RenderThreadDisposeStartMarker = new();
        internal static readonly object RenderThreadJobsStartMarker = new();
        internal static readonly object RenderThreadJobsEndMarker = new();
        internal static readonly object RenderThreadPostTargetJobsStartMarker = new();
        internal static readonly object RenderThreadPostTargetJobsEndMarker = new();
        public CompositionOptions Options { get; }
        public ServerCompositorAnimations Animations { get; }
        public ReadbackIndices Readback { get; } = new();
        
        private int _ticksSinceLastCommit;
        private const int CommitGraceTicks = 10;

        public ServerCompositor(IRenderLoop renderLoop, IPlatformGraphics? platformGraphics,
            CompositionOptions options,
            BatchStreamObjectPool<object?> batchObjectPool, BatchStreamMemoryPool batchMemoryPool)
        {
            Options = options;
            Animations = new();
            _renderLoop = renderLoop;
            RenderInterface = new PlatformRenderInterfaceContextManager(platformGraphics);
            RenderInterface.ContextDisposed += RT_OnContextDisposed;
            RenderInterface.ContextCreated += RT_OnContextCreated;
            BatchObjectPool = batchObjectPool;
            BatchMemoryPool = batchMemoryPool;
            _renderLoop.Add(this);
        }

        public void EnqueueBatch(CompositionBatch batch)
        {
            lock (_batches) 
                _batches.Enqueue(batch);
            _renderLoop.Wakeup();
        }

        internal void UpdateServerTime() => ServerNow = Clock.Elapsed;

        readonly List<CompositionBatch> _reusableToNotifyProcessedList = new();
        readonly List<CompositionBatch> _reusableToNotifyRenderedList = new();
        void ApplyPendingBatches()
        {
            bool hadBatches = false;
            while (true)
            {
                CompositionBatch batch;
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
                            ReadServerJobs(stream, _receivedJobQueue, RenderThreadJobsEndMarker);
                            continue;
                        }
                        if (readObject == RenderThreadPostTargetJobsStartMarker)
                        {
                            ReadServerJobs(stream, _receivedPostTargetJobQueue, RenderThreadPostTargetJobsEndMarker);
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
                hadBatches = true;
            }
            
            if (hadBatches)
                _ticksSinceLastCommit = 0;
            else
                _ticksSinceLastCommit++;
        }

        void ReadServerJobs(BatchStreamReader reader, Queue<Action> queue, object endMarker)
        {
            object? readObject;
            while ((readObject = reader.ReadObject()) != endMarker)
                queue.Enqueue((Action)readObject!);
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

        void ExecuteServerJobs(Queue<Action> queue)
        {
            while(queue.Count > 0)
                try
                {
                    queue.Dequeue()();
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

        bool IRenderLoopTask.Render() => ExecuteRender(true);
        public void Render(bool catchExceptions) => ExecuteRender(catchExceptions);
        
        private bool ExecuteRender(bool catchExceptions)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                if (_uiThreadIsInsideRender)
                    throw new InvalidOperationException("Reentrancy is not supported");
                _uiThreadIsInsideRender = true;
                try
                {
                    using (Dispatcher.UIThread.DisableProcessing()) 
                        return RenderReentrancySafe(catchExceptions);
                }
                finally
                {
                    _uiThreadIsInsideRender = false;
                }
            }
            else
                return RenderReentrancySafe(catchExceptions);
        }
        
        private bool RenderReentrancySafe(bool catchExceptions)
        {
            lock (_lock)
            {
                try
                {
                    try
                    {
                        _safeThread = Thread.CurrentThread;
                        return RenderCore(catchExceptions);
                    }
                    finally
                    {
                        NotifyBatchesRendered();
                    }
                }
                finally
                {
                    _safeThread = null;
                }
            }
        }

        private TimeSpan ExecuteGlobalPasses()
        {
            var compositorGlobalPassesStarted = Stopwatch.GetTimestamp();
            ApplyPendingBatches();
            NotifyBatchesProcessed();

            Animations.Process();

            ApplyEnqueuedRenderResourceChangesPass();
            
            VisualOwnPropertiesUpdatePass();
            
            // Adorners need to be updated after own properties recompute pass,
            // because they may depend on ancestor's transform chain to be consistent
            AdornerUpdatePass();

            return Stopwatch.GetElapsedTime(compositorGlobalPassesStarted);
        }
        
        private bool RenderCore(bool catchExceptions)
        {
            UpdateServerTime();
            
            var compositorGlobalPassesElapsed = ExecuteGlobalPasses();
            
            try
            {
                if (!RenderInterface.IsReady)
                    return true;
                RenderInterface.EnsureValidBackendContext();
                ExecuteServerJobs(_receivedJobQueue);

                foreach (var t in _activeTargets)
                {
                    t.Update(compositorGlobalPassesElapsed);
                    t.Render();
                }

                VisualReadbackUpdatePass();
                
                ExecuteServerJobs(_receivedPostTargetJobQueue);
            }
            catch (Exception e) when(RT_OnContextLostExceptionFilterObserver(e) && catchExceptions)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this, "Exception when rendering: {Error}", e);
            }
            
            // Request a tick if we have active animations or if there are recent batches
            if (Animations.NeedNextTick || _ticksSinceLastCommit < CommitGraceTicks)
                return true;
            
            // Request a tick if we had unready targets in the last tick, to check if they are ready next time
            foreach (var target in _activeTargets)
                if (target.IsWaitingForReadyRenderTarget)
                    return true;
            
            // Otherwise there is no need to waste CPU cycles, tell the timer to pause
            return false;
        }

        public void AddCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Add(target);
        }

        public void RemoveCompositionTarget(ServerCompositionTarget target)
        {
            _activeTargets.Remove(target);
        }
        
        public IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
        {
            using (RenderInterface.EnsureCurrent())
                return RenderInterface.CreateRenderTarget(surfaces);
        }

        public bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces)
        {
            return RenderInterface.IsReadyToCreateRenderTarget(surfaces);
        }

        public bool CheckAccess() => _safeThread == Thread.CurrentThread;
        public void VerifyAccess()
        {
            if (!CheckAccess())
                throw new InvalidOperationException("This object can be only accessed under compositor lock");
        }
    }
}
