using System;
using Foundation;
using ObjCRuntime;
using Avalonia.Input.TextInput;
using Avalonia.Input;
using Avalonia.Input.Raw;
using CoreGraphics;
using UIKit;

namespace Avalonia.iOS;

#nullable enable

[Adopts("UITextInput")]
[Adopts("UITextInputTraits")]
[Adopts("UIKeyInput")]
public partial class AvaloniaView : ITextInputMethodImpl, IUITextInput
{
    private class AvaloniaTextRange : UITextRange
    {
        private readonly AvaloniaTextPosition _start;
        private readonly AvaloniaTextPosition _end;

        public AvaloniaTextRange(int start, int end)
        {
            _start = new AvaloniaTextPosition(start);
            _end = new AvaloniaTextPosition(end);
        }

        public override AvaloniaTextPosition Start => _start;

        public override AvaloniaTextPosition End => _end;
    }

    private class AvaloniaTextPosition : UITextPosition
    {
        public AvaloniaTextPosition(int offset)
        {
            Offset = offset;
        }

        public int Offset { get; }
    }

    private string _markedText = "";
    private readonly IUITextInputDelegate _inputDelegate;
    private ITextInputMethodClient? _client;
    private NSDictionary? _markedTextStyle;
    private readonly UITextPosition _beginningOfDocument = new AvaloniaTextPosition(0);
    private readonly UITextInputStringTokenizer _tokenizer;

    private class TextInputHandler : UITextInputDelegate
    {
    }

    public ITextInputMethodClient? Client => _client;

    public bool IsActive => _client != null;

    public override bool CanResignFirstResponder => true;

    public override bool CanBecomeFirstResponder => true;

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
        // maybe this will be cursor / selection rect?
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

    // Traits (Optional)
    [Export("keyboardType")] public UIKeyboardType KeyboardType { get; private set; } = UIKeyboardType.Default;

    [Export("isSecureTextEntry")] public bool IsSecureEntry { get; private set; }

    [Export("returnKeyType")] public UIReturnKeyType ReturnKeyType { get; set; }

    void IUIKeyInput.InsertText(string text)
    {
        if (_client == null)
        {
            return;
        }

        if (text == "\n")
        {
            // emulate return key released.
        }

        switch (ReturnKeyType)
        {
            case UIReturnKeyType.Done:
            case UIReturnKeyType.Search:
            case UIReturnKeyType.Go:
            case UIReturnKeyType.Send:
                ResignFirstResponder();
                return;
        }

        // TODO replace this with _client.SetCommitText?
        if (KeyboardDevice.Instance is { })
        {
            _topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance,
                0, InputRoot, text));
        }
    }

    void IUIKeyInput.DeleteBackward()
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

    bool IUIKeyInput.HasText => true;

    string IUITextInput.TextInRange(UITextRange range)
    {
        var text = _client.SurroundingText.Text;

        if (!string.IsNullOrWhiteSpace(_markedText))
        {
            // todo check this combining _marked text with surrounding text.
            int cursorPos = _client.SurroundingText.CursorOffset;
            text = text[.. cursorPos] + _markedText + text[cursorPos ..];
        }

        var start = (range.Start as AvaloniaTextPosition).Offset;
        int end = (range.End as AvaloniaTextPosition).Offset;

        return text[start .. end];
    }

    void IUITextInput.ReplaceText(UITextRange range, string text)
    {
        ((IUITextInput)this).SelectedTextRange = range;

        // todo _client.SetCommitText(text);
        if (KeyboardDevice.Instance is { })
        {
            _topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance,
                0, InputRoot, text));
        }
    }

    void IUITextInput.SetMarkedText(string markedText, NSRange selectedRange)
    {
        _markedText = markedText;

        // todo check this... seems correct
        _client.SetPreeditText(markedText);
    }

    void IUITextInput.UnmarkText()
    {
        if (string.IsNullOrWhiteSpace(_markedText))
            return;

        // todo _client.CommitString (_markedText);

        _markedText = "";
    }

    UITextRange IUITextInput.GetTextRange(UITextPosition fromPosition, UITextPosition toPosition)
    {
        if (fromPosition is AvaloniaTextPosition f && toPosition is AvaloniaTextPosition t)
        {
            // todo check calculation.
            return new AvaloniaTextRange(f.Offset, t.Offset);
        }

        throw new Exception();
    }

    UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, nint offset)
    {
        if (fromPosition is AvaloniaTextPosition f)
        {
            var position = f.Offset;
            int posPlusIndex = position + (int)offset;
            var length = _client.SurroundingText.Text.Length;

            if (posPlusIndex < 0 || posPlusIndex > length)
            {
                return null;
            }

            return new AvaloniaTextPosition(posPlusIndex);
        }

        throw new Exception();
    }

    UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection, nint offset)
    {
        if (fromPosition is AvaloniaTextPosition f)
        {
            var pos = f.Offset;

            switch (inDirection)
            {
                case UITextLayoutDirection.Left:
                    return new AvaloniaTextPosition(pos - (int)offset);

                case UITextLayoutDirection.Right:
                    return new AvaloniaTextPosition(pos + (int)offset);

                default:
                    return fromPosition;
            }
        }

        throw new Exception();
    }

    NSComparisonResult IUITextInput.ComparePosition(UITextPosition first, UITextPosition second)
    {
        if (first is AvaloniaTextPosition f && second is AvaloniaTextPosition s)
        {
            if (f.Offset > s.Offset)
                return NSComparisonResult.Ascending;

            if (f.Offset < s.Offset)
                return NSComparisonResult.Descending;

            return NSComparisonResult.Same;
        }

        throw new Exception();
    }

    nint IUITextInput.GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition)
    {
        if (fromPosition is AvaloniaTextPosition f && toPosition is AvaloniaTextPosition t)
        {
            return t.Offset - f.Offset;
        }

        throw new Exception();
    }

    UITextPosition IUITextInput.GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction)
    {
        if (range is AvaloniaTextRange r)
        {
            switch (direction)
            {
                case UITextLayoutDirection.Right:
                    return r.End;

                default:
                    return r.Start;
            }
        }

        throw new Exception();
    }

    UITextRange IUITextInput.GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction)
    {
        if (byExtendingPosition is AvaloniaTextPosition p)
        {
            switch (direction)
            {
                case UITextLayoutDirection.Left:
                    return new AvaloniaTextRange(0, p.Offset);

                default:
                    // todo check this.
                    return new AvaloniaTextRange(p.Offset, _client.SurroundingText.Text.Length);
            }
        }

        throw new Exception();
    }

    NSWritingDirection IUITextInput.GetBaseWritingDirection(UITextPosition forPosition,
        UITextStorageDirection direction)
    {
        return NSWritingDirection.LeftToRight;

        // todo query and retyrn RTL.
    }

    void IUITextInput.SetBaseWritingDirectionforRange(NSWritingDirection writingDirection, UITextRange range)
    {
        // todo ? ignore?
    }

    CGRect IUITextInput.GetFirstRectForRange(UITextRange range)
    {
        if (_client == null)
            return CGRect.Empty;

        if (!string.IsNullOrWhiteSpace(_markedText))
        {
            return CGRect.Empty;
        }

        if (range is AvaloniaTextRange r)
        {
            // todo add ime apis to get cursor rect.
            throw new NotImplementedException();
        }

        throw new Exception();
    }

    CGRect IUITextInput.GetCaretRectForPosition(UITextPosition? position)
    {
        var rect = _client.CursorRectangle;

        return new CGRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    UITextPosition IUITextInput.GetClosestPositionToPoint(CGPoint point)
    {
        // TODO HitTest text? 
        throw new System.NotImplementedException();
    }

    UITextPosition IUITextInput.GetClosestPositionToPoint(CGPoint point, UITextRange withinRange)
    {
        // TODO HitTest text? 
        throw new System.NotImplementedException();
    }

    UITextRange IUITextInput.GetCharacterRangeAtPoint(CGPoint point)
    {
        // TODO check if needed, hittest?
        return new AvaloniaTextRange(_client.SurroundingText.CursorOffset, _client.SurroundingText.CursorOffset);
    }

    UITextSelectionRect[] IUITextInput.GetSelectionRects(UITextRange range)
    {
        // todo?
        return Array.Empty<UITextSelectionRect>();
    }

    UITextRange? IUITextInput.SelectedTextRange
    {
        get
        {
            return new AvaloniaTextRange(
                Math.Min(_client.SurroundingText.CursorOffset, _client.SurroundingText.AnchorOffset),
                Math.Max(_client.SurroundingText.CursorOffset, _client.SurroundingText.AnchorOffset));
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    NSDictionary? IUITextInput.MarkedTextStyle
    {
        get => _markedTextStyle;
        set => _markedTextStyle = value;
    }

    UITextPosition IUITextInput.BeginningOfDocument => _beginningOfDocument;

    UITextPosition IUITextInput.EndOfDocument
    {
        get
        {
            return new AvaloniaTextPosition(_client.SurroundingText.Text.Length + _markedText.Length);
        }
    }

    NSObject? IUITextInput.WeakInputDelegate
    {
        get => _inputDelegate as TextInputHandler;
        set => throw new NotSupportedException();
    }

    NSObject IUITextInput.WeakTokenizer => _tokenizer;

    UITextRange IUITextInput.MarkedTextRange
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_markedText))
            {
                return null;
            }

            return new AvaloniaTextRange(0, _markedText.Length);            
        }
    }
}
