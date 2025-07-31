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
            var selection = _editBuffer.Selection;

            if (IsInMonitorMode && InputMethod.Client is { } client)
            {
                InputMethod.IMM.UpdateExtractedText(InputMethod.View, ExtractedTextToken,
                    _editBuffer.ExtractedText);
            }

            var composition = _editBuffer.HasComposition ? _editBuffer.Composition!.Value : new TextSelection(-1, -1);
            InputMethod.IMM.UpdateSelection(InputMethod.View, selection.Start, selection.End, composition.Start, composition.End);
        }

        public bool SetComposingRegion(int start, int end)
        {
            if (InputMethod.IsActive)
            {
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
                var compositionText = text.SubSequence(0, text.Length());
                QueueCommand(new CompositionTextCommand(compositionText, newCursorPosition));
            }

            return InputMethod.IsActive;
        }

        public bool SetSelection(int start, int end)
        {
            if (InputMethod.IsActive)
            {
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
                var committedText = text.SubSequence(0, text.Length());
                QueueCommand(new CommitTextCommand(committedText, newCursorPosition));
            }

            return InputMethod.IsActive;
        }

        public bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            if (InputMethod.IsActive)
            {
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
                        FocusManager.GetFocusManager(_toplevel.InputRoot)?
                            .TryMoveFocus(NavigationDirection.Next);
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
                QueueCommand(new DeleteRegionInCodePointsCommand(beforeLength, afterLength));
            }

            return InputMethod.IsActive;
        }

        public bool FinishComposingText()
        {
            if (InputMethod.IsActive)
            {
                QueueCommand(new FinishComposingCommand());
            }

            return InputMethod.IsActive;
        }

        [return: GeneratedEnum]
        public CapitalizationMode GetCursorCapsMode([GeneratedEnum] CapitalizationMode reqModes)
        {
            return TextUtils.GetCapsMode(_editBuffer.Text, _editBuffer.Selection.Start, reqModes);
        }

        public ICharSequence? GetSelectedTextFormatted([GeneratedEnum] GetTextFlags flags)
        {
            return new Java.Lang.String(_editBuffer.SelectedText ?? "");
        }

        public ICharSequence? GetTextAfterCursorFormatted(int n, [GeneratedEnum] GetTextFlags flags)
        {
            var end = Math.Min(_editBuffer.Selection.End, _editBuffer.Text.Length);
            return SafeSubstring(_editBuffer.Text, end, Math.Min(n, _editBuffer.Text.Length - end));
        }

        public ICharSequence? GetTextBeforeCursorFormatted(int n, [GeneratedEnum] GetTextFlags flags)
        {
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
    }
}
