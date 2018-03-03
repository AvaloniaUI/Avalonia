using Avalonia.Interactivity;

namespace Avalonia.Controls.DragDrop
{
    public sealed class DragDrop : AvaloniaObject
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
    }
}