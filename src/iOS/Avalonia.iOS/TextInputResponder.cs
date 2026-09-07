using System;
using System.Runtime.InteropServices;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Input.TextInput;
using Avalonia.Logging;
using Avalonia.Media.TextFormatting;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
// ReSharper disable InconsistentNaming
// ReSharper disable StringLiteralTypo

namespace Avalonia.iOS;

partial class AvaloniaView
{

    [Adopts("UITextInput")]
    [Adopts("UITextInputTraits")]
    [Adopts("UIKeyInput")]
    partial class TextInputResponder : UIResponder, IUITextInput
    {
        private static AvaloniaEmptyTextPosition? _emptyPosition;
        private static AvaloniaEmptyTextPosition EmptyPosition => _emptyPosition ??= new();

        private class AvaloniaTextRange : UITextRange, INSCopying
        {
            private UITextPosition? _start;
            private UITextPosition? _end;
            public int StartIndex { get; }
            public int EndIndex { get; }

            public AvaloniaTextRange(int startIndex, int endIndex)
            {
                var a = Math.Max(0, startIndex);
                var b = Math.Max(0, endIndex);
                StartIndex = Math.Min(a, b);
                EndIndex = Math.Max(a, b);
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
                Index = Math.Max(0, index);
            }

            public int Index { get; }
            public NSObject Copy(NSZone? zone) => new AvaloniaTextPosition(Index);
        }

        private class AvaloniaEmptyTextPosition : UITextPosition, INSCopying
        {
            public AvaloniaEmptyTextPosition()
            {

            }
            public NSObject Copy(NSZone? zone) => this;
        }

        public TextInputResponder(AvaloniaView view, TextInputMethodClient client)
        {
            _view = view;
            NextResponder = view;
            _client = client;
            _structured = client as IStructuredTextInput;
            _tokenizer = new UITextInputStringTokenizer(this);
        }

        public override UIResponder NextResponder { get; }

        private readonly TextInputMethodClient _client;
        private int _inSurroundingTextUpdateEvent;
        private readonly UITextPosition _beginningOfDocument = new AvaloniaTextPosition(0);
        private readonly UITextInputStringTokenizer _tokenizer;
        private readonly NSString _textInputContextIdentifier = new NSString(Guid.NewGuid().ToString());
        private bool _isInUpdate;
        private NSWritingDirection _baseWritingDirection = NSWritingDirection.LeftToRight;

        public TextInputMethodClient? Client => _client;

        public override bool CanResignFirstResponder => true;

        public override bool CanBecomeFirstResponder => true;

        public override UIEditingInteractionConfiguration EditingInteractionConfiguration =>
            UIEditingInteractionConfiguration.Default;

        public override NSString TextInputContextIdentifier => _textInputContextIdentifier;

        public override UITextInputMode TextInputMode
        {
            get
            {
                var localeHints = _view._options?.LocaleHints;

                if (localeHints?.Count > 0)
                {
                    foreach (var locale in localeHints)
                    {
                        foreach (var inputMode in UITextInputMode.ActiveInputModes)
                        {
                            var lang = inputMode.PrimaryLanguage;
                            if (lang != null && (lang.Equals(locale, StringComparison.OrdinalIgnoreCase) || lang.StartsWith(locale + "-", StringComparison.OrdinalIgnoreCase)))
                            {
                                return inputMode;
                            }
                        }
                    }
                }

                UITextInputMode? mode = null;
#if !TVOS
#pragma warning disable CA1422
                mode = UITextInputMode.CurrentInputMode;
#pragma warning restore CA1422
#endif
                // Can be empty see https://developer.apple.com/documentation/uikit/uitextinputmode/1614522-activeinputmodes
                if (mode is null && UITextInputMode.ActiveInputModes.Length > 0)
                {
                    mode = UITextInputMode.ActiveInputModes[0];
                }
                // See: https://stackoverflow.com/a/33337483/20894223
                if (mode is null)
                {
                    using var tv = new UITextView();
                    mode = tv.TextInputMode;
                }
                return mode;
            }
        }

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
            if (WeakInputDelegate == null || _isInUpdate)
                return;
            _inSurroundingTextUpdateEvent++;
            try
            {
                objc_msgSend(WeakInputDelegate.Handle.Handle, TextWillChange, Handle.Handle);
                objc_msgSend(WeakInputDelegate.Handle.Handle, TextDidChange, Handle.Handle);
                objc_msgSend(WeakInputDelegate.Handle.Handle, SelectionWillChange, Handle.Handle);
                objc_msgSend(WeakInputDelegate.Handle.Handle, SelectionDidChange, Handle.Handle);
            }
            finally
            {
                _inSurroundingTextUpdateEvent--;
            }
        }

        private void ClientStateChanged(object? sender, EventArgs e) => SurroundingTextChanged(sender, e);

        private void KeyPress(Key key, PhysicalKey physicalKey, string? keySymbol)
        {
            _isInUpdate = true;
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "Triggering key press {key}", key);

            if (_view._topLevelImpl.Input is { } input)
            {
                input.Invoke(new RawKeyEventArgs(KeyboardDevice.Instance!, 0, _view.InputRoot,
                    RawKeyEventType.KeyDown, key, RawInputModifiers.None, physicalKey, keySymbol));

                input.Invoke(new RawKeyEventArgs(KeyboardDevice.Instance!, 0, _view.InputRoot,
                    RawKeyEventType.KeyUp, key, RawInputModifiers.None, physicalKey, keySymbol));
            }
            _isInUpdate = false;
        }

        private void TextInput(string text)
        {
            _isInUpdate = true;
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "Triggering text input {length}", text.Length);
            _view._topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance!, 0, _view.InputRoot, text));
            _isInUpdate = false;
        }

        void IUIKeyInput.InsertText(string text)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.InsertText {length}", text.Length);

            if (text == "\n")
            {
                KeyPress(Key.Enter, PhysicalKey.Enter, "\r");

                switch (ReturnKeyType)
                {
                    case UIReturnKeyType.Next:
                        FocusManager.GetFocusManager(_view._topLevel)?
                            .TryMoveFocus(NavigationDirection.Next);
                        break;
                    case UIReturnKeyType.Done:
                    case UIReturnKeyType.Go:
                    case UIReturnKeyType.Send:
                    case UIReturnKeyType.Search:
                        ResignFirstResponder();
                        break;
                }
                return;
            }

            if (_structured is { } structured)
            {
                StructuredInsertText(structured, text);
                return;
            }

            TextInput(text);
        }

        void IUIKeyInput.DeleteBackward() => KeyPress(Key.Back, PhysicalKey.Backspace, "\b");

        bool IUIKeyInput.HasText =>
            _structured is { } structured ? !structured.DocumentRange.IsEmpty :
            !string.IsNullOrEmpty(_client.SurroundingText) ||
            !string.IsNullOrEmpty(_markedText);

        string IUITextInput.TextInRange(UITextRange range)
        {
            if (_structured is { } structured)
            {
                return StructuredTextInRange(structured, range);
            }

            if (range is AvaloniaTextRange r)
            {
                var surroundingText = _client.SurroundingText;

                Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.TextInRange {start} {end}", r.StartIndex, r.EndIndex);

                string result = "";
                if (string.IsNullOrEmpty(_markedText))
                {
                    if (surroundingText != null && r.EndIndex <= surroundingText.Length)
                    {
                        result = surroundingText[r.StartIndex..r.EndIndex];
                    }
                }
                else
                {
                    var currentSelection = _client.Selection;
                    var span = new CombinedSpan3<char>(surroundingText.AsSpan(0, currentSelection.Start),
                        _markedText,
                        surroundingText.AsSpan(currentSelection.Start, currentSelection.End - currentSelection.Start));
                    var buf = new char[r.EndIndex - r.StartIndex];
                    span.CopyTo(buf, r.StartIndex);
                    result = new string(buf);
                }

                return result;
            }

            return null!;
        }

        void IUITextInput.ReplaceText(UITextRange range, string text)
        {
            if (_structured is { } structured)
            {
                StructuredReplaceText(structured, range, text);
                return;
            }

            if (range is AvaloniaTextRange r)
            {
                Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.ReplaceText {start} {end} {length}", r.StartIndex, r.EndIndex, text.Length);

                _client.Selection = new TextSelection(r.StartIndex, r.EndIndex);
                TextInput(text);
            }
        }

        void IUITextInput.SetMarkedText(string markedText, NSRange selectedRange)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUIKeyInput.SetMarkedText {start} {len} {length}", selectedRange.Location,
                    selectedRange.Length, markedText?.Length ?? 0);

            if (_structured is { } structured)
            {
                StructuredSetMarkedText(structured, markedText, selectedRange);
                return;
            }

            _markedText = markedText;
            _client.DeliverComposition(markedText, null);
        }

        void IUITextInput.UnmarkText()
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.UnmarkText");

            if (_structured is { } structured)
            {
                StructuredUnmarkText(structured);
                return;
            }

            if (_markedText == null)
                return;
            var commitString = _markedText;
            _markedText = null;
            // A structured client keeps the marked text in its document and commits it there in one
            // change; any other client gets it as text input.
            if (_client.CommitComposition(commitString))
                return;
            if (string.IsNullOrWhiteSpace(commitString))
                return;
            TextInput(commitString);
        }

        public UITextRange GetTextRange(UITextPosition fromPosition, UITextPosition toPosition)
        {
            if (_structured is { } structured)
            {
                return StructuredGetTextRange(structured, fromPosition, toPosition);
            }

            if (fromPosition is AvaloniaTextPosition f && toPosition is AvaloniaTextPosition t)
            {
                Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.GetTextRange {start} {end}", f.Index, t.Index);
                return new AvaloniaTextRange(f.Index, t.Index);
            }

            return null!;
        }

        UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, nint offset)
        {
            if (_structured is { } structured)
            {
                return StructuredGetPosition(structured, fromPosition, offset);
            }

            if (fromPosition is AvaloniaTextPosition pos)
            {
                Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.GetPosition {start} {offset}", pos.Index, (int)offset);

                var res = GetPositionCore(pos, offset);

                if (res is not null)
                {
                    Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "res: {position}", res.Index);
                    return res;
                }

                return null!;
            }

            return null!;
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

        UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection, nint offset)
        {
            if (_structured is { } structured)
            {
                return StructuredGetPosition(structured, fromPosition, inDirection, offset);
            }

            if (fromPosition is AvaloniaTextPosition pos)
            {
                Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.GetPosition {start} {direction} {offset}", pos.Index, inDirection, (int)offset);

                var res = GetPositionCore(pos, inDirection, offset);

                if (res is not null)
                {
                    Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "res: {position}", res.Index);
                    return res;
                }

                return null!;
            }

            return null!;
        }

        private AvaloniaTextPosition? GetPositionCore(AvaloniaTextPosition fromPosition, UITextLayoutDirection inDirection, nint offset)
        {
            var newPosition = fromPosition.Index;

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
                return null;

            if (newPosition > DocumentLength)
                return null;

            return new AvaloniaTextPosition(newPosition);
        }

        NSComparisonResult IUITextInput.ComparePosition(UITextPosition first, UITextPosition second)
        {
            if (_structured is not null)
            {
                return StructuredComparePosition(first, second);
            }

            if (first is AvaloniaTextPosition f && second is AvaloniaTextPosition s)
            {
                if (f.Index < s.Index)
                    return NSComparisonResult.Ascending;

                if (f.Index > s.Index)
                    return NSComparisonResult.Descending;

                return NSComparisonResult.Same;
            }

            return default;
        }

        nint IUITextInput.GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition)
        {
            if (_structured is { } structured)
            {
                return StructuredGetOffset(structured, fromPosition, toPosition);
            }

            if (fromPosition is AvaloniaTextPosition f && toPosition is AvaloniaTextPosition t)
            {
                return t.Index - f.Index;
            }

            return default;
        }

        UITextPosition IUITextInput.GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction)
        {
            if (_structured is not null)
            {
                return StructuredGetPositionWithinRange(range, direction);
            }

            if (range is AvaloniaTextRange r)
            {
                if (direction is UITextLayoutDirection.Right or UITextLayoutDirection.Down)
                    return r.End;
                return r.Start;
            }

            return null!;
        }

        UITextRange IUITextInput.GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction)
        {
            if (_structured is { } structured)
            {
                return StructuredGetCharacterRange(structured, byExtendingPosition, direction);
            }

            if (byExtendingPosition is AvaloniaTextPosition p)
            {
                if (direction is UITextLayoutDirection.Left or UITextLayoutDirection.Up)
                    return new AvaloniaTextRange(0, p.Index);

                return new AvaloniaTextRange(p.Index, DocumentLength);
            }

            return null!;
        }

        NSWritingDirection IUITextInput.GetBaseWritingDirection(UITextPosition forPosition,
            UITextStorageDirection direction)
        {
            return _baseWritingDirection;
        }

        void IUITextInput.SetBaseWritingDirectionforRange(NSWritingDirection writingDirection, UITextRange range)
        {
            _baseWritingDirection = writingDirection;
        }

        CGRect IUITextInput.GetFirstRectForRange(UITextRange range)
        {

            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetFirstRectForRange");
            if (_structured is { } structured)
            {
                return AsRange(range) is { } textRange
                    ? ToCGRect(structured.GetFirstRectForRange(textRange))
                    : CGRect.Null;
            }

            if (range is AvaloniaTextRange fallbackRange && _client.TextViewVisual is TextPresenter fallbackPresenter)
            {
                foreach (var rect in fallbackPresenter.TextLayout.HitTestTextRange(
                             fallbackRange.StartIndex,
                             Math.Max(0, fallbackRange.EndIndex - fallbackRange.StartIndex)))
                {
                    return new CGRect(rect.X, rect.Y, rect.Width, rect.Height);
                }
            }

            var r = _view._cursorRect;

            return new CGRect(r.Left, r.Top, r.Width, r.Height);
        }

        CGRect IUITextInput.GetCaretRectForPosition(UITextPosition? position)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetCaretRectForPosition");

            if (_structured is { } structured)
            {
                return AsPointer(position) is { } pointer
                    ? ToCGRect(structured.GetCaretRect(pointer))
                    : CGRect.Null;
            }

            if (position is AvaloniaTextPosition fallbackPosition && _client.TextViewVisual is TextPresenter fallbackPresenter)
            {
                var clamped = Math.Clamp(fallbackPosition.Index, 0, DocumentLength);
                var fallbackRect = fallbackPresenter.TextLayout.HitTestTextPosition(clamped);
                return new CGRect(fallbackRect.X, fallbackRect.Y, fallbackRect.Width, fallbackRect.Height);
            }

            var rect = _client.CursorRectangle;

            return new CGRect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        UITextPosition IUITextInput.GetClosestPositionToPoint(CGPoint point)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetClosestPositionToPoint");

            if (_structured is { } structured)
            {
                return structured.GetClosestPosition(new Point(point.X, point.Y)) is { } closest
                    ? new PointerPosition(closest)
                    : null!;
            }

            var presenter = _client.TextViewVisual as TextPresenter;

            if (presenter is { })
            {
                var hitResult = presenter.TextLayout.HitTestPoint(new Point(point.X, point.Y));

                return new AvaloniaTextPosition(hitResult.TextPosition);
            }

            return EmptyPosition;
        }

        UITextPosition IUITextInput.GetClosestPositionToPoint(CGPoint point, UITextRange withinRange)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetClosestPositionToPoint");

            if (_structured is { } structured)
            {
                return AsRange(withinRange) is { } range &&
                    structured.GetClosestPosition(new Point(point.X, point.Y), range) is { } closest
                        ? new PointerPosition(closest)
                        : null!;
            }

            if (withinRange is AvaloniaTextRange fallbackRange)
            {
                var closest = ((IUITextInput)this).GetClosestPositionToPoint(point);
                if (closest is AvaloniaTextPosition fallbackPosition)
                {
                    var clamped = Math.Clamp(fallbackPosition.Index, fallbackRange.StartIndex, fallbackRange.EndIndex);
                    return new AvaloniaTextPosition(clamped);
                }
            }

            return ((IUITextInput)this).GetClosestPositionToPoint(point);
        }

        UITextRange IUITextInput.GetCharacterRangeAtPoint(CGPoint point)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetCharacterRangeAtPoint");

            // nil when no character is under the point.
            if (_structured is { } structured)
            {
                return structured.GetCharacterRangeAtPoint(new Point(point.X, point.Y)) is { } range
                    ? new PointerRange(range)
                    : null!;
            }

            var closest = ((IUITextInput)this).GetClosestPositionToPoint(point);
            if (closest is AvaloniaTextPosition fallbackPosition)
            {
                var start = Math.Clamp(fallbackPosition.Index, 0, DocumentLength);
                var end = Math.Min(DocumentLength, start + 1);
                return new AvaloniaTextRange(start, end);
            }

            return new AvaloniaTextRange(0, 0);
        }

        UITextSelectionRect[] IUITextInput.GetSelectionRects(UITextRange range)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUITextInput:GetSelectionRect");

            if (_structured is { } structured)
            {
                return StructuredGetSelectionRects(structured, range);
            }

            if (range is AvaloniaTextRange fallbackRange && _client.TextViewVisual is TextPresenter fallbackPresenter)
            {
                var hitRects = fallbackPresenter.TextLayout.HitTestTextRange(
                    fallbackRange.StartIndex,
                    Math.Max(0, fallbackRange.EndIndex - fallbackRange.StartIndex));

                var selectionRects = new System.Collections.Generic.List<UITextSelectionRect>();
                foreach (var rect in hitRects)
                {
                    selectionRects.Add(new AvaloniaSelectionRect(rect, false, false));
                }

                return selectionRects.ToArray();
            }

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
                if (_structured is { } structured)
                {
                    return StructuredSelectedTextRange(structured);
                }

                return new AvaloniaTextRange(_client.Selection.Start, _client.Selection.End);
            }
            set
            {
                if (_inSurroundingTextUpdateEvent > 0)
                    return;

                if (_structured is { } structured)
                {
                    StructuredSetSelectedTextRange(structured, value);
                    return;
                }

                if (value is AvaloniaTextRange r)
                {
                    _client.Selection = new TextSelection(r.StartIndex, r.EndIndex);
                }
                else
                {
                    _client.Selection = default;
                }
            }
        }

        NSDictionary? IUITextInput.MarkedTextStyle
        {
            get => null;
            set { }
        }

        UITextPosition IUITextInput.BeginningOfDocument => _structured is { } structured
            ? new PointerPosition(Scope(structured).Range.Start)
            : _beginningOfDocument;

        private int DocumentLength => (_client.SurroundingText?.Length ?? 0) + (_markedText?.Length ?? 0);

        UITextPosition IUITextInput.EndOfDocument => _structured is { } structured
            ? new PointerPosition(Scope(structured).Range.End)
            : new AvaloniaTextPosition(DocumentLength);

        UITextRange IUITextInput.MarkedTextRange
        {
            get
            {
                if (_structured is { } structured)
                {
                    return structured.CompositionRange is { } range ? new PointerRange(range) : null!;
                }

                if (string.IsNullOrWhiteSpace(_markedText))
                    return null!;
                return new AvaloniaTextRange(_client.Selection.Start, _client.Selection.Start + _markedText.Length);
            }
        }

        private sealed class AvaloniaSelectionRect : UITextSelectionRect
        {
            private readonly CGRect _rect;
            private readonly bool _containsStart;
            private readonly bool _containsEnd;

            public AvaloniaSelectionRect(Rect rect, bool containsStart, bool containsEnd)
            {
                _rect = new CGRect(rect.X, rect.Y, rect.Width, rect.Height);
                _containsStart = containsStart;
                _containsEnd = containsEnd;
            }

            public override CGRect Rect => _rect;

            public override bool ContainsStart => _containsStart;

            public override bool ContainsEnd => _containsEnd;

            public override bool IsVertical => false;
        }

        public override bool BecomeFirstResponder()
        {
            var res = base.BecomeFirstResponder();
            if (res)
            {
                Logger.TryGet(LogEventLevel.Debug, "IOSIME")
                    ?.Log(null, "Became first responder");

                if (_structured is { } structured)
                {
                    SubscribeStructured(structured);
                }
                else
                {
                    _client.SurroundingTextChanged += ClientStateChanged;
                    _client.SelectionChanged += ClientStateChanged;
                }

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

                if (_structured is { } structured)
                {
                    UnsubscribeStructured(structured);
                }
                else
                {
                    _client.SurroundingTextChanged -= ClientStateChanged;
                    _client.SelectionChanged -= ClientStateChanged;
                }

                CurrentAvaloniaResponder = null;
            }

            return res;
        }
    }
}
