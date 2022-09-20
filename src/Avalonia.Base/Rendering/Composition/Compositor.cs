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
        private Queue<Action<Task>> _invokeBeforeCommit = new();
        internal ServerCompositor Server => _server;
        private Task? _pendingBatch;
        private readonly object _pendingBatchLock = new();

        internal IEasing DefaultEasing { get; }
        

        /// <summary>
        /// Creates a new compositor on a specified render loop that would use a particular GPU
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="gpu"></param>
        public Compositor(IRenderLoop loop, IPlatformGpu? gpu)
        {
            Loop = loop;
            _server = new ServerCompositor(loop, gpu, _batchObjectPool, _batchMemoryPool);
            _commit = () =>
            {
                Console.WriteLine("Dispatcher:Commit");
                Commit();
            };

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
            Dispatcher.UIThread.VerifyAccess();
            using var noPump = NonPumpingLockHelper.Use();
            
            _nextCommit ??= new TaskCompletionSource<int>();

            while (_invokeBeforeCommit.Count > 0)
                _invokeBeforeCommit.Dequeue()(_nextCommit.Task);
            
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
            }
            
            batch.CommitedAt = Server.Clock.Elapsed;
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

        internal void InvokeBeforeNextCommit(Action<Task> action)
        {
            Dispatcher.UIThread.VerifyAccess();
            _invokeBeforeCommit.Enqueue(action);
            RequestCommitAsync();
        }
    }
}
