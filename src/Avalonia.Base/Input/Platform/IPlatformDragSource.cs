using System;
using System.Threading.Tasks;
using Avalonia.Metadata;

namespace Avalonia.Input.Platform
{
    [NotClientImplementable]
    public interface IPlatformDragSource
    {
        [Obsolete($"Use {nameof(DoDragDropAsync)} instead.")]
        Task<DragDropEffects> DoDragDrop(
            PointerEventArgs triggerEvent,
            IDataObject data,
            DragDropEffects allowedEffects);

        Task<DragDropEffects> DoDragDropAsync(
            PointerEventArgs triggerEvent,
            IDataTransfer dataTransfer,
            DragDropEffects allowedEffects);
    }
}
