using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class RequestBringIntoViewEventArgs : RoutedEventArgs
    {
        public IVisual? TargetObject { get; set; }

        public Rect TargetRect { get; set; }
    }
}
