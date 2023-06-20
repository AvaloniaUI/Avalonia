using Avalonia.Metadata;

namespace Avalonia.Input.Raw
{
    [PrivateApi]
    public class RawTextInputEventArgs : RawInputEventArgs
    {
        public RawTextInputEventArgs(
            IKeyboardDevice device,
            ulong timestamp,
            IInputRoot root,
            string text)
            : base(device, timestamp, root)
        {
            Text = text;
        }

        public string Text { get; }
    }
}
