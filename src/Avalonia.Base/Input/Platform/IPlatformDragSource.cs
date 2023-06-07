using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform
{
    [Unstable]
    public interface IPlatformDragSource
    {
        Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent, IDataObject data, DragDropEffects allowedEffects);
    }
}
