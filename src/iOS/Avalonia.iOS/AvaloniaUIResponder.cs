using Foundation;
using ObjCRuntime;
using UIKit;
using Avalonia.Input.TextInput;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.iOS;

[Adopts("UIKeyInput")]
public partial class AvaloniaView : ITextInputMethodImpl
{
    public override bool CanResignFirstResponder => true;
    public override bool CanBecomeFirstResponder => true;
    public override bool CanBecomeFocused => true;

    [Export("hasText")] public bool HasText => false;

    [Export("insertText:")]
    public void InsertText(string text)
    {
        if (KeyboardDevice.Instance is { })
        {
            _topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance,
                0, InputRoot, text));
        }

    }

    [Export("deleteBackward")]
    public void DeleteBackward()
    {
        if (KeyboardDevice.Instance is { })
        {
            // TODO: pass this through IME infrastructure instead of emulating a backspace press
            _topLevelImpl.Input?.Invoke(new RawKeyEventArgs(KeyboardDevice.Instance,
                0, InputRoot, RawKeyEventType.KeyDown, Key.Back, RawInputModifiers.None));

            _topLevelImpl.Input?.Invoke(new RawKeyEventArgs(KeyboardDevice.Instance,
                0, InputRoot, RawKeyEventType.KeyUp, Key.Back, RawInputModifiers.None));
        }
    }

    void ITextInputMethodImpl.SetActive(bool active)
    {
        if (active)
        {
            var isFr = IsFirstResponder;
            var next = NextResponder;
            var result = BecomeFirstResponder();
        }
        else
        {
            ResignFirstResponder();
        }
    }

    void ITextInputMethodImpl.SetCursorRect(Rect rect)
    {
        
    }

    void ITextInputMethodImpl.SetOptions(TextInputOptionsQueryEventArgs options)
    {
        
    }

    void ITextInputMethodImpl.Reset()
    {
        
    }
}
