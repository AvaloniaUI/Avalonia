using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Threading;
using Avalonia.Utilities;


// Special license applies <see href="https://raw.githubusercontent.com/AvaloniaUI/Avalonia/master/src/Avalonia.Base/Rendering/Composition/License.md">License.md</see>

namespace Avalonia.Rendering.Composition
{
    /// <summary>
    /// The Compositor class manages communication between UI-thread and render-thread parts of the composition engine.
    /// It also serves as a factory to create UI-thread parts of various composition objects 
    /// </summary>
    public partial class Compositor
    {
        internal IRenderLoop Loop { get; }
        private ServerCompositor _server;
        private TaskCompletionSource<int>? _nextCommit;
        private Action _commit;
        private BatchStreamObjectPool<object?> _batchObjectPool = new();
        private BatchStreamMemoryPool _batchMemoryPool = new();
        private List<CompositionObject> _objectsForSerialization = new();
        private Queue<Action> _invokeBeforeCommitWrite = new(), _invokeBeforeCommitRead = new();
        internal ServerCompositor Server => _server;
        private Task? _pendingBatch;
        private readonly object _pendingBatchLock = new();
        private List<Action> _pendingServerCompositorJobs = new();

        internal IEasing DefaultEasing { get; }

        internal event Action? AfterCommit;
        

        /// <summary>
        /// Creates a new compositor on a specified render loop that would use a particular GPU
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="gpu"></param>
        public Compositor(IRenderLoop loop, IPlatformGraphics? gpu)
        {
            Loop = loop;
            _server = new ServerCompositor(loop, gpu, _batchObjectPool, _batchMemoryPool);
            _commit = () => Commit();

            DefaultEasing = new CubicBezierEasing(new Point(0.25f, 0.1f), new Point(0.25f, 1f));
        }

        /// <summary>
        /// Requests pending changes in the composition objects to be serialized and sent to the render thread
        /// </summary>
        /// <returns>A task that completes when sent changes are applied and rendered on the render thread</returns>
        public Task RequestCommitAsync()
        {
            Dispatcher.UIThread.VerifyAccess();
            if (_nextCommit == null)
            {
                _nextCommit = new TaskCompletionSource<int>();
                var pending = _pendingBatch;
                if (pending != null)
                {
                    pending.ContinueWith(_ =>
                    {
                        Dispatcher.UIThread.Post(_commit, DispatcherPriority.Composition);
                    });
                }
                else
                    Dispatcher.UIThread.Post(_commit, DispatcherPriority.Composition);
            }

            return _nextCommit.Task;
        }

        internal Task Commit()
        {
            try
            {
                return CommitCore();
            }
            finally
            {
                if (_invokeBeforeCommitWrite.Count > 0)
                    RequestCommitAsync();
                AfterCommit?.Invoke();
            }
        }
        
        Task CommitCore()
        {
            Dispatcher.UIThread.VerifyAccess();
            using var noPump = NonPumpingLockHelper.Use();
            
            _nextCommit ??= new TaskCompletionSource<int>();

            (_invokeBeforeCommitRead, _invokeBeforeCommitWrite) = (_invokeBeforeCommitWrite, _invokeBeforeCommitRead);
            while (_invokeBeforeCommitRead.Count > 0)
                _invokeBeforeCommitRead.Dequeue()();
            
            var batch = new Batch(_nextCommit);
            
            using (var writer = new BatchStreamWriter(batch.Changes, _batchMemoryPool, _batchObjectPool))
            {
                foreach (var obj in _objectsForSerialization)
                {
                    writer.WriteObject(obj.Server);
                    obj.SerializeChanges(writer);
#if DEBUG_COMPOSITOR_SERIALIZATION
                    writer.Write(BatchStreamDebugMarkers.ObjectEndMagic);
                    writer.WriteObject(BatchStreamDebugMarkers.ObjectEndMarker);
#endif
                }
                _objectsForSerialization.Clear();
                if (_pendingServerCompositorJobs.Count > 0)
                {
                    writer.WriteObject(ServerCompositor.RenderThreadJobsStartMarker);
                    foreach (var job in _pendingServerCompositorJobs)
                        writer.WriteObject(job);
                    writer.WriteObject(ServerCompositor.RenderThreadJobsEndMarker);
                }
                _pendingServerCompositorJobs.Clear();
            }
            
            batch.CommittedAt = Server.Clock.Elapsed;
            _server.EnqueueBatch(batch);
            
            lock (_pendingBatchLock)
            {
                _pendingBatch = _nextCommit.Task;
                _pendingBatch.ContinueWith(t =>
                {
                    lock (_pendingBatchLock)
                    {
                        if (_pendingBatch == t)
                            _pendingBatch = null;
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
                _nextCommit = null;
                
                return _pendingBatch;
            }
        }

        internal void RegisterForSerialization(CompositionObject compositionObject)
        {
            Dispatcher.UIThread.VerifyAccess();
            _objectsForSerialization.Add(compositionObject);
            RequestCommitAsync();
        }

        /// <summary>
        /// Enqueues a callback to be called before the next scheduled commit.
        /// If there is no scheduled commit it automatically schedules one
        /// This is useful for updating your composition tree objects after binding
        /// and layout passes have completed
        /// </summary>
        public void RequestCompositionUpdate(Action action)
        {
            Dispatcher.UIThread.VerifyAccess();
            _invokeBeforeCommitWrite.Enqueue(action);
            RequestCommitAsync();
        }

        internal void PostServerJob(Action job)
        {
            Dispatcher.UIThread.VerifyAccess();
            _pendingServerCompositorJobs.Add(job);
            RequestCommitAsync();
        }

        internal Task InvokeServerJobAsync(Action job) =>
            InvokeServerJobAsync<object?>(() =>
            {
                job();
                return null;
            });

        internal Task<T> InvokeServerJobAsync<T>(Func<T> job)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            PostServerJob(() =>
            {
                try
                {
                    tcs.SetResult(job());
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Attempts to query for a feature from the platform render interface
        /// </summary>
        public ValueTask<object?> TryGetRenderInterfaceFeature(Type featureType) =>
            new(InvokeServerJobAsync(() =>
            {
                using (Server.RenderInterface.EnsureCurrent())
                {
                    return Server.RenderInterface.Value.TryGetFeature(featureType);
                }
            }));

        public ValueTask<ICompositionGpuInterop?> TryGetCompositionGpuInterop() =>
            new(InvokeServerJobAsync<ICompositionGpuInterop?>(() =>
            {
                using (Server.RenderInterface.EnsureCurrent())
                {
                    var feature = Server.RenderInterface.Value
                        .TryGetFeature<IExternalObjectsRenderInterfaceContextFeature>();
                    if (feature == null)
                        return null;
                    return new CompositionInterop(this, feature);
                }
            }));
    }
}
