using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Native
{
    class AvaloniaNativeDragSource : IPlatformDragSource
    {
        private readonly IAvaloniaNativeFactory _factory;

        public AvaloniaNativeDragSource(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }
        
        TopLevel FindRoot(IInteractive interactive)
        {
            while (interactive != null && !(interactive is IVisual))
                interactive = interactive.InteractiveParent;
            if (interactive == null)
                return null;
            var visual = (IVisual)interactive;
            return visual.VisualRoot as TopLevel;
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
            var tl = FindRoot(triggerEvent.Source);
            var view = tl?.PlatformImpl as WindowBaseImpl;
            if (view == null)
                throw new ArgumentException();

            triggerEvent.Pointer.Capture(null);
            
            var tcs = new TaskCompletionSource<DragDropEffects>();
            
            var clipboardImpl = _factory.CreateDndClipboard();
            using (var clipboard = new ClipboardImpl(clipboardImpl))
            using (var cb = new DndCallback(tcs))
            {
                if (data.Contains(DataFormats.Text))
                    // API is synchronous, so it's OK
                    clipboard.SetTextAsync(data.GetText()).Wait();
                
                view.BeginDraggingSession((AvnDragDropEffects)allowedEffects,
                    triggerEvent.GetPosition(tl).ToAvnPoint(), clipboardImpl, cb,
                    GCHandle.ToIntPtr(GCHandle.Alloc(data)));
            }

            return tcs.Task;
        }
    }
}
