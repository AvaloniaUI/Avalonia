using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32
{
    internal sealed class DragSource : IPlatformDragSource
    {
        private static bool s_isDragging;

        public Task<DragDropEffects> DoDragDropAsync(
            PointerPressedEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();

            triggerEvent.Pointer.Capture(null);

            // Windows emulates the mouse messages that OLE follows for the primary contact only, so a second finger would drag with the wrong one.
            if (!triggerEvent.Pointer.IsPrimary)
            {
                dataTransfer.Dispose();
                return Task.FromResult(DragDropEffects.None);
            }

            if (TopLevel.GetTopLevel(triggerEvent.Source as Visual)?.PlatformImpl is WindowImpl window)
            {
                var result = new TaskCompletionSource<DragDropEffects>(TaskCreationOptions.RunContinuationsAsynchronously);

                var pending = window.TrySetPendingDrag(
                    triggerEvent,
                    () =>
                    {
                        try
                        {
                            result.TrySetResult(StartDragDrop(dataTransfer, allowedEffects));
                        }
                        catch (Exception ex)
                        {
                            result.TrySetException(ex);
                        }
                    },
                    () =>
                    {
                        dataTransfer.Dispose();
                        result.TrySetResult(DragDropEffects.None);
                    });

                if (pending)
                {
                    return result.Task;
                }
            }

            return Task.FromResult(StartDragDrop(dataTransfer, allowedEffects));
        }

        private static DragDropEffects StartDragDrop(IDataTransfer dataTransfer, DragDropEffects allowedEffects)
        {
            // Touch messages are still dispatched during a drag, so another press can start a second one, which OLE rejects.
            if (s_isDragging)
            {
                dataTransfer.Dispose();
                return DragDropEffects.None;
            }

            using var dataObject = new DataTransferToOleDataObjectWrapper(dataTransfer);
            using var src = new OleDragSource();
            var allowed = OleDropTarget.ConvertDropEffect(allowedEffects);
            
            var objPtr = dataObject.GetNativeIntPtr<Win32Com.IDataObject>();
            var srcPtr = src.GetNativeIntPtr<Win32Com.IDropSource>();

            int finalEffect;
            s_isDragging = true;
            try
            {
                UnmanagedMethods.DoDragDrop(objPtr, srcPtr, (int)allowed, out finalEffect);
            }
            finally
            {
                s_isDragging = false;
            }
            
            // Force releasing of internal wrapper to avoid memory leak, if drop target keeps com reference.
            dataObject.ReleaseDataTransfer();

            return OleDropTarget.ConvertDropEffect((Win32Com.DropEffect)finalEffect);
        }
    }
}
