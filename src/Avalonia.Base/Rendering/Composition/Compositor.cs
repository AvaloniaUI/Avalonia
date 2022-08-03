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
        private bool _implicitBatchCommitQueued;
        private Action _implicitBatchCommit;
        private BatchStreamObjectPool<object?> _batchObjectPool = new();
        private BatchStreamMemoryPool _batchMemoryPool = new();
        private List<CompositionObject> _objectsForSerialization = new();
        internal ServerCompositor Server => _server;
        internal IEasing DefaultEasing { get; }
        private List<Action>? _invokeOnNextCommit;
        private readonly Stack<List<Action>> _invokeListPool = new();

        /// <summary>
        /// Creates a new compositor on a specified render loop that would use a particular GPU
        /// </summary>
        /// <param name="loop"></param>
        /// <param name="gpu"></param>
        public Compositor(IRenderLoop loop, IPlatformGpu? gpu)
        {
            Loop = loop;
            _server = new ServerCompositor(loop, gpu, _batchObjectPool, _batchMemoryPool);
            _implicitBatchCommit = ImplicitBatchCommit;

            DefaultEasing = new CubicBezierEasing(new Point(0.25f, 0.1f), new Point(0.25f, 1f));
        }

        /// <summary>
        /// Creates a new CompositionTarget
        /// </summary>
        /// <param name="renderTargetFactory">A factory method to create IRenderTarget to be called from the render thread</param>
        /// <returns></returns>
        public CompositionTarget CreateCompositionTarget(Func<IRenderTarget> renderTargetFactory)
        {
            return new CompositionTarget(this, new ServerCompositionTarget(_server, renderTargetFactory));
        }

        /// <summary>
        /// Requests pending changes in the composition objects to be serialized and sent to the render thread
        /// </summary>
        /// <returns>A task that completes when sent changes are applied and rendered on the render thread</returns>
        public Task RequestCommitAsync()
        {
            Dispatcher.UIThread.VerifyAccess();
            var batch = new Batch();
            
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
            if (_invokeOnNextCommit != null) 
                ScheduleCommitCallbacks(batch.Completed);
            
            return batch.Completed;
        }

        async void ScheduleCommitCallbacks(Task task)
        {
            var list = _invokeOnNextCommit;
            _invokeOnNextCommit = null;
            await task;
            foreach (var i in list!)
                i();
            list.Clear();
            _invokeListPool.Push(list);
        }

        public CompositionContainerVisual CreateContainerVisual() => new(this, new ServerCompositionContainerVisual(_server));
        
        public ExpressionAnimation CreateExpressionAnimation() => new ExpressionAnimation(this);

        public ExpressionAnimation CreateExpressionAnimation(string expression) => new ExpressionAnimation(this)
        {
            Expression = expression
        };

        public ImplicitAnimationCollection CreateImplicitAnimationCollection() => new ImplicitAnimationCollection(this);

        public CompositionAnimationGroup CreateAnimationGroup() => new CompositionAnimationGroup(this);
        
        private void QueueImplicitBatchCommit()
        {
            if(_implicitBatchCommitQueued)
                return;
            _implicitBatchCommitQueued = true;
            Dispatcher.UIThread.Post(_implicitBatchCommit, DispatcherPriority.CompositionBatch);
        }

        private void ImplicitBatchCommit()
        {
            _implicitBatchCommitQueued = false;
            RequestCommitAsync();
        }

        internal void RegisterForSerialization(CompositionObject compositionObject)
        {
            Dispatcher.UIThread.VerifyAccess();
            _objectsForSerialization.Add(compositionObject);
            QueueImplicitBatchCommit();
        }

        internal void InvokeOnNextCommit(Action action)
        {
            _invokeOnNextCommit ??= _invokeListPool.Count > 0 ? _invokeListPool.Pop() : new();
            _invokeOnNextCommit.Add(action);
        }
    }
}
