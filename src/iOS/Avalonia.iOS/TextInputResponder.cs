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
            _tokenizer = new UITextInputStringTokenizer(this);
        }

        public override UIResponder NextResponder { get; }

        private readonly TextInputMethodClient _client;
        private int _inSurroundingTextUpdateEvent;
        private readonly UITextPosition _beginningOfDocument = new AvaloniaTextPosition(0);
        private readonly UITextInputStringTokenizer _tokenizer;
        private bool _isInUpdate;
        private NSWritingDirection _baseWritingDirection = NSWritingDirection.LeftToRight;

        public TextInputMethodClient? Client => _client;

        public override bool CanResignFirstResponder => true;

        public override bool CanBecomeFirstResponder => true;

        public override UIEditingInteractionConfiguration EditingInteractionConfiguration =>
            UIEditingInteractionConfiguration.Default;

        public override NSString TextInputContextIdentifier => new NSString(Guid.NewGuid().ToString());

        public override UITextInputMode TextInputMode
        {
            get
            {
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

        private IStructuredTextInput? StructuredClient => _client as IStructuredTextInput;

        private static ITextRange CreateRange(IStructuredTextInput structured, int start, int end)
        {
            var normalizedStart = Math.Min(start, end);
            var normalizedEnd = Math.Max(start, end);
            var startPointer = structured.CreatePointer(normalizedStart, LogicalDirection.Forward);
            var endPointer = structured.CreatePointer(normalizedEnd, LogicalDirection.Backward);
            return structured.CreateRange(startPointer, endPointer);
        }

        private static AvaloniaTextRange ToAvaloniaRange(ITextRange range)
            => new AvaloniaTextRange(range.Start.Offset, range.End.Offset);

        private static CGRect ToCGRect(Rect rect)
            => new CGRect(rect.X, rect.Y, rect.Width, rect.Height);



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
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "Triggering text input {text}", text);
            _view._topLevelImpl.Input?.Invoke(new RawTextInputEventArgs(KeyboardDevice.Instance!, 0, _view.InputRoot, text));
            _isInUpdate = false;
        }

        void IUIKeyInput.InsertText(string text)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.InsertText {text}", text);

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

            if (StructuredClient is { } structured)
            {
                structured.ReplaceText(structured.Selection, text);
                structured.CommitComposition();
                return;
            }

            TextInput(text);
        }

        void IUIKeyInput.DeleteBackward() => KeyPress(Key.Back, PhysicalKey.Backspace, "\b");

        bool IUIKeyInput.HasText =>
            StructuredClient?.DocumentEnd.Offset > 0 ||
            !string.IsNullOrEmpty(_client.SurroundingText) ||
            !string.IsNullOrEmpty(_markedText);

        string IUITextInput.TextInRange(UITextRange range)
        {
            if (range is AvaloniaTextRange r)
            {
                if (StructuredClient is { } structured)
                {
                    return structured.GetText(CreateRange(structured, r.StartIndex, r.EndIndex));
                }

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
                Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "result: {res}", result);

                return result;
            }

            return null!;
        }

        void IUITextInput.ReplaceText(UITextRange range, string text)
        {
            if (range is AvaloniaTextRange r)
            {
                Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.ReplaceText {start} {end} {text}", r.StartIndex, r.EndIndex, text);

                if (StructuredClient is { } structured)
                {
                    structured.ReplaceText(CreateRange(structured, r.StartIndex, r.EndIndex), text);
                    return;
                }

                _client.Selection = new TextSelection(r.StartIndex, r.EndIndex);
                TextInput(text);
            }
        }

        void IUITextInput.SetMarkedText(string markedText, NSRange selectedRange)
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?
                .Log(null, "IUIKeyInput.SetMarkedText {start} {len} {text}", selectedRange.Location,
                    selectedRange.Location, markedText);

            if (StructuredClient is { } structured)
            {
                structured.SetCompositionText(markedText, (int)selectedRange.Location);
                return;
            }

            _markedText = markedText;
            _client.SetPreeditText(markedText);
        }

        void IUITextInput.UnmarkText()
        {
            Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.UnmarkText");

            if (StructuredClient is { } structured)
            {
                structured.CommitComposition();
                return;
            }

            if (_markedText == null)
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
            if (fromPosition is AvaloniaTextPosition f && toPosition is AvaloniaTextPosition t)
            {
                Logger.TryGet(LogEventLevel.Debug, ImeLog)?.Log(null, "IUIKeyInput.GetTextRange {start} {end}", f.Index, t.Index);
                return new AvaloniaTextRange(f.Index, t.Index);
            }

            return null!;
        }

        UITextPosition IUITextInput.GetPosition(UITextPosition fromPosition, nint offset)
        {
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
            if (StructuredClient is { } structured)
            {
                var steps = Math.Abs((int)offset);
                if (steps == 0)
                {
                    return new AvaloniaTextPosition(fromPosition.Index);
                }

                var baseDirection = inDirection is UITextLayoutDirection.Right or UITextLayoutDirection.Down
                    ? LogicalDirection.Forward
                    : LogicalDirection.Backward;

                var direction = offset >= 0
                    ? baseDirection
                    : baseDirection == LogicalDirection.Forward
                        ? LogicalDirection.Backward
                        : LogicalDirection.Forward;

                var granularity = inDirection is UITextLayoutDirection.Left or UITextLayoutDirection.Right
                    ? TextGranularity.Character
                    : TextGranularity.Line;

                var pointer = structured.CreatePointer(fromPosition.Index, direction);
                for (var i = 0; i < steps; i++)
                {
                    var next = structured.GetBoundaryPosition(pointer, granularity, direction);
                    if (next is null)
                    {
                        return null;
                    }

                    pointer = next;
                }

                return new AvaloniaTextPosition(pointer.Offset);
            }

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
            if (fromPosition is AvaloniaTextPosition f && toPosition is AvaloniaTextPosition t)
            {
                return t.Index - f.Index;
            }

            return default;
        }

        UITextPosition IUITextInput.GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction)
        {
            if (range is AvaloniaTextRange r)
            {
                if (StructuredClient is { } structured)
                {
                    var structuredRange = CreateRange(structured, r.StartIndex, r.EndIndex);
                    var pointer = direction is UITextLayoutDirection.Right or UITextLayoutDirection.Down
                        ? structuredRange.End
                        : structuredRange.Start;
                    return new AvaloniaTextPosition(pointer.Offset);
                }

                if (direction is UITextLayoutDirection.Right or UITextLayoutDirection.Down)
                    return r.End;
                return r.Start;
            }

            return null!;
        }

        UITextRange IUITextInput.GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction)
        {
            if (byExtendingPosition is AvaloniaTextPosition p)
            {
                if (StructuredClient is { } structured)
                {
                    var position = structured.CreatePointer(p.Index, LogicalDirection.Forward);
                    var boundaryDirection = direction is UITextLayoutDirection.Left or UITextLayoutDirection.Up
                        ? LogicalDirection.Backward
                        : LogicalDirection.Forward;
                    var boundary = structured.GetBoundaryPosition(position, TextGranularity.Character, boundaryDirection);

                    if (boundary is null)
                    {
                        return new AvaloniaTextRange(p.Index, p.Index);
                    }

                    var textRange = boundaryDirection == LogicalDirection.Backward
                        ? structured.CreateRange(boundary, position)
                        : structured.CreateRange(position, boundary);

                    return ToAvaloniaRange(textRange);
                }

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
            if (range is AvaloniaTextRange textRange && StructuredClient is { } structured)
            {
                var rect = structured.GetFirstRectForRange(CreateRange(structured, textRange.StartIndex, textRange.EndIndex));
                return ToCGRect(rect);
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

            if (position is AvaloniaTextPosition p && StructuredClient is { } structured)
            {
                var pointer = structured.CreatePointer(p.Index, LogicalDirection.Forward);
                return ToCGRect(structured.GetCaretRect(pointer));
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

            if (StructuredClient is { } structured)
            {
                var closest = structured.GetClosestPosition(new Point(point.X, point.Y));
                return closest is null ? EmptyPosition : new AvaloniaTextPosition(closest.Offset);
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

            if (withinRange is AvaloniaTextRange r && StructuredClient is { } structured)
            {
                var closest = structured.GetClosestPosition(new Point(point.X, point.Y),
                    CreateRange(structured, r.StartIndex, r.EndIndex));
                return closest is null ? EmptyPosition : new AvaloniaTextPosition(closest.Offset);
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

            if (StructuredClient is { } structured)
            {
                var range = structured.GetCharacterRangeAtPoint(new Point(point.X, point.Y));
                return range is null ? new AvaloniaTextRange(0, 0) : ToAvaloniaRange(range);
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

            if (range is AvaloniaTextRange textRange && StructuredClient is { } structured)
            {
                var rects = structured.GetSelectionRects(CreateRange(structured, textRange.StartIndex, textRange.EndIndex));
                var result = new UITextSelectionRect[rects.Length];
                for (var i = 0; i < rects.Length; i++)
                {
                    result[i] = new AvaloniaSelectionRect(rects[i]);
                }

                return result;
            }

            if (range is AvaloniaTextRange fallbackRange && _client.TextViewVisual is TextPresenter fallbackPresenter)
            {
                var hitRects = fallbackPresenter.TextLayout.HitTestTextRange(
                    fallbackRange.StartIndex,
                    Math.Max(0, fallbackRange.EndIndex - fallbackRange.StartIndex));

                var selectionRects = new System.Collections.Generic.List<UITextSelectionRect>();
                foreach (var rect in hitRects)
                {
                    selectionRects.Add(new AvaloniaSelectionRect(rect));
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
                if (StructuredClient is { } structured)
                {
                    return ToAvaloniaRange(structured.Selection);
                }

                return new AvaloniaTextRange(_client.Selection.Start, _client.Selection.End);
            }
            set
            {
                if (_inSurroundingTextUpdateEvent > 0)
                    return;

                if (value is AvaloniaTextRange r)
                {
                    if (StructuredClient is { } structured)
                    {
                        structured.Selection = CreateRange(structured, r.StartIndex, r.EndIndex);
                        return;
                    }

                    _client.Selection = new TextSelection(r.StartIndex, r.EndIndex);
                }
                else
                {
                    if (StructuredClient is { } structured)
                    {
                        structured.Selection = CreateRange(structured, 0, 0);
                        return;
                    }

                    _client.Selection = default;
                }
            }
        }

        NSDictionary? IUITextInput.MarkedTextStyle
        {
            get => null;
            set { }
        }

        UITextPosition IUITextInput.BeginningOfDocument => _beginningOfDocument;

        private int DocumentLength => StructuredClient?.DocumentEnd.Offset ?? ((_client.SurroundingText?.Length ?? 0) + (_markedText?.Length ?? 0));
        UITextPosition IUITextInput.EndOfDocument => new AvaloniaTextPosition(DocumentLength);

        UITextRange IUITextInput.MarkedTextRange
        {
            get
            {
                if (StructuredClient is { } structured)
                {
                    return structured.CompositionRange is { } range ? ToAvaloniaRange(range) : null!;
                }

                if (string.IsNullOrWhiteSpace(_markedText))
                    return null!;
                return new AvaloniaTextRange(_client.Selection.Start, _client.Selection.Start + _markedText.Length);
            }
        }

        private sealed class AvaloniaSelectionRect : UITextSelectionRect
        {
            private readonly CGRect _rect;

            public AvaloniaSelectionRect(Rect rect)
            {
                _rect = new CGRect(rect.X, rect.Y, rect.Width, rect.Height);
            }

            public override CGRect Rect => _rect;

            public override bool ContainsStart => false;

            public override bool ContainsEnd => false;

            public override bool IsVertical => false;
        }

        public override bool BecomeFirstResponder()
        {
            var res = base.BecomeFirstResponder();
            if (res)
            {
                Logger.TryGet(LogEventLevel.Debug, "IOSIME")
                    ?.Log(null, "Became first responder");

                if (StructuredClient is { } structured)
                {
                    structured.TextChanged += ClientStateChanged;
                    structured.CaretPositionChanged += ClientStateChanged;
                    structured.CompositionChanged += ClientStateChanged;
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

                if (StructuredClient is { } structured)
                {
                    structured.TextChanged -= ClientStateChanged;
                    structured.CaretPositionChanged -= ClientStateChanged;
                    structured.CompositionChanged -= ClientStateChanged;
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
