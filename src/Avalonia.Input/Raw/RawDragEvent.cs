namespace Avalonia.Input.Raw
{
    public class RawDragEvent : RawInputEventArgs
    {
        public IInputElement InputRoot { get; }
        public Point Location { get; set; }
        public IDataObject Data { get; }
        public DragDropEffects Effects { get; set; }
        public RawDragEventType Type { get; }
        public InputModifiers Modifiers { get; }

        public RawDragEvent(IDragDropDevice inputDevice, RawDragEventType type, 
            IInputElement inputRoot, Point location, IDataObject data, DragDropEffects effects, InputModifiers modifiers)
            :base(inputDevice, 0)
        {
            Type = type;
            InputRoot = inputRoot;
            Location = location;
            Data = data;
            Effects = effects;
            Modifiers = modifiers;
        }
    }
}
