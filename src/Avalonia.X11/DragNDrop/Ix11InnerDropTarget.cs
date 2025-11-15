using System.Threading.Tasks;
using Avalonia.Input;

namespace Avalonia.X11.DragNDrop
{
    internal interface Ix11InnerDropTarget
    {
        DragDropEffects HandleDragEnter(PixelPoint coords, IDataTransfer dataObject, DragDropEffects effects);
        DragDropEffects HandleDragOver(PixelPoint coords, DragDropEffects effects);
        DragDropEffects HandleDrop(DragDropEffects effects);
        Task<DragDropEffects> HandleDragLeave(PixelPoint coords, DragDropEffects effects);
    }
}
