using Avalonia.Input;

namespace Avalonia.Controls.DragDrop
{
    public interface IDragDispatcher
    {
        DragOperation DragEnter(IInputRoot inputRoot, Point point, IDragData data, DragOperation operation);
        DragOperation DragOver(IInputRoot inputRoot, Point point, IDragData data, DragOperation operation);
        void DragLeave(IInputRoot inputRoot);
        DragOperation Drop(IInputRoot inputRoot, Point point, IDragData data, DragOperation operation);
    }
}