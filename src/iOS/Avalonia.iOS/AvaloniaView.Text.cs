using Avalonia.Input;
using Avalonia.Input.Raw;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace Avalonia.iOS
{
    [Adopts("UIKeyInput")]
    public partial class AvaloniaView
    {
        public override bool CanBecomeFirstResponder => true;

        [Export("hasText")] public bool HasText => false;

        [Export("insertText:")]
        public void InsertText(string text) =>
            _topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance,
                0, InputRoot, text));

        [Export("deleteBackward")]
        public void DeleteBackward()
        {
            // TODO: pass this through IME infrastructure instead of emulating a backspace press
            _topLevelImpl.Input?.Invoke(new RawKeyEventArgs(KeyboardDevice.Instance,
                0, InputRoot, RawKeyEventType.KeyDown, Key.Back, RawInputModifiers.None));
            
            _topLevelImpl.Input?.Invoke(new RawKeyEventArgs(KeyboardDevice.Instance,
                0, InputRoot, RawKeyEventType.KeyUp, Key.Back, RawInputModifiers.None));
        }
    }
}