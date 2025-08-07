using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32
{
    internal sealed class DragSource : IPlatformDragSource
    {
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
            Dispatcher.UIThread.VerifyAccess();

            triggerEvent.Pointer.Capture(null);
            
            using var dataObject = new DataTransferToOleDataObjectWrapper(dataTransfer);
            using var src = new OleDragSource();
            var allowed = OleDropTarget.ConvertDropEffect(allowedEffects);
            
            var objPtr = dataObject.GetNativeIntPtr<Win32Com.IDataObject>();
            var srcPtr = src.GetNativeIntPtr<Win32Com.IDropSource>();

            UnmanagedMethods.DoDragDrop(objPtr, srcPtr, (int)allowed, out var finalEffect);
            
            // Force releasing of internal wrapper to avoid memory leak, if drop target keeps com reference.
            dataObject.ReleaseDataTransfer();

            return Task.FromResult(OleDropTarget.ConvertDropEffect((Win32Com.DropEffect)finalEffect));
        }
    }
}
