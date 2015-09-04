namespace Perspex.Input.Raw
{
    public class RawTextInputEventArgs : RawInputEventArgs
    {

        public string Text { get; set; }

        public RawTextInputEventArgs(IKeyboardDevice device, uint timestamp, string text) : base(device, timestamp)
        {
            Text = text;
        }
    }
}
