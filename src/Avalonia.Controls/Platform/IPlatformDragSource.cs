using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.DragDrop;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Platform
{
    public interface IPlatformDragSource
    {
        Task<DragDropEffects> DoDragDrop(IDataObject data, DragDropEffects allowedEffects);
    }
}
