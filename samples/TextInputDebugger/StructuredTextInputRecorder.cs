using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;

namespace TextInputDebugger
{
    /// <summary>
    /// Wraps a control's structured text input client so every call from the platform backend and
    /// every event raised back at it is logged, while continuously checking the contract invariants
    /// a platform integration bug violates first. The backend cannot tell the difference: the base
    /// surface delegates member by member and the base events are re-raised through the protected
    /// raisers.
    /// </summary>
    internal sealed class StructuredTextInputRecorder : TextInputMethodClient, IStructuredTextInput
    {
        private readonly TextInputMethodClient _innerClient;
        private readonly IStructuredTextInput _inner;
        private readonly Action<TraceCategory, string, string, bool> _log;

        private string _shadowText;
        private long _lastVersion;

        public StructuredTextInputRecorder(TextInputMethodClient innerClient, Action<TraceCategory, string, string, bool> log)
        {
            _innerClient = innerClient;
            _inner = (IStructuredTextInput)innerClient;
            _log = log;

            _shadowText = _inner.GetText(_inner.DocumentRange);
            _lastVersion = _inner.DocumentVersion;

            _inner.TextChanged += OnInnerTextChanged;
            _inner.CaretPositionChanged += OnInnerCaretPositionChanged;
            _inner.CompositionChanged += OnInnerCompositionChanged;
            _inner.InputDecorationsChanged += OnInnerInputDecorationsChanged;

            innerClient.TextViewVisualChanged += (_, _) => RaiseTextViewVisualChanged();
            innerClient.CursorRectangleChanged += (_, _) =>
            {
                Log(TraceCategory.Geometry, "CursorRectangleChanged", FmtRect(_innerClient.CursorRectangle));
                RaiseCursorRectangleChanged();
            };
            innerClient.SurroundingTextChanged += (_, _) =>
            {
                Log(TraceCategory.Legacy, "SurroundingTextChanged", Trunc(_innerClient.SurroundingText));
                RaiseSurroundingTextChanged();
            };
            innerClient.SelectionChanged += (_, _) =>
            {
                var s = _innerClient.Selection;
                Log(TraceCategory.Legacy, "SelectionChanged", $"{s.Start}..{s.End}");
                RaiseSelectionChanged();
            };
            innerClient.ResetRequested += (_, _) =>
            {
                Log(TraceCategory.Event, "ResetRequested", "");
                RequestReset();
            };
            innerClient.InputPaneActivationRequested += (_, _) => RaiseInputPaneActivationRequested();

            Log(TraceCategory.Event, "ClientAttached",
                $"len={_shadowText.Length} v={_lastVersion} preedit={innerClient.SupportsPreedit} inDoc={innerClient.SupportsInDocumentComposition}");
        }

        public IStructuredTextInput Inner => _inner;

        // ── Base (legacy) surface ───────────────────────────────────────────

        public override Visual TextViewVisual => _innerClient.TextViewVisual;

        public override bool SupportsPreedit => _innerClient.SupportsPreedit;

        public override string? PreeditText => _innerClient.PreeditText;

        public override bool SupportsInDocumentComposition => _innerClient.SupportsInDocumentComposition;

        public override bool SupportsSurroundingText => _innerClient.SupportsSurroundingText;

        public override string SurroundingText
        {
            get
            {
                var text = _innerClient.SurroundingText;
                Log(TraceCategory.Legacy, "get_SurroundingText", Trunc(text));
                return text;
            }
        }

        public override Rect CursorRectangle
        {
            get
            {
                var rect = _innerClient.CursorRectangle;
                Log(TraceCategory.Geometry, "get_CursorRectangle", FmtRect(rect));
                return rect;
            }
        }

        public override TextSelection Selection
        {
            get
            {
                var selection = _innerClient.Selection;
                Log(TraceCategory.Legacy, "get_Selection", $"{selection.Start}..{selection.End}");
                return selection;
            }
            set
            {
                Log(TraceCategory.Legacy, "set_Selection", $"{value.Start}..{value.End}");
                CheckThread("set_Selection");
                _innerClient.Selection = value;
            }
        }

        public override void SetPreeditText(string? preeditText)
            => SetPreeditText(preeditText, null);

        public override void SetPreeditText(string? preeditText, int? cursorPos)
        {
            Log(TraceCategory.Legacy, "SetPreeditText", $"{Trunc(preeditText)} cursor={(cursorPos?.ToString() ?? "null")}");
            CheckThread("SetPreeditText");
            _innerClient.SetPreeditText(preeditText, cursorPos);
        }

        public override void ExecuteContextMenuAction(ContextMenuAction action)
        {
            Log(TraceCategory.Mutation, "ExecuteContextMenuAction", action.ToString());
            _innerClient.ExecuteContextMenuAction(action);
        }

        // ── ITextNavigation ─────────────────────────────────────────────────

        public ITextPointer DocumentStart => _inner.DocumentStart;

        public ITextPointer DocumentEnd => _inner.DocumentEnd;

        public ITextRange DocumentRange => _inner.DocumentRange;

        public long DocumentVersion => _inner.DocumentVersion;

        public ITextPointer GetPosition(ITextPointer origin, int distance)
        {
            var result = _inner.GetPosition(origin, distance);
            Log(TraceCategory.Read, "GetPosition", $"{FmtPtr(origin)} {distance:+0;-0;+0} -> {FmtPtr(result)}");
            return result;
        }

        public ITextPointer GetPosition(ITextPointer origin, TextUnit unit, int count)
        {
            var result = _inner.GetPosition(origin, unit, count);
            Log(TraceCategory.Read, "GetPosition", $"{FmtPtr(origin)} {unit} {count:+0;-0;+0} -> {FmtPtr(result)}");
            return result;
        }

        public ITextRange GetRangeEnclosing(ITextPointer position, TextUnit unit)
        {
            var result = _inner.GetRangeEnclosing(position, unit);
            Log(TraceCategory.Read, "GetRangeEnclosing", $"{FmtPtr(position)} {unit} -> {FmtRange(result)}");
            return result;
        }

        public ITextRange GetRange(ITextPointer a, ITextPointer b)
            => _inner.GetRange(a, b);

        public int GetOffset(ITextPointer from, ITextPointer to)
            => _inner.GetOffset(from, to);

        public string GetText(ITextRange range)
        {
            var text = _inner.GetText(range);
            var span = range.End.Offset - range.Start.Offset;
            Log(TraceCategory.Read, "GetText", $"{FmtRange(range)} -> {Trunc(text)}");
            if (text.Length != span)
            {
                Invariant($"GetText length {text.Length} != range span {span} for {FmtRange(range)}");
            }

            return text;
        }

        public event EventHandler<TextChange>? TextChanged;

        // ── IStructuredTextInput ────────────────────────────────────────────

        public ITextPointer CaretPosition => _inner.CaretPosition;

        ITextRange IStructuredTextInput.Selection
        {
            get
            {
                var selection = _inner.Selection;
                Log(TraceCategory.Read, "get_Selection(structured)", FmtRange(selection));
                return selection;
            }
            set
            {
                Log(TraceCategory.Mutation, "set_Selection(structured)", FmtRange(value));
                CheckThread("set_Selection(structured)");
                _inner.Selection = value;
            }
        }

        public ITextRange? CompositionRange
        {
            get => _inner.CompositionRange;
            set
            {
                Log(TraceCategory.Composition, "set_CompositionRange", value is null ? "null" : FmtRange(value));
                CheckThread("set_CompositionRange");
                _inner.CompositionRange = value;
            }
        }

        public void ReplaceText(ITextRange range, string text)
        {
            Log(TraceCategory.Mutation, "ReplaceText", $"{FmtRange(range)} <- {Trunc(text)}");
            CheckThread("ReplaceText");
            _inner.ReplaceText(range, text);
        }

        public void SetCompositionText(string? text, int cursorOffset)
        {
            Log(TraceCategory.Composition, "SetCompositionText", $"{Trunc(text)} cursor={cursorOffset}");
            CheckThread("SetCompositionText");
            _inner.SetCompositionText(text, cursorOffset);
        }

        public void CommitComposition()
        {
            Log(TraceCategory.Composition, "CommitComposition", "");
            CheckThread("CommitComposition");
            _inner.CommitComposition();
        }

        public Rect GetFirstRectForRange(ITextRange range)
        {
            var rect = _inner.GetFirstRectForRange(range);
            Log(TraceCategory.Geometry, "GetFirstRectForRange", $"{FmtRange(range)} -> {FmtRect(rect)}");
            return rect;
        }

        public Rect GetCaretRect(ITextPointer position)
        {
            var rect = _inner.GetCaretRect(position);
            Log(TraceCategory.Geometry, "GetCaretRect", $"{FmtPtr(position)} -> {FmtRect(rect)}");
            return rect;
        }

        public Rect[] GetSelectionRects(ITextRange range)
        {
            var rects = _inner.GetSelectionRects(range);
            Log(TraceCategory.Geometry, "GetSelectionRects", $"{FmtRange(range)} -> {rects.Length} rects");
            return rects;
        }

        public ITextPointer? GetClosestPosition(Point point)
        {
            var result = _inner.GetClosestPosition(point);
            Log(TraceCategory.Geometry, "GetClosestPosition", $"({point.X:0.#},{point.Y:0.#}) -> {(result is null ? "null" : FmtPtr(result))}");
            return result;
        }

        public ITextPointer? GetClosestPosition(Point point, ITextRange withinRange)
        {
            var result = _inner.GetClosestPosition(point, withinRange);
            Log(TraceCategory.Geometry, "GetClosestPosition", $"({point.X:0.#},{point.Y:0.#}) in {FmtRange(withinRange)} -> {(result is null ? "null" : FmtPtr(result))}");
            return result;
        }

        public ITextRange? GetCharacterRangeAtPoint(Point point)
        {
            var result = _inner.GetCharacterRangeAtPoint(point);
            Log(TraceCategory.Geometry, "GetCharacterRangeAtPoint", $"({point.X:0.#},{point.Y:0.#}) -> {(result is null ? "null" : FmtRange(result))}");
            return result;
        }

        public event EventHandler? CaretPositionChanged;

        public event EventHandler? CompositionChanged;

        public IReadOnlyList<TextInputDecoration> InputDecorations => _inner.InputDecorations;

        public void SetInputDecorations(IReadOnlyList<TextInputDecoration> decorations)
        {
            Log(TraceCategory.Composition, "SetInputDecorations", FmtDecorations(decorations));
            CheckThread("SetInputDecorations");
            _inner.SetInputDecorations(decorations);
        }

        public event EventHandler? InputDecorationsChanged;

        // ── Event forwarding with invariant checks ──────────────────────────

        private void OnInnerTextChanged(object? sender, TextChange change)
        {
            var freshVersion = _inner.DocumentVersion;
            var fresh = _inner.GetTextUnlogged();
            var position = change.Position.Offset;

            if (freshVersion <= _lastVersion)
            {
                Invariant($"DocumentVersion did not advance on TextChanged ({_lastVersion} -> {freshVersion})");
            }

            _lastVersion = freshVersion;

            var applied = false;
            if (position >= 0
                && position + change.OldLength <= _shadowText.Length
                && position + change.NewLength <= fresh.Length)
            {
                var expected = _shadowText
                    .Remove(position, change.OldLength)
                    .Insert(position, fresh.Substring(position, change.NewLength));
                applied = expected == fresh;
            }

            if (!applied)
            {
                Invariant($"TextChange delta does not reproduce the document (pos={position} old={change.OldLength} new={change.NewLength} len={fresh.Length})");
            }

            _shadowText = fresh;

            Log(TraceCategory.Event, "TextChanged", $"pos={position} old={change.OldLength} new={change.NewLength} v={freshVersion}");
            TextChanged?.Invoke(this, change);
        }

        private void OnInnerCaretPositionChanged(object? sender, EventArgs e)
        {
            var caret = _inner.CaretPosition;
            if (caret.Offset < 0 || caret.Offset > _shadowText.Length)
            {
                Invariant($"Caret offset {caret.Offset} outside the document (len={_shadowText.Length})");
            }

            Log(TraceCategory.Event, "CaretPositionChanged", FmtPtr(caret));
            CaretPositionChanged?.Invoke(this, e);
        }

        private void OnInnerCompositionChanged(object? sender, EventArgs e)
        {
            var composition = _inner.CompositionRange;
            if (composition is not null
                && (composition.Start.Offset < 0 || composition.End.Offset > _shadowText.Length))
            {
                Invariant($"CompositionRange {FmtRange(composition)} outside the document (len={_shadowText.Length})");
            }

            Log(TraceCategory.Event, "CompositionChanged", composition is null ? "null" : FmtRange(composition));
            CompositionChanged?.Invoke(this, e);
        }

        private void OnInnerInputDecorationsChanged(object? sender, EventArgs e)
        {
            Log(TraceCategory.Event, "InputDecorationsChanged", FmtDecorations(_inner.InputDecorations));
            InputDecorationsChanged?.Invoke(this, e);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void Log(TraceCategory category, string member, string details, bool isError = false)
            => _log(category, member, details, isError);

        private void Invariant(string details)
            => _log(TraceCategory.Invariant, "INVARIANT", details, true);

        private void CheckThread(string member)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Invariant($"{member} called off the UI thread");
            }
        }

        private static string FmtPtr(ITextPointer pointer)
            => $"{pointer.Offset}{(pointer.Gravity == LogicalDirection.Forward ? "f" : "b")}";

        private static string FmtRange(ITextRange range)
            => $"[{range.Start.Offset}..{range.End.Offset})";

        private static string FmtRect(Rect rect)
            => $"({rect.X:0.#},{rect.Y:0.#} {rect.Width:0.#}x{rect.Height:0.#})";

        private static string FmtDecorations(IReadOnlyList<TextInputDecoration> decorations)
        {
            if (decorations.Count == 0)
            {
                return "none";
            }

            var builder = new StringBuilder();
            foreach (var decoration in decorations)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                builder.Append(decoration.Kind).Append(FmtRange(decoration.Range));
            }

            return builder.ToString();
        }

        private static string Trunc(string? text, int max = 28)
        {
            if (text is null)
            {
                return "null";
            }

            var escaped = text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            return escaped.Length <= max ? $"\"{escaped}\"" : $"\"{escaped[..max]}\"+{escaped.Length - max}";
        }
    }

    internal static class RecorderExtensions
    {
        /// <summary>Reads the whole document without producing a trace entry.</summary>
        public static string GetTextUnlogged(this IStructuredTextInput input)
            => input.GetText(input.DocumentRange);
    }
}
