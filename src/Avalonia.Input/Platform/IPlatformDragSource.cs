using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input.DragDrop;
using Avalonia.Interactivity;

namespace Avalonia.Input.Platform
{
    public interface IPlatformDragSource
    {
        Task<DragDropEffects> DoDragDrop(IDataObject data, DragDropEffects allowedEffects);
    }
}
