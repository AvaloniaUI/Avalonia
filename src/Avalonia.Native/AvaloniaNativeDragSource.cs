using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    class AvaloniaNativeDragSource : IPlatformDragSource
    {
        private readonly IAvaloniaNativeFactory _factory;

        public AvaloniaNativeDragSource(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        class DndCallback : NativeCallbackBase, IAvnDndResultCallback
        {
            private TaskCompletionSource<DragDropEffects>? _tcs;

            public DndCallback(TaskCompletionSource<DragDropEffects> tcs)
            {
                _tcs = tcs;
            }
            public void OnDragAndDropComplete(AvnDragDropEffects effect)
            {
                _tcs?.TrySetResult((DragDropEffects)effect);
                _tcs = null;
            }
        }

        [Obsolete($"Use {nameof(DoDragDropAsync)} instead.")]
        Task<DragDropEffects> IPlatformDragSource.DoDragDrop(
            PointerEventArgs triggerEvent,
            IDataObject data,
            DragDropEffects allowedEffects)
            => DoDragDropAsync(triggerEvent, new DataObjectToDataTransferWrapper(data), allowedEffects);

        public Task<DragDropEffects> DoDragDropAsync(
            PointerEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects)
        {
            // Sanity check
            var tl = TopLevel.GetTopLevel(triggerEvent.Source as Visual);
            var view = tl?.PlatformImpl as TopLevelImpl;
            if (view == null)
                throw new ArgumentException();

            triggerEvent.Pointer.Capture(null);
            
            var tcs = new TaskCompletionSource<DragDropEffects>();

            using (var cb = new DndCallback(tcs))
            {
                var dataSource = new DataTransferToAvnClipboardDataSourceWrapper(dataTransfer);

                view.BeginDraggingSession((AvnDragDropEffects)allowedEffects,
                    triggerEvent.GetPosition(tl).ToAvnPoint(), dataSource, cb,
                    GCHandle.ToIntPtr(GCHandle.Alloc(dataTransfer)));
            }

            return tcs.Task;
        }
    }
}
