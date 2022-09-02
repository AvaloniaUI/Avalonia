using System;
using System.Runtime.InteropServices;
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
public class AvaloniaResponder : UIResponder, IUITextInput
{
    private class AvaloniaTextRange : UITextRange
    {
        private readonly NSRange _range;

        public AvaloniaTextRange(NSRange range)
        {
            _range = range;
        }

        public override AvaloniaTextPosition Start => new AvaloniaTextPosition((int)_range.Location);

        public override AvaloniaTextPosition End => new AvaloniaTextPosition((int)(_range.Location + _range.Length));

        public NSRange Range => _range;

        public override bool IsEmpty => _range.Length == 0;
    }

    private class AvaloniaTextPosition : UITextPosition
    {
        public AvaloniaTextPosition(nint index)
        {
            Index = index;
        }

        public nint Index { get; }
        
        //[Export("inputDelegate")]
        //public UITextInputDelegate InputDelegate { get; set; }

        [Export("foo")]
        public void Foo()
        {
            
        }
    }

    private UIResponder _nextResponder;

    public AvaloniaResponder(AvaloniaView view, ITextInputMethodClient client)
    {
        _nextResponder = view;
        _client = client;
        _tokenizer = new UITextInputStringTokenizer(this);
        
    }

    public override UIResponder NextResponder => _nextResponder;

    public override bool CanPerform(Selector action, NSObject? withSender)
    {
        return base.CanPerform(action, withSender);
    }

    private string _markedText = "";
    private ITextInputMethodClient? _client;
    private NSDictionary? _markedTextStyle;
    private readonly UITextPosition _beginningOfDocument = new AvaloniaTextPosition(0);
    private readonly UITextInputStringTokenizer _tokenizer;

    public ITextInputMethodClient? Client => _client;

    public bool IsActive => _client != null;

    public override bool CanResignFirstResponder => true;

    public override bool CanBecomeFirstResponder => true;

    public override UIEditingInteractionConfiguration EditingInteractionConfiguration =>
        UIEditingInteractionConfiguration.Default;

    public override NSString TextInputContextIdentifier => new NSString("Test");

    public override UITextInputMode TextInputMode => UITextInputMode.CurrentInputMode;

    [DllImport("/usr/lib/libobjc.dylib")]
    extern static void objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

    private static readonly IntPtr SelectionWillChange = Selector.GetHandle("selectionWillChange:");
    private static readonly IntPtr SelectionDidChange = Selector.GetHandle("selectionDidChange:");
    private static readonly IntPtr TextWillChange = Selector.GetHandle("textWillChange:");
    private static readonly IntPtr TextDidChange = Selector.GetHandle("textDidChange:");
        
    private void ClientOnCursorRectangleChanged(object? sender, EventArgs e)
    {
        objc_msgSend(WeakInputDelegate.Handle.Handle, SelectionWillChange, this.Handle.Handle);
        objc_msgSend(WeakInputDelegate.Handle.Handle, SelectionDidChange, this.Handle.Handle);
    }

    private void ClientOnSurroundingTextChanged(object? sender, EventArgs e)
    {
        objc_msgSend(WeakInputDelegate.Handle.Handle, TextWillChange, Handle.Handle);
        objc_msgSend(WeakInputDelegate.Handle.Handle, TextDidChange, Handle.Handle);
    }

    // Traits (Optional)
    [Export("autocapitalizationType")] public UITextAutocapitalizationType AutocapitalizationType { get; private set; }
    
    [Export("autocorrectionType")] public UITextAutocorrectionType AutocorrectionType { get; private set; }
    [Export("keyboardType")] public UIKeyboardType KeyboardType { get; private set; } = UIKeyboardType.Default;

    [Export("keyboardAppearance")]
    public UIKeyboardAppearance KeyboardAppearance { get; private set; } = UIKeyboardAppearance.Default;
    
    [Export("returnKeyType")] public UIReturnKeyType ReturnKeyType { get; set; }
    
    [Export("enablesReturnKeyAutomatically")] public bool EnablesReturnKeyAutomatically { get; set; }
    [Export("isSecureTextEntry")] public bool IsSecureEntry { get; private set; }

    [Export("spellCheckingType")]
    public UITextSpellCheckingType SpellCheckingType { get; set; } = UITextSpellCheckingType.Default;
    
    [Export("textContentType")]
    public NSString TextContentType { get; set; }
  
    [Export("smartQuotesType")]
    public UITextSmartQuotesType SmartQuotesType { get; set; } = UITextSmartQuotesType.Default;
    
    [Export("smartDashesType")]
    public UITextSmartDashesType SmartDashesType { get; set; } = UITextSmartDashesType.Default;
  
    [Export("smartInsertDeleteType")]
    public UITextSmartInsertDeleteType SmartInsertDeleteType { get; set; } = UITextSmartInsertDeleteType.Default;
  
    [Export("passwordRules")]
    public UITextInputPasswordRules PasswordRules { get; set; }

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
            /*_topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance,
                0, InputRoot, text));*/
        }
    }

    void IUIKeyInput.DeleteBackward()
    {
        if (KeyboardDevice.Instance is { })
        {
            // TODO: pass this through IME infrastructure instead of emulating a backspace press
            /*_topLevelImpl.Input?.Invoke(new RawKeyEventArgs(KeyboardDevice.Instance,
                0, InputRoot, RawKeyEventType.KeyDown, Key.Back, RawInputModifiers.None));

            _topLevelImpl.Input?.Invoke(new RawKeyEventArgs(KeyboardDevice.Instance,
                0, InputRoot, RawKeyEventType.KeyUp, Key.Back, RawInputModifiers.None));*/
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

        var start = (int)(range.Start as AvaloniaTextPosition).Index;
        var end = (int)(range.End as AvaloniaTextPosition).Index;

        var result = text[start .. end];

        return result;
    }

    void IUITextInput.ReplaceText(UITextRange range, string text)
    {
        ((IUITextInput)this).SelectedTextRange = range;

        // todo _client.SetCommitText(text);
        if (KeyboardDevice.Instance is { })
        {
            /*_topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance,
                0, InputRoot, text));*/
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

    public UITextRange GetTextRange(UITextPosition fromPosition, UITextPosition toPosition)
    {
        if (fromPosition is AvaloniaTextPosition f && toPosition is AvaloniaTextPosition t)
        {
            // todo check calculation.
            var range = new NSRange(Math.Min(f.Index, t.Index), Math.Abs(t.Index - f.Index));
            return new AvaloniaTextRange(range);
        }

        return null;
        //throw new Exception();
    }

    UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, nint offset)
    {
        if (fromPosition is AvaloniaTextPosition indexedPosition)
        {
            var end = indexedPosition.Index + offset;
            // Verify position is valid in document.
            //if (end > self.text.length || end < 0) {
            //    return nil;
            //}

            return new AvaloniaTextPosition(end);
        }

        return null;
    }

    UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection, nint offset)
    {
        if (fromPosition is AvaloniaTextPosition f)
        {
            var newPosition = f.Index;

            switch (inDirection)
            {
                case UITextLayoutDirection.Left:
                    newPosition -= offset;
                    break;

                case UITextLayoutDirection.Right:
                    newPosition += offset;
                    break;
            }

            if (newPosition < 0)
            {
                newPosition = 0;
            }

            if (newPosition > _client.SurroundingText.Text.Length)
            {
                newPosition = _client.SurroundingText.Text.Length;
            }

            return new AvaloniaTextPosition(newPosition);
        }

        throw new Exception();
    }

    NSComparisonResult IUITextInput.ComparePosition(UITextPosition first, UITextPosition second)
    {
        if (first is AvaloniaTextPosition f && second is AvaloniaTextPosition s)
        {
            if (f.Index < s.Index)
                return NSComparisonResult.Ascending;

            if (f.Index > s.Index)
                return NSComparisonResult.Descending;

            return NSComparisonResult.Same;
        }

        throw new Exception();
    }

    nint IUITextInput.GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition)
    {
        if (fromPosition is AvaloniaTextPosition f && toPosition is AvaloniaTextPosition t)
        {
            return t.Index - f.Index;
        }

        throw new Exception();
    }

    UITextPosition IUITextInput.GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction)
    {
        if (range is AvaloniaTextRange indexedRange)
        {
            nint position = 0;
            
            switch (direction)
            {
                case UITextLayoutDirection.Up:
                case UITextLayoutDirection.Left:
                    position = indexedRange.Range.Location;
                    break;
                
                case UITextLayoutDirection.Down:
                case UITextLayoutDirection.Right:
                    position = indexedRange.Range.Location + indexedRange.Range.Length;
                    break;
            }

            return new AvaloniaTextPosition(position);
        }

        throw new Exception();
    }

    UITextRange IUITextInput.GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction)
    {
        if (byExtendingPosition is AvaloniaTextPosition pos)
        {
            NSRange result = new NSRange();
            
            switch (direction)
            {
                case UITextLayoutDirection.Up:
                case UITextLayoutDirection.Left:
                    result = new NSRange(pos.Index - 1, 1);
                    break;

                case UITextLayoutDirection.Right:
                case UITextLayoutDirection.Down:
                    result = new NSRange(pos.Index, 1);
                    break;
            }

            return new AvaloniaTextRange(result);
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
        return null;
    }

    UITextSelectionRect[] IUITextInput.GetSelectionRects(UITextRange range)
    {
        return null;
    }

    [Export("textStylingAtPosition:inDirection:")]
    public NSDictionary GetTextStylingAtPosition(UITextPosition position, UITextStorageDirection direction)
    {
        return null;
    }

    UITextRange? IUITextInput.SelectedTextRange
    {
        get
        {
            return new AvaloniaTextRange(new NSRange(
                (nint)Math.Min(_client.SurroundingText.CursorOffset, _client.SurroundingText.AnchorOffset),
                (nint)Math.Abs(_client.SurroundingText.CursorOffset - _client.SurroundingText.AnchorOffset)));
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


    public NSObject? WeakInputDelegate
    {
        get;
        set;
    }

    NSObject IUITextInput.WeakTokenizer => _tokenizer;

    UITextRange IUITextInput.MarkedTextRange
    {
        get
        {
            if (_client == null || string.IsNullOrWhiteSpace(_markedText))
            {
                return null;
            }

            // todo
            return new AvaloniaTextRange(new NSRange(
                (nint)Math.Min(_client.SurroundingText.CursorOffset, _client.SurroundingText.AnchorOffset),
                (nint)Math.Abs(_client.SurroundingText.CursorOffset - _client.SurroundingText.AnchorOffset)));            
        }
    }
}
