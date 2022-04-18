using System;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Wayland
{
    public class WlDragSource : IPlatformDragSource
    {
        public Task<DragDropEffects> DoDragDrop(PointerEventArgs triggerEvent, IDataObject data, DragDropEffects allowedEffects)
        {
            throw new NotImplementedException();
        }
    }
}
