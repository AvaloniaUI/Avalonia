using Avalonia.Input;

namespace Avalonia.Controls.DragDrop
{
    /// <summary>
    /// Dispatches Drag+Drop events to the correct visual targets, based on the input root and the drag location.
    /// </summary>
    public interface IDragDispatcher
    {
        DragOperation DragEnter(IInputElement inputRoot, Point point, IDragData data, DragOperation operation);
        DragOperation DragOver(IInputElement inputRoot, Point point, IDragData data, DragOperation operation);
        void DragLeave(IInputElement inputRoot);
        DragOperation Drop(IInputElement inputRoot, Point point, IDragData data, DragOperation operation);
    }
}