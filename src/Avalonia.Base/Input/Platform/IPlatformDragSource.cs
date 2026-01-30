using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform
{
    [NotClientImplementable]
    public interface IPlatformDragSource
    {
        Task<DragDropEffects> DoDragDropAsync(
            PointerEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects);
    }
}
