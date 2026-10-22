using System;
using System.Threading.Tasks;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Android.Platform
{
    internal class AndroidDragSource : IPlatformDragSource
    {
        public Task<DragDropEffects> DoDragDropAsync(PointerPressedEventArgs triggerEvent, IDataTransfer dataTransfer, DragDropEffects allowedEffects)
        {
            var source = triggerEvent.Source as Visual;
            if (TopLevel.GetTopLevel(source)?.PlatformImpl is not TopLevelImpl topLevelImpl)
            {
                throw new InvalidOperationException("TopLevelImpl is null");
            }
            return topLevelImpl.DragDrop.StartDragDrop(dataTransfer);
        }
    }
}
