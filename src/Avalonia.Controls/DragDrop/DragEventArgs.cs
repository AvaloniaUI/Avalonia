using Avalonia.Interactivity;

namespace Avalonia.Controls.DragDrop
{
    public class DragEventArgs : RoutedEventArgs
    {
        public DragOperation DragOperation { get; set; }

        public IDragData Data { get; private set; }

        public DragEventArgs(RoutedEvent<DragEventArgs> routedEvent, IDragData data)
            : base(routedEvent)
        {
            this.Data = data;
        }

    }
}