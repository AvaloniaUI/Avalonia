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
            private TaskCompletionSource<DragDropEffects> _tcs;

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
        
        public Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent, IDataObject data, DragDropEffects allowedEffects)
        {
            // Sanity check
            var tl = TopLevel.GetTopLevel(triggerEvent.Source as Visual);
            var view = tl?.PlatformImpl as WindowBaseImpl;
            if (view == null)
                throw new ArgumentException();

            triggerEvent.Pointer.Capture(null);
            
            var tcs = new TaskCompletionSource<DragDropEffects>();
            
            var clipboardImpl = _factory.CreateDndClipboard();
            using (var clipboard = new ClipboardImpl(clipboardImpl))
            using (var cb = new DndCallback(tcs))
            {
                // Native API is synchronous, so it's OK. For now.
                clipboard.SetDataObjectAsync(data).GetAwaiter().GetResult();

                view.BeginDraggingSession((AvnDragDropEffects)allowedEffects,
                    triggerEvent.GetPosition(tl).ToAvnPoint(), clipboardImpl, cb,
                    GCHandle.ToIntPtr(GCHandle.Alloc(data)));
            }

            return tcs.Task;
        }
    }
}
