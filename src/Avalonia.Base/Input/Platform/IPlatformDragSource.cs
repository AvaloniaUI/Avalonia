using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform
{
    [PrivateApi]
    public interface IPlatformDragSource
    {
        Task<DragDropEffects> DoDragDropAsync(
            PointerPressedEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects);
    }
}
