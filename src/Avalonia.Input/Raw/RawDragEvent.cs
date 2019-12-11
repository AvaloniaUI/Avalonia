namespace Avalonia.Input.Raw
{
    public class RawDragEvent : RawInputEventArgs
    {
        public Point Location { get; set; }
        public IDataObject Data { get; }
        public DragDropEffects Effects { get; set; }
        public RawDragEventType Type { get; }
        public InputModifiers Modifiers { get; }

        public RawDragEvent(IDragDropDevice inputDevice, RawDragEventType type, 
            IInputRoot root, Point location, IDataObject data, DragDropEffects effects, RawInputModifiers modifiers)
            :base(inputDevice, 0, root)
        {
            Type = type;
            Location = location;
            Data = data;
            Effects = effects;
            Modifiers = (InputModifiers)modifiers;
        }
    }
}
