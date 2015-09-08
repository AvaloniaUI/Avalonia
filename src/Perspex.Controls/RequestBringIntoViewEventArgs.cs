





namespace Perspex.Controls
{
    using Perspex.Interactivity;

    public class RequestBringIntoViewEventArgs : RoutedEventArgs
    {
        public IVisual TargetObject { get; set; }

        public Rect TargetRect { get; set; }
    }
}
