using System.Threading.Tasks;

namespace Avalonia.Input.Platform
{
    public interface IPlatformDragSource
    {
        Task<DragDropEffects> DoDragDrop(IDataObject data, DragDropEffects allowedEffects);
    }
}
