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
        public Task<DragDropEffects> DoDragDropAsync(
            PointerPressedEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();

            triggerEvent.Pointer.Capture(null);
            
            if (triggerEvent.Pointer.Type != PointerType.Mouse
                && UnmanagedMethods.GetKeyState(UnmanagedMethods.VirtualKeyStates.VK_LBUTTON) >= 0
                && TopLevel.GetTopLevel(triggerEvent.Source as Visual)?.PlatformImpl is WindowImpl window)
            {
                var result = new TaskCompletionSource<DragDropEffects>(TaskCreationOptions.RunContinuationsAsynchronously);
                window.SetPendingDrag(
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
                    () => result.TrySetResult(DragDropEffects.None));
                return result.Task;
            }

            return Task.FromResult(StartDragDrop(dataTransfer, allowedEffects));
        }

        private static DragDropEffects StartDragDrop(IDataTransfer dataTransfer, DragDropEffects allowedEffects)
        {
            using var dataObject = new DataTransferToOleDataObjectWrapper(dataTransfer);
            using var src = new OleDragSource();
            var allowed = OleDropTarget.ConvertDropEffect(allowedEffects);
            
            var objPtr = dataObject.GetNativeIntPtr<Win32Com.IDataObject>();
            var srcPtr = src.GetNativeIntPtr<Win32Com.IDropSource>();

            UnmanagedMethods.DoDragDrop(objPtr, srcPtr, (int)allowed, out var finalEffect);
            
            // Force releasing of internal wrapper to avoid memory leak, if drop target keeps com reference.
            dataObject.ReleaseDataTransfer();

            return OleDropTarget.ConvertDropEffect((Win32Com.DropEffect)finalEffect);
        }
    }
}
