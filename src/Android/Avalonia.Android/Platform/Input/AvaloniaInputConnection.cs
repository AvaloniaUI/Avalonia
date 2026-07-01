using System.Collections.Concurrent;
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

        public AvaloniaInputConnection(TopLevelImpl toplevel, IAndroidInputMethod inputMethod)
        {
            _toplevel = toplevel;
            _inputMethod = inputMethod;
            _editBuffer = new TextEditBuffer(_inputMethod, toplevel);
            _commandQueue = new ConcurrentQueue<EditCommand>();
        }

        public int ExtractedTextToken { get; private set; }

        public IAndroidInputMethod InputMethod => _inputMethod;

        public TopLevelImpl Toplevel => _toplevel;

        public bool IsInBatchEdit => _batchLevel > 0;
        public bool IsInMonitorMode { get; private set; }

        public Handler? Handler => null;

        public TextEditBuffer EditBuffer => _editBuffer;

        public bool IsInUpdate { get; set; }

        internal void UpdateState()
        {
            if (TryGetStructuredClient(out var structured))
            {
                var structuredSelection = ToTextSelection(structured.Selection);
                var structuredComposition = structured.CompositionRange is { } compositionRange
                    ? ToTextSelection(compositionRange)
                    : new TextSelection(-1, -1);

                if (IsInMonitorMode)
                {
                    InputMethod.IMM.UpdateExtractedText(InputMethod.View, ExtractedTextToken,
                        CreateExtractedText(structured));
                }

                InputMethod.IMM.UpdateSelection(InputMethod.View,
                    structuredSelection.Start,
                    structuredSelection.End,
                    structuredComposition.Start,
                    structuredComposition.End);
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

            if (!_inputMethod.IsActive)
            {
                return null;
            }

            if (TryGetStructuredClient(out var structured))
            {
                return CreateExtractedText(structured);
            }

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
                    var text = structured.GetText(structured.DocumentRange);
                    var start = MoveByCodePointsBackward(text, selection.Start, Math.Max(0, beforeLength));
                    var end = MoveByCodePointsForward(text, selection.End, Math.Max(0, afterLength));

                    structured.ReplaceText(CreateRange(structured, start, end), string.Empty);
                    structured.Selection = CreateRange(structured, start, start);
                    structured.CommitComposition();
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
                var text = structured.GetText(structured.DocumentRange);
                var caret = ToTextSelection(structured.Selection).Start;
                return TextUtils.GetCapsMode(text, caret, reqModes);
            }

            return TextUtils.GetCapsMode(_editBuffer.Text, _editBuffer.Selection.Start, reqModes);
        }

        public ICharSequence? GetSelectedTextFormatted([GeneratedEnum] GetTextFlags flags)
        {
            if (TryGetStructuredClient(out var structured))
            {
                return new Java.Lang.String(structured.GetText(structured.Selection));
            }

            return new Java.Lang.String(_editBuffer.SelectedText ?? "");
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
            var startPointer = structured.CreatePointer(normalizedStart, LogicalDirection.Forward);
            var endPointer = structured.CreatePointer(normalizedEnd, LogicalDirection.Backward);
            return structured.CreateRange(startPointer, endPointer);
        }

        private static ExtractedText CreateExtractedText(IStructuredTextInput structured)
        {
            var text = structured.GetText(structured.DocumentRange);
            var selection = ToTextSelection(structured.Selection);

            return new ExtractedText
            {
                Flags = text.Contains('\n') ? 0 : ExtractedTextFlags.SingleLine,
                PartialStartOffset = -1,
                PartialEndOffset = text.Length,
                SelectionStart = selection.Start,
                SelectionEnd = selection.End,
                StartOffset = 0,
                Text = new Java.Lang.String(text)
            };
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
