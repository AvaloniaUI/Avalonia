using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class DragSource : IPlatformDragSource
    {
        public unsafe Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent,
            IDataObject data, DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();

            triggerEvent.Pointer.Capture(null);
            
            var dataObject = new DataObject(data);
            var src = new OleDragSource();
            var allowed = OleDropTarget.ConvertDropEffect(allowedEffects);
            
            var objPtr = MicroCom.MicroComRuntime.GetNativeIntPtr<Win32Com.IDataObject>(dataObject, true);
            var srcPtr = MicroCom.MicroComRuntime.GetNativeIntPtr<Win32Com.IDropSource>(src, true);

            UnmanagedMethods.DoDragDrop(objPtr, srcPtr, (int)allowed, out var finalEffect);
            return Task.FromResult(OleDropTarget.ConvertDropEffect((Win32Com.DropEffect)finalEffect));
        }
    }
}
