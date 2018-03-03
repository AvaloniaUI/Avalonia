using Avalonia.Input;

namespace Avalonia.Controls.DragDrop
{
    /// <summary>
    /// Dispatches Drag+Drop events to the correct visual targets, based on the input root and the drag location.
    /// </summary>
    public interface IDragDispatcher
    {
        DragDropEffects DragEnter(IInputElement inputRoot, Point point, IDragData data, DragDropEffects effects);
        DragDropEffects DragOver(IInputElement inputRoot, Point point, IDragData data, DragDropEffects effects);
        void DragLeave(IInputElement inputRoot);
        DragDropEffects Drop(IInputElement inputRoot, Point point, IDragData data, DragDropEffects effects);
    }
}