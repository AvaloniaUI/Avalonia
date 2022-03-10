using Foundation;
using ObjCRuntime;
using UIKit;
using Avalonia.Input.TextInput;

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
        
    }

    [Export("deleteBackward")]
    public void DeleteBackward()
    {
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
