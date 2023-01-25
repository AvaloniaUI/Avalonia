using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32
{
    class DragSource : IPlatformDragSource
    {
        public unsafe Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent,
            IDataObject data, DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();

            triggerEvent.Pointer.Capture(null);
            
            using var dataObject = new DataObject(data);
            using var src = new OleDragSource();
            var allowed = OleDropTarget.ConvertDropEffect(allowedEffects);
            
            var objPtr = MicroComRuntime.GetNativeIntPtr<Win32Com.IDataObject>(dataObject);
            var srcPtr = MicroComRuntime.GetNativeIntPtr<Win32Com.IDropSource>(src);

            UnmanagedMethods.DoDragDrop(objPtr, srcPtr, (int)allowed, out var finalEffect);
            
            // Force releasing of internal wrapper to avoid memory leak, if drop target keeps com reference.
            dataObject.ReleaseWrapped();

            return Task.FromResult(OleDropTarget.ConvertDropEffect((Win32Com.DropEffect)finalEffect));
        }
    }
}
