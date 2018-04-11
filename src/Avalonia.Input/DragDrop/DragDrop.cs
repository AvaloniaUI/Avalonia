using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Input.Platform;

namespace Avalonia.Input.DragDrop
{
    public static class DragDrop
    {
        public static RoutedEvent<DragEventArgs> DragEnterEvent = RoutedEvent.Register<DragEventArgs>("DragEnter", RoutingStrategies.Bubble, typeof(DragDrop));
        public static RoutedEvent<RoutedEventArgs> DragLeaveEvent = RoutedEvent.Register<RoutedEventArgs>("DragLeave", RoutingStrategies.Bubble, typeof(DragDrop));
        public static RoutedEvent<DragEventArgs> DragOverEvent = RoutedEvent.Register<DragEventArgs>("DragOver", RoutingStrategies.Bubble, typeof(DragDrop));
        public static RoutedEvent<DragEventArgs> DropEvent = RoutedEvent.Register<DragEventArgs>("Drop", RoutingStrategies.Bubble, typeof(DragDrop));

        public static AvaloniaProperty<bool> AcceptDragProperty = AvaloniaProperty.RegisterAttached<Interactive, bool>("AcceptDrag", typeof(DragDrop), inherits: true);

        public static bool GetAcceptDrag(Interactive interactive)
        {
            return interactive.GetValue(AcceptDragProperty);
        }
        
        public static void SetAcceptDrag(Interactive interactive, bool value)
        {
            interactive.SetValue(AcceptDragProperty, value);
        }

        /// <summary>
        /// Starts a dragging operation with the given <see cref="IDataObject"/> and returns the applied drop effect from the target.
        /// <seealso cref="DataObject"/>
        /// </summary>
        public static Task<DragDropEffects> DoDragDrop(IDataObject data, DragDropEffects allowedEffects)
        {
            var src = AvaloniaLocator.Current.GetService<IPlatformDragSource>();
            return src?.DoDragDrop(data, allowedEffects) ?? Task.FromResult(DragDropEffects.None);
        }
    }
}