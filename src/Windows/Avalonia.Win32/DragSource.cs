using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class DragSource : IPlatformDragSource
    {
        public Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent,
            IDataObject data, DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();
            triggerEvent.Pointer.Capture(null);
            OleDragSource src = new OleDragSource();
            DataObject dataObject = new DataObject(data);
            int allowed = (int)OleDropTarget.ConvertDropEffect(allowedEffects);

            UnmanagedMethods.DoDragDrop(dataObject, src, allowed, out var finalEffect);
            return Task.FromResult(OleDropTarget.ConvertDropEffect((DropEffect)finalEffect));
        }
    }
}
