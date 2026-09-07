using System;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Avalonia.iOS;

partial class AvaloniaView
{
    // The structured half of the responder. UIKit positions and ranges wrap the client's own pointers
    // and ranges, so UIKit never computes an index the document does not have. The document UIKit sees
    // is the linear scope around the selection: its ends are the beginning and end of the document, and
    // UIKit's offset arithmetic counts code units inside it. When the selection leaves the scope, or the
    // scope dissolves, UIKit is told the whole text changed; a composition keeps its scope until it ends.
    partial class TextInputResponder
    {
        private const char MaskCharacter = '•';

        private sealed class PointerPosition : UITextPosition, INSCopying
        {
            public PointerPosition(ITextPointer pointer) => Pointer = pointer;

            public ITextPointer Pointer { get; }

            public NSObject Copy(NSZone? zone) => new PointerPosition(Pointer);
        }

        private sealed class PointerRange : UITextRange, INSCopying
        {
            private UITextPosition? _start;
            private UITextPosition? _end;

            public PointerRange(ITextRange range) => Range = range;

            public ITextRange Range { get; }

            public override bool IsEmpty => Range.IsEmpty;

            public override UITextPosition Start => _start ??= new PointerPosition(Range.Start);

            public override UITextPosition End => _end ??= new PointerPosition(Range.End);

            public NSObject Copy(NSZone? zone) => new PointerRange(Range);
        }

        private readonly IStructuredTextInput? _structured;
        private ILinearTextScope? _scope;
        private bool _scopeStale;

        // Depth of the UIKit calls that edit the document. UIKit knows about its own edits, so the
        // change notifications they cause are not reported back to it.
        private int _uikitEditDepth;

        private UITextStorageDirection _selectionAffinity = UITextStorageDirection.Forward;

        private ILinearTextScope Scope(IStructuredTextInput structured)
        {
            if (_scope is null || _scopeStale || !_scope.IsValid)
            {
                AttachScope(structured);
            }

            return _scope!;
        }

        private void AttachScope(IStructuredTextInput structured)
        {
            DetachScope();
            _scope = structured.GetLinearScope(structured.Selection.Active);
            _scope.Invalidated += OnScopeInvalidated;
        }

        private void DetachScope()
        {
            if (_scope is not null)
            {
                _scope.Invalidated -= OnScopeInvalidated;
                _scope = null;
            }

            _scopeStale = false;
        }

        private void OnScopeInvalidated(object? sender, EventArgs e) => _scopeStale = true;

        // Moves to the scope that holds the selection when it left the current one. Returns true when the
        // scope changed, which to UIKit is a replacement of the whole text.
        private bool RescopeIfNeeded(IStructuredTextInput structured)
        {
            if (_scope is { IsValid: true } scope && !_scopeStale)
            {
                if (structured.CompositionRange is not null)
                {
                    return false;
                }

                var active = structured.Selection.Active;
                if (active.CompareTo(scope.Range.Start) >= 0 && active.CompareTo(scope.Range.End) <= 0)
                {
                    return false;
                }
            }

            AttachScope(structured);
            return true;
        }

        private void SubscribeStructured(IStructuredTextInput structured)
        {
            structured.TextChanged += OnStructuredTextChanged;
            structured.CaretPositionChanged += OnStructuredSelectionChanged;
            structured.CompositionChanged += OnStructuredSelectionChanged;
        }

        private void UnsubscribeStructured(IStructuredTextInput structured)
        {
            structured.TextChanged -= OnStructuredTextChanged;
            structured.CaretPositionChanged -= OnStructuredSelectionChanged;
            structured.CompositionChanged -= OnStructuredSelectionChanged;
            DetachScope();
        }

        private void OnStructuredTextChanged(object? sender, TextChange change)
        {
            if (_uikitEditDepth > 0 || _structured is null)
            {
                return;
            }

            RescopeIfNeeded(_structured);
            NotifyDelegate(TextWillChange, TextDidChange);
        }

        private void OnStructuredSelectionChanged(object? sender, EventArgs e)
        {
            if (_structured is null)
            {
                return;
            }

            // A new scope means new text to UIKit, even when UIKit moved the selection itself.
            if (RescopeIfNeeded(_structured))
            {
                NotifyDelegate(TextWillChange, TextDidChange);
            }

            if (_uikitEditDepth == 0)
            {
                NotifyDelegate(SelectionWillChange, SelectionDidChange);
            }
        }

        private void NotifyDelegate(IntPtr will, IntPtr did)
        {
            if (WeakInputDelegate == null)
            {
                return;
            }

            _inSurroundingTextUpdateEvent++;
            try
            {
                objc_msgSend(WeakInputDelegate.Handle.Handle, will, Handle.Handle);
                objc_msgSend(WeakInputDelegate.Handle.Handle, did, Handle.Handle);
            }
            finally
            {
                _inSurroundingTextUpdateEvent--;
            }
        }

        private UIKitEdit BeginUIKitEdit()
        {
            _uikitEditDepth++;
            return new UIKitEdit(this);
        }

        private readonly struct UIKitEdit : IDisposable
        {
            private readonly TextInputResponder _responder;

            public UIKitEdit(TextInputResponder responder) => _responder = responder;

            public void Dispose() => _responder._uikitEditDepth--;
        }

        private static ITextRange? AsRange(UITextRange? range) => (range as PointerRange)?.Range;

        private static ITextPointer? AsPointer(UITextPosition? position) => (position as PointerPosition)?.Pointer;

        private int Offset(ILinearTextScope scope, ITextPointer position)
        {
            if (position.CompareTo(scope.Range.Start) <= 0)
            {
                return 0;
            }

            return position.CompareTo(scope.Range.End) >= 0 ? scope.Length : scope.GetOffset(position);
        }

        private void StructuredInsertText(IStructuredTextInput structured, string text)
        {
            // Text typed while marked text is showing is the commit of that composition: it replaces the
            // composition, not the selection inside it.
            if (structured.CompositionRange is not null)
            {
                using var _ = BeginUIKitEdit();
                structured.CommitComposition(text);
                return;
            }

            TextInput(text);
        }

        private string StructuredTextInRange(IStructuredTextInput structured, UITextRange range)
        {
            if (AsRange(range) is not { } textRange)
            {
                return null!;
            }

            var text = structured.GetText(textRange);

            // A secure field answers with mask characters of the same length, so the tokenizer and
            // dictation see consistent offsets but never the text.
            return IsSecureEntry ? new string(MaskCharacter, text.Length) : text;
        }

        private void StructuredReplaceText(IStructuredTextInput structured, UITextRange range, string text)
        {
            if (AsRange(range) is { } textRange)
            {
                using var _ = BeginUIKitEdit();
                structured.ReplaceText(textRange, text);
            }
        }

        private void StructuredSetMarkedText(IStructuredTextInput structured, string? markedText, NSRange selectedRange)
        {
            using var _ = BeginUIKitEdit();

            markedText ??= string.Empty;

            if (markedText.Length == 0)
            {
                // UIKit marks an empty string when the user deleted the whole composition, so its text goes
                // rather than coming back as the text it replaced.
                if (structured.CompositionRange is not null)
                {
                    structured.CommitComposition(string.Empty);
                }

                return;
            }

            var caret = selectedRange.Location == NSRange.NotFound
                ? markedText.Length
                : (int)Math.Clamp((long)selectedRange.Location, 0, markedText.Length);
            structured.SetCompositionText(markedText, caret);

            // A non-empty selection inside the marked text is the clause being converted.
            var length = selectedRange.Location == NSRange.NotFound
                ? 0
                : (int)Math.Clamp((long)selectedRange.Length, 0, markedText.Length - caret);
            if (length > 0 && structured.CompositionRange is { } composition)
            {
                var scope = Scope(structured);
                var start = Offset(scope, composition.Start) + caret;
                structured.Selection = new DirectedTextRange(
                    scope.GetPosition(start, LogicalDirection.Forward),
                    scope.GetPosition(start + length, LogicalDirection.Forward));
            }
        }

        private void StructuredUnmarkText(IStructuredTextInput structured)
        {
            using var _ = BeginUIKitEdit();
            structured.CommitComposition(null);
        }

        private UITextRange StructuredGetTextRange(IStructuredTextInput structured, UITextPosition from, UITextPosition to)
        {
            if (AsPointer(from) is not { } a || AsPointer(to) is not { } b)
            {
                return null!;
            }

            return new PointerRange(a.CompareTo(b) <= 0 ? structured.GetRange(a, b) : structured.GetRange(b, a));
        }

        private UITextPosition StructuredGetPosition(IStructuredTextInput structured, UITextPosition from, nint offset)
        {
            if (AsPointer(from) is not { } pointer)
            {
                return null!;
            }

            var scope = Scope(structured);
            var target = Offset(scope, pointer) + (long)offset;

            // UIKit expects nil for a position outside the document.
            if (target < 0 || target > scope.Length)
            {
                return null!;
            }

            return new PointerPosition(scope.GetPosition((int)target, LogicalDirection.Forward));
        }

        private UITextPosition StructuredGetPosition(IStructuredTextInput structured, UITextPosition from,
            UITextLayoutDirection direction, nint offset)
        {
            if (AsPointer(from) is not { } pointer)
            {
                return null!;
            }

            if (offset == 0)
            {
                return new PointerPosition(pointer);
            }

            var steps = (int)Math.Min(Math.Abs((long)offset), int.MaxValue);
            var negative = offset < 0;
            ITextPointer target;

            if (direction is UITextLayoutDirection.Up or UITextLayoutDirection.Down)
            {
                // Vertical movement keeps the caret's horizontal position, so it hit-tests a point lines
                // above or below instead of stepping by line units, which would land on a line start.
                var caret = structured.GetCaretRect(pointer);
                var down = direction == UITextLayoutDirection.Down != negative;
                var height = Math.Max(1, caret.Height);
                var y = caret.Center.Y + (down ? steps : -steps) * height;

                if (structured.GetClosestPosition(new Point(caret.X, y)) is not { } hit)
                {
                    return null!;
                }

                target = hit;
            }
            else
            {
                var forward = direction == UITextLayoutDirection.Right != negative;
                target = structured.GetPosition(pointer, TextUnit.Character, forward ? steps : -steps);
            }

            var scope = Scope(structured);
            if (target.CompareTo(scope.Range.Start) < 0 || target.CompareTo(scope.Range.End) > 0)
            {
                return null!;
            }

            return new PointerPosition(target);
        }

        private static NSComparisonResult StructuredComparePosition(UITextPosition first, UITextPosition second)
        {
            if (AsPointer(first) is not { } a || AsPointer(second) is not { } b)
            {
                return NSComparisonResult.Same;
            }

            var result = a.CompareTo(b);
            return result < 0 ? NSComparisonResult.Ascending
                : result > 0 ? NSComparisonResult.Descending
                : NSComparisonResult.Same;
        }

        private nint StructuredGetOffset(IStructuredTextInput structured, UITextPosition from, UITextPosition to)
        {
            if (AsPointer(from) is not { } a || AsPointer(to) is not { } b)
            {
                return 0;
            }

            var scope = Scope(structured);
            return Offset(scope, b) - Offset(scope, a);
        }

        private static UITextPosition StructuredGetPositionWithinRange(UITextRange range, UITextLayoutDirection direction)
        {
            if (range is not PointerRange pointerRange)
            {
                return null!;
            }

            return direction is UITextLayoutDirection.Right or UITextLayoutDirection.Down
                ? pointerRange.End
                : pointerRange.Start;
        }

        // The range from the position to the farthest extent of the document in that direction.
        private UITextRange StructuredGetCharacterRange(IStructuredTextInput structured, UITextPosition position,
            UITextLayoutDirection direction)
        {
            if (AsPointer(position) is not { } pointer)
            {
                return null!;
            }

            var scope = Scope(structured).Range;
            return new PointerRange(direction is UITextLayoutDirection.Left or UITextLayoutDirection.Up
                ? structured.GetRange(scope.Start, pointer)
                : structured.GetRange(pointer, scope.End));
        }

        private UITextSelectionRect[] StructuredGetSelectionRects(IStructuredTextInput structured, UITextRange range)
        {
            if (AsRange(range) is not { } textRange)
            {
                return Array.Empty<UITextSelectionRect>();
            }

            // The first rectangle holds the start of the selection and the last its end, which is where
            // UIKit draws the grab handles.
            var rects = structured.GetSelectionRects(textRange);
            var result = new UITextSelectionRect[rects.Length];
            for (var i = 0; i < rects.Length; i++)
            {
                result[i] = new AvaloniaSelectionRect(rects[i], containsStart: i == 0, containsEnd: i == rects.Length - 1);
            }

            return result;
        }

        private UITextRange StructuredSelectedTextRange(IStructuredTextInput structured)
        {
            var selection = structured.Selection;
            return new PointerRange(structured.GetRange(selection.Start, selection.End));
        }

        private void StructuredSetSelectedTextRange(IStructuredTextInput structured, UITextRange? value)
        {
            using var _ = BeginUIKitEdit();

            if (AsRange(value) is { } range)
            {
                // UIKit keeps the direction a selection extends in as the selection affinity.
                structured.Selection = _selectionAffinity == UITextStorageDirection.Backward
                    ? new DirectedTextRange(range.End, range.Start)
                    : new DirectedTextRange(range.Start, range.End);
                return;
            }

            var caret = structured.Selection.Active;
            structured.Selection = new DirectedTextRange(caret, caret);
        }

        [Export("selectionAffinity")]
        public UITextStorageDirection SelectionAffinity
        {
            get => _structured is { } structured && structured.Selection.IsReversed
                ? UITextStorageDirection.Backward
                : _selectionAffinity;
            set => _selectionAffinity = value;
        }

        private static CGRect ToCGRect(Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
