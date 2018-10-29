using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    class DragSource : IPlatformDragSource
    {
        public Task<DragDropEffects> DoDragDrop(IDataObject data, DragDropEffects allowedEffects)
        {
            Dispatcher.UIThread.VerifyAccess();

            OleDragSource src = new OleDragSource();
            DataObject dataObject = new DataObject(data);
            int allowed = (int)OleDropTarget.ConvertDropEffect(allowedEffects);

            int[] finalEffect = new int[1];
            UnmanagedMethods.DoDragDrop(dataObject, src, allowed, finalEffect);

            return Task.FromResult(OleDropTarget.ConvertDropEffect((DropEffect)finalEffect[0]));}
    }
}
