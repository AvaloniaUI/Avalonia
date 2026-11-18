using System;
using Android.Text;
using Android.Views.InputMethods;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Java.Lang;
using Math = System.Math;

namespace Avalonia.Android.Platform.Input
{
    // The structured half of the connection. The editable text the IME sees is the linear scope around the
    // selection, so every Android offset is an offset inside that scope. When the selection leaves the
    // scope, or the scope dissolves, input restarts and a new connection serves the scope the selection is
    // in now; a composition pins the scope until it ends.
    internal partial class AvaloniaInputConnection
    {
        private const char MaskCharacter = '•';

        private IStructuredTextInput? _structured;
        private ILinearTextScope? _scope;
        private bool _scopeStale;
        private bool _detached;

        // Set by an edit inside a batch: the state is reported once when the outermost batch ends.
        private bool _structuredStatePending;

        // The changes since the IME's extract base, merged into one region: it starts at _changeStart,
        // covered _changeOldLength code units in the base and covers _changeNewLength now. A partial
        // extract of that region carries the final text with the final selection.
        private bool _hasExtractBase;
        private bool _hasPendingChange;
        private int _changeStart;
        private int _changeOldLength;
        private int _changeNewLength;

        // HintMaxChars from the monitoring request (0 = the IME wants the whole scope). When > 0 the IME
        // asked for a bounded view, so extracts are windowed around the selection and re-sent whole.
        private int _extractHintChars;

        // Partial extract updates cost the size of the edit, not of the scope. False sends the whole extract
        // on every change, for an IME that mishandles partial updates.
        internal static bool UseDiffExtractedText = true;

        // Whether the edited field accepts multiple lines (from TextInputOptions.Multiline).
        public bool IsMultiline { get; set; }

        /// <summary>
        /// Whether the field is a password or other sensitive input. Text reads then answer with mask
        /// characters of the same length, so the IME keeps consistent offsets without the field's text
        /// leaving the control.
        /// </summary>
        public bool IsSecure { get; set; }

        /// <summary>The selection inside the scope, for the editor info the connection is created with.</summary>
        public (int Anchor, int Active) InitialSelection => _structured is { } structured
            ? SelectionOffsets(structured)
            : (-1, -1);

        private int ScopeLength => _scope?.Length ?? 0;

        private void AttachStructured(IStructuredTextInput structured)
        {
            _structured = structured;
            AttachScope(structured.GetLinearScope(structured.Selection.Active));
        }

        private void AttachScope(ILinearTextScope scope)
        {
            DetachScope();
            _scope = scope;
            _scopeStale = false;
            _scope.TextChanged += OnScopeTextChanged;
            _scope.Invalidated += OnScopeInvalidated;
        }

        private void DetachScope()
        {
            if (_scope is not null)
            {
                _scope.TextChanged -= OnScopeTextChanged;
                _scope.Invalidated -= OnScopeInvalidated;
                _scope = null;
            }

            _hasExtractBase = false;
            _hasPendingChange = false;
        }

        /// <summary>
        /// Stops serving the client: the connection was closed or replaced, so it neither listens to the
        /// document nor reports to the IME again.
        /// </summary>
        public void Detach()
        {
            DetachScope();
            _structured = null;
            _detached = true;
        }

        private void OnScopeInvalidated(object? sender, EventArgs e) => _scopeStale = true;

        private void OnScopeTextChanged(object? sender, LinearTextChange change)
        {
            if (!IsInMonitorMode || !_hasExtractBase)
            {
                return;
            }

            if (!_hasPendingChange)
            {
                _hasPendingChange = true;
                _changeStart = change.Start;
                _changeOldLength = change.OldLength;
                _changeNewLength = change.NewLength;
                return;
            }

            // Union of the merged region and this change, in the coordinates before this change. Inside the
            // merged region the base had _changeOldLength units where there are _changeNewLength now;
            // outside it the two coordinates differ only by a shift.
            var start = Math.Min(_changeStart, change.Start);
            var end = Math.Max(_changeStart + _changeNewLength, change.Start + change.OldLength);
            var length = end - start;

            _changeOldLength = length - _changeNewLength + _changeOldLength;
            _changeNewLength = length - change.OldLength + change.NewLength;
            _changeStart = start;
        }

        private void UpdateStructuredState(IStructuredTextInput structured)
        {
            if (_detached)
            {
                return;
            }

            if (IsInBatchEdit)
            {
                _structuredStatePending = true;
                return;
            }

            _structuredStatePending = false;

            if (NeedsRescope(structured))
            {
                // The new connection reports the new scope; offsets of this one mean nothing there.
                Detach();
                InputMethod.RestartInput();

                AndroidImeTrace.Event(this, "Rescope");
                return;
            }

            if (IsInMonitorMode)
            {
                FlushExtractedText(structured);
            }

            var (anchor, active) = SelectionOffsets(structured);
            var (compositionStart, compositionEnd) = CompositionOffsets(structured);

            InputMethod.IMM.UpdateSelection(InputMethod.View, anchor, active, compositionStart, compositionEnd);

            AndroidImeTrace.Event(this, "UpdateSelection sel=[{Anchor},{Active}] comp=[{CompStart},{CompEnd}] monitor={Monitor}",
                anchor, active, compositionStart, compositionEnd, IsInMonitorMode);
        }

        private bool NeedsRescope(IStructuredTextInput structured)
        {
            if (_scope is null || _scopeStale || !_scope.IsValid)
            {
                return true;
            }

            // A composition never spans two scopes, so it keeps the current one until it ends.
            if (structured.CompositionRange is not null)
            {
                return false;
            }

            var active = structured.Selection.Active;
            return active.CompareTo(_scope.Range.Start) < 0 || active.CompareTo(_scope.Range.End) > 0;
        }

        // Runs one IME operation as a batch, so the client's change notifications during the edit are held
        // and the IME learns the outcome once.
        private StructuredEdit BeginStructuredEdit()
        {
            BeginBatchEdit();
            _structuredStatePending = true;
            return new StructuredEdit(this);
        }

        private readonly struct StructuredEdit : IDisposable
        {
            private readonly AvaloniaInputConnection _connection;

            public StructuredEdit(AvaloniaInputConnection connection) => _connection = connection;

            public void Dispose() => _connection.EndBatchEdit();
        }

        private bool StructuredSetComposingRegion(IStructuredTextInput structured, int start, int end)
        {
            using var _ = BeginStructuredEdit();

            // An empty region ends the composition and keeps its text, as finishComposingText does.
            structured.CompositionRange = start == end || _scope is null ? null : ScopeRange(start, end);

            AndroidImeTrace.Event(this, "SetComposingRegion [{Start},{End})", start, end);
            return true;
        }

        private bool StructuredSetComposingText(IStructuredTextInput structured, string text, int newCursorPosition)
        {
            using var _ = BeginStructuredEdit();

            // newCursorPosition counts from the end of the text when positive (1 = just after it) and from
            // its start otherwise, and may point outside the composition.
            var cursor = newCursorPosition > 0 ? text.Length + newCursorPosition - 1 : newCursorPosition;

            int compositionStart;
            if (text.Length == 0)
            {
                // Empty composing text deletes the composition rather than restoring what it replaced.
                var selection = structured.Selection;
                compositionStart = Offset(structured.CompositionRange?.Start ?? selection.Start);

                if (structured.CompositionRange is not null)
                {
                    structured.CommitComposition(string.Empty);
                }
                else if (!selection.IsEmpty)
                {
                    structured.ReplaceText(structured.GetRange(selection.Start, selection.End), string.Empty);
                }
            }
            else
            {
                structured.SetCompositionText(text, Math.Clamp(cursor, 0, text.Length));
                compositionStart = structured.CompositionRange is { } composition ? Offset(composition.Start) : 0;
            }

            if (cursor < 0 || cursor > text.Length)
            {
                SetCaret(structured, compositionStart + cursor);
            }

            AndroidImeTrace.Event(this, "SetComposingText len={Length} newCursor={Cursor}", text.Length, newCursorPosition);
            return true;
        }

        private bool StructuredCommitText(IStructuredTextInput structured, string text, int newCursorPosition)
        {
            using var _ = BeginStructuredEdit();

            var start = Offset(structured.CompositionRange?.Start ?? structured.Selection.Start);

            // One change: the commit replaces the composition, or the selection when none is active.
            structured.CommitComposition(text);

            if (newCursorPosition != 1 && !structured.IsReadOnly)
            {
                SetCaret(structured, newCursorPosition > 0
                    ? start + text.Length + newCursorPosition - 1
                    : start + newCursorPosition);
            }

            AndroidImeTrace.Event(this, "CommitText len={Length} newCursor={Cursor} at={Start}", text.Length, newCursorPosition, start);
            return true;
        }

        private bool StructuredSetSelection(IStructuredTextInput structured, int start, int end)
        {
            // Out-of-range requests are ignored, as an Android text view ignores them.
            if (_scope is not { } scope || start < 0 || end < 0 || start > scope.Length || end > scope.Length)
            {
                return true;
            }

            using var _ = BeginStructuredEdit();

            // start is the anchor and end the active end, so a backward selection stays backward.
            structured.Selection = new DirectedTextRange(
                scope.GetPosition(start, LogicalDirection.Forward),
                scope.GetPosition(end, LogicalDirection.Forward));

            AndroidImeTrace.Event(this, "SetSelection [{Start},{End}]", start, end);
            return true;
        }

        private bool StructuredDeleteSurroundingText(IStructuredTextInput structured, int beforeLength, int afterLength,
            bool inCodePoints)
        {
            if (beforeLength < 0 || afterLength < 0 || _scope is not { } scope)
            {
                return false;
            }

            using var _ = BeginStructuredEdit();

            var (anchor, active) = SelectionOffsets(structured);
            var (compositionStart, compositionEnd) = CompositionOffsets(structured);
            var hasComposition = compositionStart >= 0;

            // The deletion skips over the selection and the composition: it removes text before the first
            // and after the last of them, and both survive it.
            var start = Math.Min(anchor, active);
            var end = Math.Max(anchor, active);
            if (hasComposition)
            {
                start = Math.Min(start, compositionStart);
                end = Math.Max(end, compositionEnd);
            }

            var deleteStart = inCodePoints ? StepCodePointsBackward(scope, start, beforeLength) : start - Math.Min(beforeLength, start);
            var deleteEnd = inCodePoints
                ? StepCodePointsForward(scope, end, afterLength)
                : end + Math.Min(afterLength, scope.Length - end);

            if (deleteStart == start && deleteEnd == end)
            {
                return true;
            }

            // The text after goes first, so the offsets before it still hold for the second deletion.
            if (deleteEnd > end)
            {
                structured.ReplaceText(ScopeRange(end, deleteEnd), string.Empty);
            }

            if (deleteStart < start)
            {
                structured.ReplaceText(ScopeRange(deleteStart, start), string.Empty);
            }

            // Replacing text ends the composition and collapses the selection; both lay between the two
            // deletions, so they come back shifted by what was removed before them.
            var shift = start - deleteStart;
            if (hasComposition)
            {
                structured.CompositionRange = ScopeRange(compositionStart - shift, compositionEnd - shift);
            }

            structured.Selection = new DirectedTextRange(
                scope.GetPosition(anchor - shift, LogicalDirection.Forward),
                scope.GetPosition(active - shift, LogicalDirection.Forward));

            AndroidImeTrace.Event(this, "DeleteSurroundingText codePoints={CodePoints} before={Before} after={After} range=[{Start},{End})",
                inCodePoints, beforeLength, afterLength, deleteStart, deleteEnd);
            return true;
        }

        private bool StructuredFinishComposingText(IStructuredTextInput structured)
        {
            using var _ = BeginStructuredEdit();

            structured.CommitComposition(null);

            AndroidImeTrace.Event(this, "FinishComposingText");
            return true;
        }

        private ExtractedText StructuredGetExtractedText(IStructuredTextInput structured, ExtractedTextRequest? request,
            bool monitor)
        {
            using var _ = AndroidImeTrace.BeginSection("Ime.GetExtractedText");

            var hintChars = Math.Max(0, request?.HintMaxChars ?? 0);
            if (monitor)
            {
                _extractHintChars = hintChars;
            }

            var (windowStart, windowEnd) = ComputeExtractWindow(structured, hintChars);
            var extracted = CreateExtractedText(structured, windowStart, windowEnd);

            // A monitoring pull is the IME's new base: changes before it are already in it.
            if (monitor)
            {
                _hasExtractBase = true;
                _hasPendingChange = false;
            }

            AndroidImeTrace.Event(this, "GetExtractedText monitor={Monitor} token={Token} start={Start} len={Length}",
                monitor, ExtractedTextToken, windowStart, windowEnd - windowStart);

            return extracted;
        }

        private CapitalizationMode StructuredGetCursorCapsMode(IStructuredTextInput structured, CapitalizationMode reqModes)
        {
            if (IsSecure || _scope is not { } scope)
            {
                return 0;
            }

            // Caps mode looks back only to the start of the current sentence, a caps-reset point, so the
            // sentence before the caret answers as the whole text would. Backward gravity keeps the
            // terminator of the previous sentence in the slice when the caret sits on a boundary.
            var caretPosition = structured.Selection.Start;
            var caret = Offset(caretPosition);
            var sentence = structured.GetRangeEnclosing(structured.WithGravity(caretPosition, LogicalDirection.Backward),
                TextUnit.Sentence);
            var from = Math.Min(Offset(sentence.Start), caret);
            var text = scope.GetText(from, caret - from);

            return TextUtils.GetCapsMode(text, text.Length, reqModes);
        }

        private ICharSequence? StructuredGetSelectedText(IStructuredTextInput structured)
        {
            // Null, not an empty string, when nothing is selected: some keyboards key their word recapture
            // off a null result.
            var (anchor, active) = SelectionOffsets(structured);
            if (anchor == active)
            {
                return null;
            }

            var start = Math.Min(anchor, active);
            return new Java.Lang.String(ReadText(start, Math.Abs(active - anchor)));
        }

        private ICharSequence? StructuredGetTextAfterCursor(IStructuredTextInput structured, int n)
        {
            var (anchor, active) = SelectionOffsets(structured);
            var end = Math.Max(anchor, active);

            // n may be int.MaxValue, so it is clamped by subtraction rather than added to the offset.
            return new Java.Lang.String(ReadText(end, Math.Clamp(n, 0, ScopeLength - end)));
        }

        private ICharSequence? StructuredGetTextBeforeCursor(IStructuredTextInput structured, int n)
        {
            var (anchor, active) = SelectionOffsets(structured);
            var start = Math.Min(anchor, active);
            var length = Math.Clamp(n, 0, start);

            return new Java.Lang.String(ReadText(start - length, length));
        }

        private void FlushExtractedText(IStructuredTextInput structured)
        {
            if (!_hasPendingChange)
            {
                return;
            }

            _hasPendingChange = false;

            if (_extractHintChars > 0 || !UseDiffExtractedText)
            {
                var (windowStart, windowEnd) = ComputeExtractWindow(structured, _extractHintChars);
                InputMethod.IMM.UpdateExtractedText(InputMethod.View, ExtractedTextToken,
                    CreateExtractedText(structured, windowStart, windowEnd));

                AndroidImeTrace.Event(this, "UpdateExtractedText full start={Start} len={Length}",
                    windowStart, windowEnd - windowStart);
                return;
            }

            var (anchor, active) = SelectionOffsets(structured);
            var extracted = new ExtractedText
            {
                Flags = IsMultiline ? 0 : ExtractedTextFlags.SingleLine,
                StartOffset = 0,
                PartialStartOffset = _changeStart,
                PartialEndOffset = _changeStart + _changeOldLength,
                SelectionStart = anchor,
                SelectionEnd = active,
                Text = new Java.Lang.String(ReadText(_changeStart, _changeNewLength))
            };

            InputMethod.IMM.UpdateExtractedText(InputMethod.View, ExtractedTextToken, extracted);

            AndroidImeTrace.Event(this, "UpdateExtractedText partial start={Start} old={Old} new={New}",
                _changeStart, _changeOldLength, _changeNewLength);
        }

        private ExtractedText CreateExtractedText(IStructuredTextInput structured, int windowStart, int windowEnd)
        {
            using var _ = AndroidImeTrace.BeginSection("Ime.CreateExtractedText");

            var (anchor, active) = SelectionOffsets(structured);
            var length = windowEnd - windowStart;

            return new ExtractedText
            {
                // SingleLine is a property of the field, not the text: a multi-line box holding one line is
                // still multi-line, and a window may omit a line break the scope has.
                Flags = IsMultiline ? 0 : ExtractedTextFlags.SingleLine,
                PartialStartOffset = -1,
                PartialEndOffset = -1,
                SelectionStart = Math.Clamp(anchor - windowStart, 0, length),
                SelectionEnd = Math.Clamp(active - windowStart, 0, length),
                StartOffset = windowStart,
                Text = new Java.Lang.String(ReadText(windowStart, length))
            };
        }

        // The extract window: the whole scope when hintChars <= 0, otherwise at most hintChars units around
        // the selection that always contain the whole selection, so the IME keeps the caret.
        private (int Start, int End) ComputeExtractWindow(IStructuredTextInput structured, int hintChars)
        {
            var length = ScopeLength;

            if (hintChars <= 0)
            {
                return (0, length);
            }

            var (anchor, active) = SelectionOffsets(structured);
            var selectionStart = Math.Min(anchor, active);
            var selectionEnd = Math.Max(anchor, active);

            var start = Math.Max(0, selectionStart - hintChars / 2);
            var end = Math.Max(selectionEnd, start + Math.Min(hintChars, length - start));

            if (end - start < hintChars)
            {
                start = Math.Max(0, end - hintChars);
            }

            return (start, end);
        }

        // Scope text for the IME; a secure field answers with mask characters of the same length.
        private string ReadText(int start, int length)
        {
            if (_scope is null || length <= 0)
            {
                return string.Empty;
            }

            return IsSecure ? new string(MaskCharacter, length) : _scope.GetText(start, length);
        }

        // The offset of a position in the scope, clamped to the scope's ends.
        private int Offset(ITextPointer position)
        {
            if (_scope is not { } scope || position.CompareTo(scope.Range.Start) <= 0)
            {
                return 0;
            }

            return position.CompareTo(scope.Range.End) >= 0 ? scope.Length : scope.GetOffset(position);
        }

        private ITextRange ScopeRange(int start, int end)
        {
            var length = ScopeLength;
            var from = Math.Clamp(Math.Min(start, end), 0, length);
            var to = Math.Clamp(Math.Max(start, end), 0, length);
            return _scope!.GetRange(from, to - from);
        }

        private void SetCaret(IStructuredTextInput structured, int offset)
        {
            var caret = _scope!.GetPosition(Math.Clamp(offset, 0, ScopeLength), LogicalDirection.Forward);
            structured.Selection = new DirectedTextRange(caret, caret);
        }

        private (int Anchor, int Active) SelectionOffsets(IStructuredTextInput structured)
        {
            var selection = structured.Selection;
            return (Offset(selection.Anchor), Offset(selection.Active));
        }

        private (int Start, int End) CompositionOffsets(IStructuredTextInput structured) =>
            structured.CompositionRange is { } composition
                ? (Offset(composition.Start), Offset(composition.End))
                : (-1, -1);

        // Steps count code points from offset, reading only the 2 * count + 1 units a step can cover. The
        // Android API counts code points, not the grapheme clusters a character unit steps over.
        private int StepCodePointsBackward(ILinearTextScope scope, int offset, int count)
        {
            var windowStart = Math.Max(0, offset - Math.Min(offset, count) * 2 - 1);
            var text = scope.GetText(windowStart, offset - windowStart);
            var index = text.Length;

            for (var i = 0; i < count && index > 0; i++)
            {
                index--;
                if (index > 0 && char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1]))
                {
                    index--;
                }
            }

            return windowStart + index;
        }

        private int StepCodePointsForward(ILinearTextScope scope, int offset, int count)
        {
            var available = scope.Length - offset;
            var text = scope.GetText(offset, Math.Min(available, Math.Min(count, available) * 2 + 1));
            var index = 0;

            for (var i = 0; i < count && index < text.Length; i++)
            {
                index += index + 1 < text.Length && char.IsHighSurrogate(text[index]) && char.IsLowSurrogate(text[index + 1])
                    ? 2
                    : 1;
            }

            return offset + index;
        }
    }
}
