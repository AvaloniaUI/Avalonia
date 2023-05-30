using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Metadata;
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
        internal bool UseUiThreadForSynchronousCommits { get; }
        private ServerCompositor _server;
        private Batch? _nextCommit;
        private BatchStreamObjectPool<object?> _batchObjectPool;
        private BatchStreamMemoryPool _batchMemoryPool;
        private Queue<ICompositorSerializable> _objectSerializationQueue = new();
        private HashSet<ICompositorSerializable> _objectSerializationHashSet = new();
        private Queue<Action> _invokeBeforeCommitWrite = new(), _invokeBeforeCommitRead = new();
        private HashSet<IDisposable> _disposeOnNextBatch = new();
        internal ServerCompositor Server => _server;
        private Batch? _pendingBatch;
        private readonly object _pendingBatchLock = new();
        private List<Action> _pendingServerCompositorJobs = new();
        private DiagnosticTextRenderer? _diagnosticTextRenderer;
        private Action _triggerCommitRequested;

        internal IEasing DefaultEasing { get; }

        private DiagnosticTextRenderer? DiagnosticTextRenderer
        {
            get
            {
                if (_diagnosticTextRenderer == null)
                {
                    // We are running in some unit test context
                    if (AvaloniaLocator.Current.GetService<IFontManagerImpl>() == null)
                        return null;
                    _diagnosticTextRenderer = new(Typeface.Default.GlyphTypeface, 12.0);
                }

                return _diagnosticTextRenderer;
            }
        }

        internal event Action? AfterCommit;

        
        /// <summary>
        /// Creates a new compositor on a specified render loop that would use a particular GPU
        /// </summary>
        [PrivateApi]
        public Compositor(IPlatformGraphics? gpu, bool useUiThreadForSynchronousCommits = false)
            : this(RenderLoop.LocatorAutoInstance, gpu, useUiThreadForSynchronousCommits)
        {
        }

        internal Compositor(IRenderLoop loop, IPlatformGraphics? gpu, bool useUiThreadForSynchronousCommits = false)
            : this(loop, gpu, useUiThreadForSynchronousCommits, MediaContext.Instance, false)
        {
        }

        internal Compositor(IRenderLoop loop, IPlatformGraphics? gpu,
            bool useUiThreadForSynchronousCommits,
            ICompositorScheduler scheduler, bool reclaimBuffersImmediately)
        {
            Loop = loop;
            UseUiThreadForSynchronousCommits = useUiThreadForSynchronousCommits;
            _batchMemoryPool = new(reclaimBuffersImmediately);
            _batchObjectPool = new(reclaimBuffersImmediately);
            _server = new ServerCompositor(loop, gpu, _batchObjectPool, _batchMemoryPool);
            _triggerCommitRequested = () => scheduler.CommitRequested(this);

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
                _nextCommit = new ();
                var pending = _pendingBatch;
                if (pending != null)
                    pending.Processed.ContinueWith(
                        _ => Dispatcher.UIThread.Post(_triggerCommitRequested, DispatcherPriority.Send));
                else
                    _triggerCommitRequested();
            }

            return _nextCommit.Processed;
        }

        internal Batch Commit()
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
        
        Batch CommitCore()
        {
            Dispatcher.UIThread.VerifyAccess();
            using var noPump = NonPumpingLockHelper.Use();
            
            _nextCommit ??= new();

            (_invokeBeforeCommitRead, _invokeBeforeCommitWrite) = (_invokeBeforeCommitWrite, _invokeBeforeCommitRead);
            while (_invokeBeforeCommitRead.Count > 0)
                _invokeBeforeCommitRead.Dequeue()();
            
            using (var writer = new BatchStreamWriter(_nextCommit.Changes, _batchMemoryPool, _batchObjectPool))
            {
                while(_objectSerializationQueue.TryDequeue(out var obj))
                {
                    var serverObject = obj.TryGetServer(this);
                    if (serverObject != null)
                    {
                        writer.WriteObject(serverObject);
                        obj.SerializeChanges(this, writer);
#if DEBUG_COMPOSITOR_SERIALIZATION
                        writer.Write(BatchStreamDebugMarkers.ObjectEndMagic);
                        writer.WriteObject(BatchStreamDebugMarkers.ObjectEndMarker);
#endif
                    }
                }
                _objectSerializationHashSet.Clear();

                if (_disposeOnNextBatch.Count != 0)
                {
                    writer.WriteObject(ServerCompositor.RenderThreadDisposeStartMarker);
                    writer.Write(_disposeOnNextBatch.Count);
                    foreach (var d in _disposeOnNextBatch)
                        writer.WriteObject(d);
                    _disposeOnNextBatch.Clear();
                }

                if (_pendingServerCompositorJobs.Count > 0)
                {
                    writer.WriteObject(ServerCompositor.RenderThreadJobsStartMarker);
                    foreach (var job in _pendingServerCompositorJobs)
                        writer.WriteObject(job);
                    writer.WriteObject(ServerCompositor.RenderThreadJobsEndMarker);
                }
                _pendingServerCompositorJobs.Clear();
            }
            
            _nextCommit.CommittedAt = Server.Clock.Elapsed;
            _server.EnqueueBatch(_nextCommit);
            
            lock (_pendingBatchLock)
            {
                _pendingBatch = _nextCommit;
                _pendingBatch.Processed.ContinueWith(t =>
                {
                    lock (_pendingBatchLock)
                    {
                        if (_pendingBatch.Processed == t)
                            _pendingBatch = null;
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
                _nextCommit = null;
                
                return _pendingBatch;
            }
        }

        internal void RegisterForSerialization(ICompositorSerializable compositionObject)
        {
            Dispatcher.UIThread.VerifyAccess();
            if(_objectSerializationHashSet.Add(compositionObject))
                _objectSerializationQueue.Enqueue(compositionObject);
            RequestCommitAsync();
        }

        internal void DisposeOnNextBatch(SimpleServerObject obj)
        {
            if (obj is IDisposable disposable && _disposeOnNextBatch.Add(disposable))
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

        internal bool UnitTestIsRegisteredForSerialization(ICompositorSerializable serializable) =>
            _objectSerializationHashSet.Contains(serializable);
    }
    
    internal interface ICompositorScheduler
    {
        void CommitRequested(Compositor compositor);
    }
}
