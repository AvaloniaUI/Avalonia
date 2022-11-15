using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls.Presenters;
using Foundation;
using ObjCRuntime;
using Avalonia.Input.TextInput;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Logging;
using CoreGraphics;
using UIKit;
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

namespace Avalonia.iOS;

#nullable enable

partial class AvaloniaView
{

    [Adopts("UITextInput")]
    [Adopts("UITextInputTraits")]
    [Adopts("UIKeyInput")]
    partial class TextInputResponder : UIResponder, IUITextInput
    {
        private class AvaloniaTextRange : UITextRange, INSCopying
        {
            private UITextPosition? _start;
            private UITextPosition? _end;
            public int StartIndex { get; }
            public int EndIndex { get; }

            public AvaloniaTextRange(int startIndex, int endIndex)
            {
                if (startIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(startIndex));

                if (endIndex < startIndex)
                    throw new ArgumentOutOfRangeException(nameof(endIndex));

                StartIndex = startIndex;
                EndIndex = endIndex;
            }

            public override bool IsEmpty => StartIndex == EndIndex;

            public override UITextPosition Start => _start ??= new AvaloniaTextPosition(StartIndex);
            public override UITextPosition End => _end ??= new AvaloniaTextPosition(EndIndex);
            public NSObject Copy(NSZone? zone)
            {
                return new AvaloniaTextRange(StartIndex, EndIndex);
            }
        }

        private class AvaloniaTextPosition : UITextPosition, INSCopying
        {
            public AvaloniaTextPosition(int index)
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));
                Index = index;
            }

            public int Index { get; }
            public NSObject Copy(NSZone? zone) => new AvaloniaTextPosition(Index);
        }

        public TextInputResponder(AvaloniaView view, ITextInputMethodClient client)
        {
            _view = view;
            NextResponder = view;
            _client = client;
            _tokenizer = new UITextInputStringTokenizer(this);
        }

        public override UIResponder NextResponder { get; }

        private readonly ITextInputMethodClient _client;
        private int _inSurroundingTextUpdateEvent;
        private readonly UITextPosition _beginningOfDocument = new AvaloniaTextPosition(0);
        private readonly UITextInputStringTokenizer _tokenizer;

        public ITextInputMethodClient? Client => _client;

        public override bool CanResignFirstResponder => true;

        public override bool CanBecomeFirstResponder => true;

        public override UIEditingInteractionConfiguration EditingInteractionConfiguration =>
            UIEditingInteractionConfiguration.Default;

        public override NSString TextInputContextIdentifier => new NSString(Guid.NewGuid().ToString());

        public override UITextInputMode TextInputMode => UITextInputMode.CurrentInputMode;

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern void objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

        private static readonly IntPtr SelectionWillChange = Selector.GetHandle("selectionWillChange:");
        private static readonly IntPtr SelectionDidChange = Selector.GetHandle("selectionDidChange:");
        private static readonly IntPtr TextWillChange = Selector.GetHandle("textWillChange:");
        private static readonly IntPtr TextDidChange = Selector.GetHandle("textDidChange:");
        private readonly AvaloniaView _view;
        private string? _markedText;

        
        
        private void SurroundingTextChanged(object? sender, EventArgs e)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "SurroundingTextChanged");
            if (WeakInputDelegate == null)
                return;
            _inSurroundingTextUpdateEvent++;
            try
            {
                objc_msgSend(WeakInputDelegate.Handle.Handle, TextWillChange, Handle.Handle);
                objc_msgSend(WeakInputDelegate.Handle.Handle, TextDidChange, Handle.Handle);
                objc_msgSend(WeakInputDelegate.Handle.Handle, SelectionWillChange, this.Handle.Handle);
                objc_msgSend(WeakInputDelegate.Handle.Handle, SelectionDidChange, this.Handle.Handle);
            }
            finally
            {
                _inSurroundingTextUpdateEvent--;
            }
        }

        private void KeyPress(Key ev)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "Triggering key press {key}", ev);
            _view._topLevelImpl.Input(new RawKeyEventArgs(KeyboardDevice.Instance!, 0, _view.InputRoot,
                RawKeyEventType.KeyDown, ev, RawInputModifiers.None));

            _view._topLevelImpl.Input(new RawKeyEventArgs(KeyboardDevice.Instance!, 0, _view.InputRoot,
                RawKeyEventType.KeyUp, ev, RawInputModifiers.None));
        }

        private void TextInput(string text)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "Triggering text input {text}", text);
            _view._topLevelImpl.Input(new RawTextInputEventArgs(KeyboardDevice.Instance!, 0, _view.InputRoot, text));
        }

        void IUIKeyInput.InsertText(string text)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.InsertText {text}", text);

            if (text == "\n")
            {
                KeyPress(Key.Enter);

                switch (ReturnKeyType)
                {
                    case UIReturnKeyType.Done:
                        case UIReturnKeyType.Go:
                        case UIReturnKeyType.Send:
                        case UIReturnKeyType.Search:
                        ResignFirstResponder();
                        break;
                }
                return;
            }

            TextInput(text);
        }
        
        void IUIKeyInput.DeleteBackward() => KeyPress(Key.Back);

        bool IUIKeyInput.HasText => true;

        string IUITextInput.TextInRange(UITextRange range)
        {
            var r = (AvaloniaTextRange)range;
            var s = _client.SurroundingText;
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.TextInRange {start} {end}", r.StartIndex, r.EndIndex);

            string result = "";
            if(string.IsNullOrEmpty(_markedText))
                result = s.Text[r.StartIndex .. r.EndIndex];
            else
            {
                var span = new CombinedSpan3<char>(s.Text.AsSpan().Slice(0, s.CursorOffset),
                    _markedText,
                    s.Text.AsSpan().Slice(s.CursorOffset));
                var buf = new char[r.EndIndex - r.StartIndex];
                span.CopyTo(buf, r.StartIndex);
                result = new string(buf);
            }
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "result: {res}", result);

            return result;
        }

        void IUITextInput.ReplaceText(UITextRange range, string text)
        {
            var r = (AvaloniaTextRange)range;
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUIKeyInput.ReplaceText {start} {end} {text}", r.StartIndex, r.EndIndex, text);
            _client.SelectInSurroundingText(r.StartIndex, r.EndIndex);
            TextInput(text);
        }

        void IUITextInput.SetMarkedText(string markedText, NSRange selectedRange)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUIKeyInput.SetMarkedText {start} {len} {text}", selectedRange.Location,
                    selectedRange.Location, markedText);

            _markedText = markedText;
            _client.SetPreeditText(markedText);
        }

        void IUITextInput.UnmarkText()
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.UnmarkText");
            if(_markedText == null)
                return;
            var commitString = _markedText;
            _markedText = null;
            _client.SetPreeditText(null);
            if (string.IsNullOrWhiteSpace(commitString))
                return;
            TextInput(commitString);
        }

        public UITextRange GetTextRange(UITextPosition fromPosition, UITextPosition toPosition)
        {
            var f = (AvaloniaTextPosition)fromPosition;
            var t = (AvaloniaTextPosition)toPosition;
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.GetTextRange {start} {end}", f.Index, t.Index);

            return new AvaloniaTextRange(f.Index, t.Index);
        }

        UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, nint offset)
        {
            var pos = (AvaloniaTextPosition)fromPosition;
            Logger.TryGet(LogEventLevel.Debug, ImeLog)
                ?.Log(null, "IUIKeyInput.GetPosition {start} {offset}", pos.Index, (int)offset);

             var res = GetPositionCore(pos, offset);
             Logger.TryGet(LogEventLevel.Debug, ImeLog)
                 ?.Log(null, $"res: " + (res == null ? "null" : (int)res.Index));
             return res!;
        }

        private AvaloniaTextPosition? GetPositionCore(AvaloniaTextPosition pos, nint offset)
        {
            
            var end = pos.Index + (int)offset;
            if (end < 0)
                return null!;
            if (end > DocumentLength)
                return null;
            return new AvaloniaTextPosition(end);
        }

        UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection,
            nint offset)
        {
            var pos = (AvaloniaTextPosition)fromPosition;
            Logger.TryGet(LogEventLevel.Debug, ImeLog)
                ?.Log(null, "IUIKeyInput.GetPosition {start} {direction} {offset}", pos.Index,  inDirection, (int)offset);

            var res = GetPositionCore(pos, inDirection, offset);
            Logger.TryGet(LogEventLevel.Debug, ImeLog)
                ?.Log(null, $"res: " + (res == null ? "null" : (int)res.Index));
            return res!;
        }
        
        private AvaloniaTextPosition? GetPositionCore(AvaloniaTextPosition fromPosition, UITextLayoutDirection inDirection,
            nint offset)
        {
            var f = (AvaloniaTextPosition)fromPosition;
            var newPosition = f.Index;

            switch (inDirection)
            {
                case UITextLayoutDirection.Left:
                    newPosition -= (int)offset;
                    break;

                case UITextLayoutDirection.Right:
                    newPosition += (int)offset;
                    break;
            }

            if (newPosition < 0)
                return null!;

            if (newPosition > DocumentLength)
                return null!;

            return new AvaloniaTextPosition(newPosition);
        }

        NSComparisonResult IUITextInput.ComparePosition(UITextPosition first, UITextPosition second)
        {
            var f = (AvaloniaTextPosition)first;
            var s = (AvaloniaTextPosition)second;
            if (f.Index < s.Index)
                return NSComparisonResult.Ascending;

            if (f.Index > s.Index)
                return NSComparisonResult.Descending;

            return NSComparisonResult.Same;
        }

        nint IUITextInput.GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition)
        {
            var f = (AvaloniaTextPosition)fromPosition;
            var t = (AvaloniaTextPosition)toPosition;
            return t.Index - f.Index;
        }

        UITextPosition IUITextInput.GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction)
        {
            var r = (AvaloniaTextRange)range;

            if (direction is UITextLayoutDirection.Right or UITextLayoutDirection.Down)
                return r.End;
            return r.Start;
        }

        UITextRange IUITextInput.GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction)
        {
            var p = (AvaloniaTextPosition)byExtendingPosition;
            if (direction is UITextLayoutDirection.Left or UITextLayoutDirection.Up)
                return new AvaloniaTextRange(0, p.Index);

            return new AvaloniaTextRange(p.Index, DocumentLength);
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
            
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetFirstRectForRange");
            // TODO: Query from the input client
            var r = _view._cursorRect;

            return new CGRect(r.Left, r.Top, r.Width, r.Height);
        }

        CGRect IUITextInput.GetCaretRectForPosition(UITextPosition? position)
        {
            // TODO: Query from the input client
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetCaretRectForPosition");
            var rect = _client.CursorRectangle;

            return new CGRect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        UITextPosition IUITextInput.GetClosestPositionToPoint(CGPoint point)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetClosestPositionToPoint");

            var presenter = _client.TextViewVisual as TextPresenter;

            if (presenter is { })
            {
                var hitResult = presenter.TextLayout.HitTestPoint(new Point(point.X, point.Y));
                
                return new AvaloniaTextPosition(hitResult.TextPosition);
            }

            return null;
        }

        UITextPosition IUITextInput.GetClosestPositionToPoint(CGPoint point, UITextRange withinRange)
        {
            // TODO: Query from the input client
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetClosestPositionToPoint");
            return new AvaloniaTextPosition(0);
        }

        UITextRange IUITextInput.GetCharacterRangeAtPoint(CGPoint point)
        {
            // TODO: Query from the input client
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetCharacterRangeAtPoint");
            return new AvaloniaTextRange(0, 0);
        }

        UITextSelectionRect[] IUITextInput.GetSelectionRects(UITextRange range)
        {
            // TODO: Query from the input client
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetSelectionRect");
            return Array.Empty<UITextSelectionRect>();
        }

        [Export("textStylingAtPosition:inDirection:")]
        public NSDictionary GetTextStylingAtPosition(UITextPosition position, UITextStorageDirection direction)
        {
            return null!;
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
                if (_inSurroundingTextUpdateEvent > 0)
                    return;
                if (value == null)
                    _client.SelectInSurroundingText(_client.SurroundingText.CursorOffset,
                        _client.SurroundingText.CursorOffset);
                else
                {
                    var r = (AvaloniaTextRange)value;
                    _client.SelectInSurroundingText(r.StartIndex, r.EndIndex);
                }
            }
        }

        NSDictionary? IUITextInput.MarkedTextStyle
        {
            get => null;
            set {}
        }

        UITextPosition IUITextInput.BeginningOfDocument => _beginningOfDocument;

        private int DocumentLength => _client.SurroundingText.Text.Length + (_markedText?.Length ?? 0);
        UITextPosition IUITextInput.EndOfDocument => new AvaloniaTextPosition(DocumentLength);

        UITextRange IUITextInput.MarkedTextRange
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_markedText))
                    return null!;
                return new AvaloniaTextRange(_client.SurroundingText.CursorOffset, _client.SurroundingText.CursorOffset + _markedText.Length);
            }
        }

        public override bool BecomeFirstResponder()
        {
            var res = base.BecomeFirstResponder();
            if (res)
            {
                Logger.TryGet(LogEventLevel.Debug, "IOSIME")
                    ?.Log(null, "Became first responder");
                _client.SurroundingTextChanged += SurroundingTextChanged;
                CurrentAvaloniaResponder = this;
            }

            return res;
        }


        public override bool ResignFirstResponder()
        {
            var res = base.ResignFirstResponder();
            if (res && ReferenceEquals(CurrentAvaloniaResponder, this))
            {
                
                Logger.TryGet(LogEventLevel.Debug, "IOSIME")
                    ?.Log(null, "Resigned first responder");
                _client.SurroundingTextChanged -= SurroundingTextChanged;
                CurrentAvaloniaResponder = null;
            }

            return res;
        }
    }
}
