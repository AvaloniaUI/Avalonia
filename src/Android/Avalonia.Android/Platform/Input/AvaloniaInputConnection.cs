using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Views;
using Android.Views.InputMethods;
using Avalonia.Android.Platform.SkiaPlatform;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Java.Lang;

namespace Avalonia.Android.Platform.Input
{
    internal class AvaloniaInputConnection : Object, IInputConnection
    {
        private readonly TopLevelImpl _toplevel;
        private readonly IAndroidInputMethod _inputMethod;
        private readonly TextEditBuffer _editBuffer;
        private readonly ConcurrentQueue<EditCommand> _commandQueue;

        private int _batchLevel = 0;

        // The DocumentVersion the IME's extracted-text buffer currently reflects (via the last full pull or
        // the last flushed diff). -1 is the sentinel "never extracted". DocumentVersion is non-negative and
        // bumped once per contiguous change, so it doubles as a continuity check for diff sync.
        private long _lastExtractedVersion = -1;

        // Diff sync: text changes are mirrored to the IME as partial ExtractedText updates built from the
        // client's TextChange deltas, so monitor mode costs O(edit) instead of O(document) per keystroke.
        // Set false to fall back to full-document extracts (the O1-guarded whole-doc path) if an IME turns
        // out to mishandle partial updates.
        internal static bool UseDiffExtractedText = true;

        private readonly List<PendingChange> _pendingChanges = new List<PendingChange>();
        private IStructuredTextInput? _subscribedClient;

        // HintMaxChars from the monitoring GetExtractedText request (0 = the IME wants the whole document).
        // When > 0 the IME asked for a bounded view, so extracts are windowed around the caret and pushed as
        // full windowed snapshots rather than partial diffs.
        private int _extractHintChars;

        public AvaloniaInputConnection(TopLevelImpl toplevel, IAndroidInputMethod inputMethod)
        {
            _toplevel = toplevel;
            _inputMethod = inputMethod;
            _editBuffer = new TextEditBuffer(_inputMethod, toplevel);
            _commandQueue = new ConcurrentQueue<EditCommand>();

            HookStructuredClient();
        }

        public int ExtractedTextToken { get; private set; }

        public IAndroidInputMethod InputMethod => _inputMethod;

        public TopLevelImpl Toplevel => _toplevel;

        public bool IsInBatchEdit => _batchLevel > 0;
        public bool IsInMonitorMode { get; private set; }

        public Handler? Handler => null;

        public TextEditBuffer EditBuffer => _editBuffer;

        public bool IsInUpdate { get; set; }

        // Whether the edited field accepts multiple lines (from TextInputOptions.Multiline). This is a
        // property of the control, not of the current text, so it - not a newline scan - drives the
        // ExtractedText SingleLine flag.
        public bool IsMultiline { get; set; }

        internal void UpdateState()
        {
            using var _ = AndroidImeTrace.BeginSection("Ime.UpdateState");

            if (TryGetStructuredClient(out var structured))
            {
                var structuredSelection = ToTextSelection(structured.Selection);
                var structuredComposition = structured.CompositionRange is { } compositionRange
                    ? ToTextSelection(compositionRange)
                    : new TextSelection(-1, -1);

                // Push text updates only outside a batch (buffered deltas flush together at EndBatchEdit)
                // and only while the IME is monitoring. Text is delta-driven (OnDocumentTextChanged buffers,
                // FlushPendingExtractedText emits partials); a moved caret/selection carries no TextChange,
                // so it never pushes an extract - it is reported by UpdateSelection below. That is the O1
                // guard, now structural rather than a version comparison.
                if (IsInMonitorMode && !IsInBatchEdit)
                {
                    if (UseDiffExtractedText)
                    {
                        FlushPendingExtractedText(structured);
                    }
                    else
                    {
                        var version = structured.DocumentVersion;
                        if (version != _lastExtractedVersion)
                        {
                            var (windowStart, windowEnd) = ComputeExtractWindow(structured, _extractHintChars);
                            InputMethod.IMM.UpdateExtractedText(InputMethod.View, ExtractedTextToken,
                                CreateExtractedText(structured, windowStart, windowEnd));
                            _lastExtractedVersion = version;

                            AndroidImeTrace.Event(this, "UpdateExtractedText full token={Token} start={Start} ver={Version}",
                                ExtractedTextToken, windowStart, version);
                        }
                    }
                }

                InputMethod.IMM.UpdateSelection(InputMethod.View,
                    structuredSelection.Start,
                    structuredSelection.End,
                    structuredComposition.Start,
                    structuredComposition.End);

                AndroidImeTrace.Event(this,
                    "UpdateSelection sel=[{Start},{End}] comp=[{CompStart},{CompEnd}] ver={Version} monitor={Monitor}",
                    structuredSelection.Start, structuredSelection.End,
                    structuredComposition.Start, structuredComposition.End,
                    structured.DocumentVersion, IsInMonitorMode);
                return;
            }

            var legacySelection = _editBuffer.Selection;

            if (IsInMonitorMode && InputMethod.Client is { } client)
            {
                InputMethod.IMM.UpdateExtractedText(InputMethod.View, ExtractedTextToken,
                    _editBuffer.ExtractedText);
            }

            var legacyComposition = _editBuffer.HasComposition ? _editBuffer.Composition!.Value : new TextSelection(-1, -1);
            InputMethod.IMM.UpdateSelection(InputMethod.View, legacySelection.Start, legacySelection.End, legacyComposition.Start, legacyComposition.End);
        }

        public bool SetComposingRegion(int start, int end)
        {
            if (InputMethod.IsActive)
            {
                if (TryGetStructuredClient(out var structured))
                {
                    structured.CompositionRange = CreateRange(structured, start, end);

                    AndroidImeTrace.Event(this, "SetComposingRegion [{Start},{End})", start, end);

                    UpdateState();
                    return true;
                }

                QueueCommand(new CompositionRegionCommand(start, end));
            }
            return InputMethod.IsActive;
        }

        public bool SetComposingText(ICharSequence? text, int newCursorPosition)
        {
            if (text is null)
            {
                return false;
            }

            if (InputMethod.IsActive)
            {
                if (TryGetStructuredClient(out var structured))
                {
                    var composingText = text.ToString() ?? string.Empty;
                    var cursorOffset = newCursorPosition > 0
                        ? composingText.Length + newCursorPosition - 1
                        : composingText.Length + newCursorPosition;
                    structured.SetCompositionText(composingText, cursorOffset);

                    AndroidImeTrace.Event(this, "SetComposingText '{Text}' newCursor={Cursor}",
                        composingText, newCursorPosition);

                    UpdateState();
                    return true;
                }

                var compositionText = text.SubSequence(0, text.Length());
                QueueCommand(new CompositionTextCommand(compositionText, newCursorPosition));
            }

            return InputMethod.IsActive;
        }

        public bool SetSelection(int start, int end)
        {
            if (InputMethod.IsActive)
            {
                if (TryGetStructuredClient(out var structured))
                {
                    structured.Selection = CreateRange(structured, start, end);

                    AndroidImeTrace.Event(this, "SetSelection [{Start},{End})", start, end);

                    UpdateState();
                    return true;
                }

                if (IsInUpdate)
                    new SelectionCommand(start, end).Apply(EditBuffer);
                else
                    QueueCommand(new SelectionCommand(start, end));
            }

            return InputMethod.IsActive;
        }

        public bool BeginBatchEdit()
        {
            _batchLevel = Interlocked.Increment(ref _batchLevel);

            AndroidImeTrace.Event(this, "BeginBatchEdit level={Level}", _batchLevel);

            return InputMethod.IsActive;
        }

        public bool EndBatchEdit()
        {
            _batchLevel = Interlocked.Decrement(ref _batchLevel);

            if (!IsInBatchEdit)
            {
                IsInUpdate = true;
                while (_commandQueue.TryDequeue(out var command))
                {
                    command.Apply(_editBuffer);
                }
                IsInUpdate = false;
            }

            AndroidImeTrace.Event(this, "EndBatchEdit level={Level}", _batchLevel);

            UpdateState();
            return IsInBatchEdit;
        }

        public bool CommitText(ICharSequence? text, int newCursorPosition)
        {
            if (InputMethod.Client is null || text is null)
            {
                return false;
            }

            if (InputMethod.IsActive)
            {
                if (TryGetStructuredClient(out var structured))
                {
                    var structuredCommittedText = text.ToString() ?? string.Empty;
                    var replaceRange = structured.CompositionRange ?? structured.Selection;
                    var start = replaceRange.Start.Offset;

                    structured.ReplaceText(replaceRange, structuredCommittedText);

                    var targetOffset = newCursorPosition > 0
                        ? start + structuredCommittedText.Length + newCursorPosition - 1
                        : start + newCursorPosition;

                    var clampedOffset = ClampInt(targetOffset, 0, structured.DocumentEnd.Offset);
                    structured.Selection = CreateRange(structured, clampedOffset, clampedOffset);
                    structured.CommitComposition();

                    AndroidImeTrace.Event(this, "CommitText '{Text}' newCursor={Cursor} at={Start}",
                        structuredCommittedText, newCursorPosition, start);

                    UpdateState();
                    return true;
                }

                var committedSubSequence = text.SubSequence(0, text.Length());
                QueueCommand(new CommitTextCommand(committedSubSequence, newCursorPosition));
            }

            return InputMethod.IsActive;
        }

        public bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            if (InputMethod.IsActive)
            {
                if (TryGetStructuredClient(out var structured))
                {
                    var selection = ToTextSelection(structured.Selection);
                    var start = Math.Max(0, selection.Start - Math.Max(0, beforeLength));
                    var end = Math.Min(structured.DocumentEnd.Offset, selection.End + Math.Max(0, afterLength));

                    structured.ReplaceText(CreateRange(structured, start, end), string.Empty);
                    structured.Selection = CreateRange(structured, start, start);
                    structured.CommitComposition();

                    AndroidImeTrace.Event(this, "DeleteSurroundingText before={Before} after={After} range=[{Start},{End})",
                        beforeLength, afterLength, start, end);

                    UpdateState();
                    return true;
                }

                QueueCommand(new DeleteRegionCommand(beforeLength, afterLength));
            }

            return InputMethod.IsActive;
        }

        public bool PerformEditorAction([GeneratedEnum] ImeAction actionCode)
        {
            switch (actionCode)
            {
                case ImeAction.Done:
                    {
                        _inputMethod.IMM.HideSoftInputFromWindow(_inputMethod.View.WindowToken, HideSoftInputFlags.ImplicitOnly);
                        break;
                    }
                case ImeAction.Next:
                    {
                        ((FocusManager?)_toplevel.InputRoot?.FocusManager)?.TryMoveFocus(NavigationDirection.Next);
                        break;
                    }
            }

            var eventTime = SystemClock.UptimeMillis();
            SendKeyEvent(new KeyEvent(eventTime,
                                      eventTime,
                                      KeyEventActions.Down,
                                      Keycode.Enter,
                                      0,
                                      0,
                                      0,
                                      0,
                                      KeyEventFlags.SoftKeyboard | KeyEventFlags.KeepTouchMode | KeyEventFlags.EditorAction));
            SendKeyEvent(new KeyEvent(eventTime,
                                      eventTime,
                                      KeyEventActions.Up,
                                      Keycode.Enter,
                                      0,
                                      0,
                                      0,
                                      0,
                                      KeyEventFlags.SoftKeyboard | KeyEventFlags.KeepTouchMode | KeyEventFlags.EditorAction));

            return InputMethod.IsActive;
        }

        public ExtractedText? GetExtractedText(ExtractedTextRequest? request, [GeneratedEnum] GetTextFlags flags)
        {
            IsInMonitorMode = ((int)flags & (int)TextExtractFlags.Monitor) != 0;

            ExtractedTextToken = IsInMonitorMode ? request?.Token ?? 0 : ExtractedTextToken;

            // Remember the IME's size hint for the monitored session so pushes re-window consistently; a
            // one-off (non-monitor) pull honors the hint for that read only.
            var hintChars = Math.Max(0, request?.HintMaxChars ?? 0);
            if (IsInMonitorMode)
            {
                _extractHintChars = hintChars;
            }

            if (!_inputMethod.IsActive)
            {
                return null;
            }

            using var _ = AndroidImeTrace.BeginSection("Ime.GetExtractedText");

            if (TryGetStructuredClient(out var structured))
            {
                var (windowStart, windowEnd) = ComputeExtractWindow(structured, hintChars);
                var extracted = CreateExtractedText(structured, windowStart, windowEnd);

                // A full pull is the IME's new base snapshot: it supersedes any buffered diffs, and a
                // following selection-only UpdateState must not re-push the same content.
                _pendingChanges.Clear();
                _lastExtractedVersion = structured.DocumentVersion;

                AndroidImeTrace.Event(this,
                    "GetExtractedText monitor={Monitor} token={Token} start={Start} len={Length} ver={Version}",
                    IsInMonitorMode, ExtractedTextToken, windowStart, extracted.PartialEndOffset, _lastExtractedVersion);

                return extracted;
            }

            AndroidImeTrace.Event(this, "GetExtractedText legacy monitor={Monitor} token={Token}",
                IsInMonitorMode, ExtractedTextToken);

            return _editBuffer.ExtractedText;
        }

        public bool PerformContextMenuAction(int id)
        {
            if (InputMethod.Client is not { } client)
                return false;

            switch (id)
            {
                case global::Android.Resource.Id.SelectAll:
                    client.ExecuteContextMenuAction(ContextMenuAction.SelectAll);
                    return true;
                case global::Android.Resource.Id.Cut:
                    client.ExecuteContextMenuAction(ContextMenuAction.Cut);
                    return true;
                case global::Android.Resource.Id.Copy:
                    client.ExecuteContextMenuAction(ContextMenuAction.Copy);
                    return true;
                case global::Android.Resource.Id.Paste:
                    client.ExecuteContextMenuAction(ContextMenuAction.Paste);
                    return true;
                default:
                    break;
            }
            return InputMethod.IsActive;
        }

        public bool ClearMetaKeyStates([GeneratedEnum] MetaKeyStates states)
        {
            return false;
        }

        public void CloseConnection()
        {
            UnhookStructuredClient();
            _commandQueue.Clear();
            _batchLevel = 0;
        }

        public bool CommitCompletion(CompletionInfo? text)
        {
            return false;
        }

        public bool CommitContent(InputContentInfo inputContentInfo, [GeneratedEnum] InputContentFlags flags, Bundle? opts)
        {
            return false;
        }

        public bool CommitCorrection(CorrectionInfo? correctionInfo)
        {
            return false;
        }

        public bool DeleteSurroundingTextInCodePoints(int beforeLength, int afterLength)
        {
            if (InputMethod.IsActive)
            {
                if (TryGetStructuredClient(out var structured))
                {
                    var selection = ToTextSelection(structured.Selection);
                    var before = Math.Max(0, beforeLength);
                    var after = Math.Max(0, afterLength);

                    // A code point spans at most two UTF-16 units, so N code points fit in 2N units; read only
                    // those bounded windows around the caret instead of the whole document. The +1 margin
                    // guarantees the count is reached before a surrogate-pair split at a window edge, so the
                    // walk never miscounts. GetPosition(Character) is deliberately not used: the Android API
                    // is code-point precise, while Character navigation steps whole grapheme clusters (which
                    // may be several code points).
                    var beforeWindowStart = Math.Max(0, selection.Start - (2 * before + 1));
                    var beforeText = structured.GetText(CreateRange(structured, beforeWindowStart, selection.Start));
                    var start = beforeWindowStart + MoveByCodePointsBackward(beforeText, beforeText.Length, before);

                    var afterWindowEnd = Math.Min(structured.DocumentEnd.Offset, selection.End + (2 * after + 1));
                    var afterText = structured.GetText(CreateRange(structured, selection.End, afterWindowEnd));
                    var end = selection.End + MoveByCodePointsForward(afterText, 0, after);

                    structured.ReplaceText(CreateRange(structured, start, end), string.Empty);
                    structured.Selection = CreateRange(structured, start, start);
                    structured.CommitComposition();

                    AndroidImeTrace.Event(this, "DeleteSurroundingTextInCodePoints before={Before} after={After} range=[{Start},{End})",
                        beforeLength, afterLength, start, end);

                    UpdateState();
                    return true;
                }

                QueueCommand(new DeleteRegionInCodePointsCommand(beforeLength, afterLength));
            }

            return InputMethod.IsActive;
        }

        public bool FinishComposingText()
        {
            if (InputMethod.IsActive)
            {
                if (TryGetStructuredClient(out var structured))
                {
                    structured.CommitComposition();

                    AndroidImeTrace.Event(this, "FinishComposingText");

                    UpdateState();
                    return true;
                }

                QueueCommand(new FinishComposingCommand());
            }

            return InputMethod.IsActive;
        }

        [return: GeneratedEnum]
        public CapitalizationMode GetCursorCapsMode([GeneratedEnum] CapitalizationMode reqModes)
        {
            if (TryGetStructuredClient(out var structured))
            {
                // Caps mode only looks back to the start of the current sentence, so read from the enclosing
                // sentence rather than the whole document. The sentence start is a caps-reset point (text
                // start or just after a .!? terminator), so GetCapsMode over the slice - with the caret at its
                // end - matches the whole-document result. Backward gravity keeps the preceding terminator in
                // the slice when the caret sits on a sentence boundary.
                var caret = ToTextSelection(structured.Selection).Start;
                var caretPointer = structured.PointerAt(caret, LogicalDirection.Backward);
                var sentence = structured.GetRangeEnclosing(caretPointer, TextUnit.Sentence);
                var start = Math.Min(sentence.Start.Offset, caret);
                var text = structured.GetText(CreateRange(structured, start, caret));

                AndroidImeTrace.Event(this, "GetCursorCapsMode caret={Caret} from={From} len={Len}",
                    caret, start, text.Length);

                return TextUtils.GetCapsMode(text, text.Length, reqModes);
            }

            return TextUtils.GetCapsMode(_editBuffer.Text, _editBuffer.Selection.Start, reqModes);
        }

        public ICharSequence? GetSelectedTextFormatted([GeneratedEnum] GetTextFlags flags)
        {
            // getSelectedText must return null (not an empty string) when nothing is selected - some
            // keyboards key their word-recapture / suggestion behavior off a null result.
            if (TryGetStructuredClient(out var structured))
            {
                var selection = structured.Selection;
                if (selection.IsEmpty)
                {
                    return null;
                }

                return new Java.Lang.String(structured.GetText(selection));
            }

            var selectedText = _editBuffer.SelectedText;
            if (string.IsNullOrEmpty(selectedText))
            {
                return null;
            }

            return new Java.Lang.String(selectedText);
        }

        public ICharSequence? GetTextAfterCursorFormatted(int n, [GeneratedEnum] GetTextFlags flags)
        {
            if (TryGetStructuredClient(out var structured))
            {
                var selection = ToTextSelection(structured.Selection);
                var rangeStart = ClampInt(selection.End, 0, structured.DocumentEnd.Offset);
                var rangeEnd = ClampInt(rangeStart + Math.Max(0, n), 0, structured.DocumentEnd.Offset);
                var text = structured.GetText(CreateRange(structured, rangeStart, rangeEnd));
                return new Java.Lang.String(text);
            }

            var end = Math.Min(_editBuffer.Selection.End, _editBuffer.Text.Length);
            return SafeSubstring(_editBuffer.Text, end, Math.Min(n, _editBuffer.Text.Length - end));
        }

        public ICharSequence? GetTextBeforeCursorFormatted(int n, [GeneratedEnum] GetTextFlags flags)
        {
            if (TryGetStructuredClient(out var structured))
            {
                var selection = ToTextSelection(structured.Selection);
                var rangeEnd = ClampInt(selection.Start, 0, structured.DocumentEnd.Offset);
                var rangeStart = ClampInt(rangeEnd - Math.Max(0, n), 0, rangeEnd);
                var text = structured.GetText(CreateRange(structured, rangeStart, rangeEnd));
                return new Java.Lang.String(text);
            }

            var start = Math.Max(0, _editBuffer.Selection.Start - n);
            var length = _editBuffer.Selection.Start - start;
            return SafeSubstring(_editBuffer.Text, start, length);
        }

        public bool PerformPrivateCommand(string? action, Bundle? data)
        {
            return false;
        }

        public bool ReportFullscreenMode(bool enabled)
        {
            return false;
        }

        public bool RequestCursorUpdates(int cursorUpdateMode)
        {
            return false;
        }

        public bool SendKeyEvent(KeyEvent? e)
        {
            _inputMethod.View.DispatchKeyEvent(e);

            return true;
        }

        private void QueueCommand(EditCommand command)
        {
            BeginBatchEdit();

            try
            {
                _commandQueue.Enqueue(command);
            }
            finally
            {
                EndBatchEdit();
            }
        }

        private static ICharSequence? SafeSubstring(string? text, int start, int length)
        {
            if (text == null || text.Length < start + length)
                return null;
            else
                return new Java.Lang.String(text.Substring(start, length));
        }

        private void HookStructuredClient()
        {
            if (_inputMethod.Client is IStructuredTextInput structured)
            {
                _subscribedClient = structured;
                structured.TextChanged += OnDocumentTextChanged;
            }
        }

        private void UnhookStructuredClient()
        {
            if (_subscribedClient is { } structured)
            {
                structured.TextChanged -= OnDocumentTextChanged;
                _subscribedClient = null;
            }

            _pendingChanges.Clear();
        }

        private void OnDocumentTextChanged(object? sender, TextChange change)
        {
            // Only mirror changes while the IME is monitoring; otherwise the next getExtractedText pull
            // re-establishes the base. Capture the inserted slice now - the document is consistent here and
            // never mid-mutation, and a later edit could overwrite that region before the buffer is flushed.
            if (!UseDiffExtractedText || !IsInMonitorMode || !TryGetStructuredClient(out var structured))
            {
                return;
            }

            var start = change.Position.Offset;

            // In windowed mode the flush re-sends the whole window, so the inserted slice is never used -
            // record an empty change just to mark the buffer dirty and skip the extra read.
            var inserted = _extractHintChars <= 0 && change.NewLength > 0
                ? structured.GetText(CreateRange(structured, start, start + change.NewLength))
                : string.Empty;

            _pendingChanges.Add(new PendingChange(start, change.OldLength, inserted));

            AndroidImeTrace.Event(this, "TextChanged start={Start} old={Old} new={New} ver={Version}",
                start, change.OldLength, change.NewLength, structured.DocumentVersion);
        }

        // Flushes buffered text deltas to the IME. For a whole-document view these are cheap partial
        // ExtractedText updates (O(edit) each) applied in order so the IME's buffer tracks our sequential
        // edits. For a windowed view (HintMaxChars), or if the version arithmetic does not line up, it sends
        // one full (windowed) snapshot instead, so a bounded or diverged buffer stays correct.
        private void FlushPendingExtractedText(IStructuredTextInput structured)
        {
            if (_pendingChanges.Count == 0)
            {
                return;
            }

            var (windowStart, windowEnd) = ComputeExtractWindow(structured, _extractHintChars);
            var windowed = _extractHintChars > 0;
            var expectedVersion = _lastExtractedVersion + _pendingChanges.Count;

            // A windowed view (the IME requested a bounded extract) or a version discontinuity re-sends the
            // whole window; only the whole-document base at offset 0 can be advanced with cheap partial diffs.
            if (windowed || _lastExtractedVersion < 0 || structured.DocumentVersion != expectedVersion)
            {
                InputMethod.IMM.UpdateExtractedText(InputMethod.View, ExtractedTextToken,
                    CreateExtractedText(structured, windowStart, windowEnd));

                AndroidImeTrace.Event(this, "UpdateExtractedText {Reason} start={Start} len={Len} ver={Version}",
                    windowed ? "windowed" : "resync", windowStart, windowEnd - windowStart, structured.DocumentVersion);
            }
            else
            {
                var selection = ToTextSelection(structured.Selection);

                foreach (var change in _pendingChanges)
                {
                    var extracted = new ExtractedText
                    {
                        Flags = IsMultiline ? 0 : ExtractedTextFlags.SingleLine,
                        StartOffset = 0,
                        PartialStartOffset = change.Start,
                        PartialEndOffset = change.Start + change.OldLength,
                        SelectionStart = selection.Start,
                        SelectionEnd = selection.End,
                        Text = new Java.Lang.String(change.InsertedText)
                    };

                    InputMethod.IMM.UpdateExtractedText(InputMethod.View, ExtractedTextToken, extracted);

                    AndroidImeTrace.Event(this,
                        "UpdateExtractedText partial start={Start} old={Old} newLen={NewLen}",
                        change.Start, change.OldLength, change.InsertedText.Length);
                }
            }

            _pendingChanges.Clear();
            _lastExtractedVersion = structured.DocumentVersion;
        }

        private readonly struct PendingChange
        {
            public PendingChange(int start, int oldLength, string insertedText)
            {
                Start = start;
                OldLength = oldLength;
                InsertedText = insertedText;
            }

            public int Start { get; }

            public int OldLength { get; }

            public string InsertedText { get; }
        }

        private bool TryGetStructuredClient(out IStructuredTextInput structured)
        {
            if (InputMethod.Client is IStructuredTextInput structuredClient)
            {
                structured = structuredClient;
                return true;
            }

            structured = null!;
            return false;
        }

        private static TextSelection ToTextSelection(ITextRange range)
        {
            var start = Math.Min(range.Start.Offset, range.End.Offset);
            var end = Math.Max(range.Start.Offset, range.End.Offset);
            return new TextSelection(start, end);
        }

        private static ITextRange CreateRange(IStructuredTextInput structured, int start, int end)
        {
            var normalizedStart = Math.Min(start, end);
            var normalizedEnd = Math.Max(start, end);
            return structured.RangeAt(normalizedStart, normalizedEnd - normalizedStart);
        }

        private ExtractedText CreateExtractedText(IStructuredTextInput structured, int windowStart, int windowEnd)
        {
            using var _ = AndroidImeTrace.BeginSection("Ime.CreateExtractedText");

            var text = structured.GetText(CreateRange(structured, windowStart, windowEnd));
            var selection = ToTextSelection(structured.Selection);

            // A windowed extract reports StartOffset = windowStart and gives the selection relative to it,
            // clamped into the window. For the whole-document case windowStart is 0, so this reduces to the
            // absolute selection.
            var selectionStart = ClampInt(selection.Start - windowStart, 0, text.Length);
            var selectionEnd = ClampInt(selection.End - windowStart, 0, text.Length);

            return new ExtractedText
            {
                // SingleLine is a property of the field, not the text: a multi-line box holding one line of
                // content is still multi-line, and a window may omit a newline the document has. Derive it
                // from the control's multi-line capability, not a scan of the (possibly windowed) text.
                Flags = IsMultiline ? 0 : ExtractedTextFlags.SingleLine,
                PartialStartOffset = -1,
                PartialEndOffset = text.Length,
                SelectionStart = selectionStart,
                SelectionEnd = selectionEnd,
                StartOffset = windowStart,
                Text = new Java.Lang.String(text)
            };
        }

        // Computes the extract window for the current selection. hintChars <= 0 (the IME wants the whole
        // document) returns the full range; otherwise a caret-centered window of at most hintChars units that
        // always contains the whole selection so the IME keeps the caret.
        private static (int Start, int End) ComputeExtractWindow(IStructuredTextInput structured, int hintChars)
        {
            var docEnd = structured.DocumentEnd.Offset;

            if (hintChars <= 0)
            {
                return (0, docEnd);
            }

            var selection = ToTextSelection(structured.Selection);
            var start = Math.Max(0, selection.Start - hintChars / 2);
            var end = Math.Min(docEnd, start + hintChars);

            // Always contain the whole selection, even if it is larger than the hint.
            if (end < selection.End)
            {
                end = Math.Min(docEnd, selection.End);
            }

            // Use the full character budget when the window bumps into the document end.
            if (end - start < hintChars)
            {
                start = Math.Max(0, end - hintChars);
            }

            return (start, end);
        }

        private static int MoveByCodePointsBackward(string text, int start, int codePoints)
        {
            var index = ClampInt(start, 0, text.Length);
            for (var i = 0; i < codePoints && index > 0; i++)
            {
                index--;
                if (index > 0 && char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1]))
                {
                    index--;
                }
            }

            return index;
        }

        private static int MoveByCodePointsForward(string text, int start, int codePoints)
        {
            var index = ClampInt(start, 0, text.Length);
            for (var i = 0; i < codePoints && index < text.Length; i++)
            {
                if (index + 1 < text.Length && char.IsHighSurrogate(text[index]) && char.IsLowSurrogate(text[index + 1]))
                {
                    index += 2;
                }
                else
                {
                    index++;
                }
            }

            return index;
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min)
                return min;

            return value > max ? max : value;
        }
    }
}
