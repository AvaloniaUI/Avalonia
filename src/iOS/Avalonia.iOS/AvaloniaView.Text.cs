using Foundation;
using ObjCRuntime;
using Avalonia.Input.TextInput;
using Avalonia.Input;
using Avalonia.Input.Raw;
using UIKit;

namespace Avalonia.iOS;

#nullable enable

[Adopts("UITextInputTraits")]
[Adopts("UIKeyInput")]
public partial class AvaloniaView : ITextInputMethodImpl
{
    private IUITextInputDelegate _inputDelegate;
    private ITextInputMethodClient? _client;

    class TextInputHandler : UITextInputDelegate
    {
    }
    
    public ITextInputMethodClient? Client => _client;
    public bool IsActive => _client != null;
    public override bool CanResignFirstResponder => true;
    public override bool CanBecomeFirstResponder => true;

    [Export("hasText")]
    public bool HasText
    {
        get
        {
            if (Client is { } && Client.SupportsSurroundingText &&
                Client.SurroundingText.Text.Length > 0)
            {
                return true;
            }

            return false;
        }
    }

    [Export("keyboardType")] public UIKeyboardType KeyboardType { get; private set; } = UIKeyboardType.Default;

    [Export("isSecureTextEntry")] public bool IsSecureEntry { get; private set; }

    [Export("insertText:")]
    public void InsertText(string text)
    {
        if (KeyboardDevice.Instance is { })
        {
            _topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance,
                0, InputRoot, text));
        }
    }

    public IUITextInputDelegate InputDelegate => _inputDelegate;

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

    void ITextInputMethodImpl.SetClient(ITextInputMethodClient? client)
    {
        _client = client;

        if (_client is { })
        {
            BecomeFirstResponder();
        }
        else
        {
            ResignFirstResponder();
        }
    }

    void ITextInputMethodImpl.SetCursorRect(Rect rect)
    {

    }

    void ITextInputMethodImpl.SetOptions(TextInputOptions options)
    {
        IsSecureEntry = false;
        
        switch (options.ContentType)
        {
            case TextInputContentType.Normal:
                KeyboardType = UIKeyboardType.Default;
                break;
            
            case TextInputContentType.Alpha:
                KeyboardType = UIKeyboardType.AsciiCapable;
                break;
            
            case TextInputContentType.Digits:
                KeyboardType = UIKeyboardType.PhonePad;
                break;
            
            case TextInputContentType.Pin:
                KeyboardType = UIKeyboardType.NumberPad;
                IsSecureEntry = true;
                break;
            
            case TextInputContentType.Number:
                KeyboardType = UIKeyboardType.PhonePad;
                break;
            
            case TextInputContentType.Email:
                KeyboardType = UIKeyboardType.EmailAddress;
                break;

            case TextInputContentType.Url:
                KeyboardType = UIKeyboardType.Url;
                break;
            
            case TextInputContentType.Name:
                KeyboardType = UIKeyboardType.NamePhonePad;
                break;
            
            case TextInputContentType.Password:
                KeyboardType = UIKeyboardType.Default;
                IsSecureEntry = true;
                break;
            
            case TextInputContentType.Social:
                KeyboardType = UIKeyboardType.Twitter;
                break;
                
            case TextInputContentType.Search:
                KeyboardType = UIKeyboardType.WebSearch;
                break;
        }

        if (options.IsSensitive)
        {
            IsSecureEntry = true;
        }
    }

    void ITextInputMethodImpl.Reset()
    {
        ResignFirstResponder();
    }
}
