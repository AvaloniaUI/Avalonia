using System;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal class AvaloniaNativeTextInputMethod : ITextInputMethodImpl, IDisposable
    {
        private const char MaskCharacter = '\u2022';

        private TextInputMethodClient? _client;
        private IAvnTextInputMethodClient? _nativeClient;
        private readonly IAvnTextInputMethod _inputMethod;

        // A structured client is the document AppKit edits. AppKit addresses it by UTF-16 offsets, which
        // here are offsets in the linear scope around the selection; the view asks for text, selection
        // and geometry when it needs them instead of keeping a copy.
        private IStructuredTextInput? _structured;
        private ILinearTextScope? _scope;
        private bool _scopeStale;
        private bool _secure;
        
        public AvaloniaNativeTextInputMethod(IAvnTopLevel topLevel)
        {
            _inputMethod = topLevel.InputMethod;
        }

        public void Dispose()
        {
            _inputMethod.Dispose();
            _nativeClient?.Dispose();
        }

        public void Reset()
        {
            _inputMethod.Reset();
        }

        public void SetClient(TextInputMethodClient? client)
        {
            if (_client is { SupportsSurroundingText: true })
            {
                _client.SurroundingTextChanged -= OnSurroundingTextChanged;
                _client.CursorRectangleChanged -= OnCursorRectangleChanged;
                _client.SelectionChanged -= OnSelectionChanged;

                _nativeClient?.Dispose();
            }

            if (_structured is not null)
            {
                _structured.CompositionChanged -= OnCursorRectangleChanged;
                _structured = null;
                DetachScope();
            }

            _nativeClient = null;
            _client = client;

            if (_client != null)
            {
                if (_client is IStructuredTextInput structured)
                {
                    _structured = structured;
                    _structured.CompositionChanged += OnCursorRectangleChanged;
                }

                _nativeClient = new AvnTextInputMethodClient(this, _client);

                OnSurroundingTextChanged(this, EventArgs.Empty);
                OnCursorRectangleChanged(this, EventArgs.Empty);
                // Note: OnSelectionChanged isn't called, it's already up-to-date thanks to OnSurroundingTextChanged

                _client.SurroundingTextChanged += OnSurroundingTextChanged;
                _client.CursorRectangleChanged += OnCursorRectangleChanged;
                _client.SelectionChanged += OnSelectionChanged;
            }

            _inputMethod.SetClient(_nativeClient);
        }

        private void OnCursorRectangleChanged(object? sender, EventArgs e)
        {
            if (_client == null)
            {
                return;
            }

            var textViewVisual = _client.TextViewVisual;

            if(textViewVisual is null )
            {
                return;
            }

            var visualRoot = textViewVisual.VisualRoot;

            if(visualRoot is null)
            {
                return;
            }

            var transform = textViewVisual.TransformToVisual((Visual)visualRoot);

            if (transform == null)
            {
                return;
            }

            var rect = _client.CursorRectangle.TransformToAABB(transform.Value);         

            _inputMethod.SetCursorRect(rect.ToAvnRect());
        }

        private void OnSurroundingTextChanged(object? sender, EventArgs e)
        {
            // The view reads a structured client's text through the client, never from a copy.
            if (_client == null || _structured is not null)
            {
                return;
            }

            var surroundingText = _client.SurroundingText;
            var selection = _client.Selection;

            _inputMethod.SetSurroundingText(
                surroundingText ?? "",
                selection.Start,
                selection.End
            );
        }

        private void OnSelectionChanged(object? sender, EventArgs e)
        {
            if (_client is null || _structured is not null)
            {
                return;
            }

            var selection = _client.Selection;
            _inputMethod.SetSelectionInSurroundingText(selection.Start, selection.End);
        }

        public void SetCursorRect(Rect rect)
        {
            _inputMethod.SetCursorRect(rect.ToAvnRect());
        }

        public void SetOptions(TextInputOptions options)
        {
            _secure = options.ContentType is TextInputContentType.Password or TextInputContentType.Pin
                || options.IsSensitive;
        }

        // The scope the view's offsets refer to: the one around the selection's active end. A composition
        // keeps its scope until it ends, since it never spans two.
        private ILinearTextScope? EnsureScope(IStructuredTextInput structured)
        {
            if (!ReferenceEquals(structured, _structured))
            {
                return null;
            }

            if (_scope is { IsValid: true } scope && !_scopeStale)
            {
                if (structured.CompositionRange is not null)
                {
                    return scope;
                }

                var active = structured.Selection.Active;
                if (active.CompareTo(scope.Range.Start) >= 0 && active.CompareTo(scope.Range.End) <= 0)
                {
                    return scope;
                }
            }

            DetachScope();
            _scope = structured.GetLinearScope(structured.Selection.Active);
            _scope.Invalidated += OnScopeInvalidated;
            return _scope;
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

        private class AvnTextInputMethodClient : NativeCallbackBase, IAvnTextInputMethodClient
        {
            private readonly AvaloniaNativeTextInputMethod _owner;
            private readonly TextInputMethodClient _client;
            private readonly IStructuredTextInput? _structured;

            public AvnTextInputMethodClient(AvaloniaNativeTextInputMethod owner, TextInputMethodClient client)
            {
                _owner = owner;
                _client = client;
                _structured = client as IStructuredTextInput;
            }

            public void SetPreeditText(string preeditText)
            {
                if (_client.SupportsPreedit)
                {
                    _client.DeliverComposition(preeditText, null);
                }
            }

            public void SelectInSurroundingText(int start, int end)
            {
                if (_structured is not null)
                {
                    if (Scope() is { } scope)
                    {
                        _structured.Selection = new DirectedTextRange(Position(scope, start), Position(scope, end));
                    }

                    return;
                }

                if (_client.SupportsSurroundingText)
                {
                    _client.Selection = new TextSelection(start, end);
                }
            }

            public int IsStructured() => _structured is null ? 0 : 1;

            public int DocumentLength => Scope()?.Length ?? 0;

            public IAvnString? GetText(int start, int length)
            {
                if (Scope() is not { } scope)
                {
                    return string.Empty.ToAvnString();
                }

                start = Math.Clamp(start, 0, scope.Length);
                length = Math.Clamp(length, 0, scope.Length - start);

                // A password or other sensitive field answers with mask characters of the same length,
                // so offsets stay consistent without the text leaving the control.
                return (_owner._secure ? new string(MaskCharacter, length) : scope.GetText(start, length)).ToAvnString();
            }

            public unsafe void GetSelectedRange(int* start, int* length)
            {
                *start = 0;
                *length = 0;

                if (_structured is null || Scope() is not { } scope)
                {
                    return;
                }

                var selection = _structured.Selection;
                *start = Offset(scope, selection.Start);
                *length = Offset(scope, selection.End) - *start;
            }

            public unsafe void GetMarkedRange(int* start, int* length)
            {
                *start = -1;
                *length = 0;

                if (_structured?.CompositionRange is not { } composition || Scope() is not { } scope)
                {
                    return;
                }

                *start = Offset(scope, composition.Start);
                *length = Offset(scope, composition.End) - *start;
            }

            public void SetMarkedText(string? text, int selectionStart, int selectionLength, int replacementStart,
                int replacementLength)
            {
                if (_structured is null || Scope() is not { } scope)
                {
                    return;
                }

                text ??= string.Empty;

                if (replacementStart >= 0)
                {
                    RetargetComposition(scope, replacementStart, replacementLength);
                }

                if (text.Length == 0)
                {
                    // AppKit marks an empty string when the user deleted the whole composition, so its
                    // text goes rather than coming back as the text it replaced.
                    if (_structured.CompositionRange is not null)
                    {
                        _structured.CommitComposition(string.Empty);
                    }

                    return;
                }

                var caret = Math.Clamp(selectionStart, 0, text.Length);
                _structured.SetCompositionText(text, caret);

                // A non-empty selection inside the marked text is the clause being converted.
                var length = Math.Clamp(selectionLength, 0, text.Length - caret);
                if (length > 0 && _structured.CompositionRange is { } composition)
                {
                    var start = Offset(scope, composition.Start) + caret;
                    _structured.Selection = new DirectedTextRange(Position(scope, start), Position(scope, start + length));
                }
            }

            public int InsertText(string? text, int replacementStart, int replacementLength)
            {
                if (_structured is null || Scope() is not { } scope)
                {
                    return 0;
                }

                text ??= string.Empty;

                // The composition is already in the document, so the result replaces it in place;
                // raising it as typed text would insert it a second time.
                if (_structured.CompositionRange is not null)
                {
                    if (replacementStart >= 0)
                    {
                        RetargetComposition(scope, replacementStart, replacementLength);
                    }

                    _structured.CommitComposition(text);
                    return 1;
                }

                if (replacementStart >= 0)
                {
                    _structured.ReplaceText(Range(scope, replacementStart, replacementLength), text);
                    return 1;
                }

                return 0;
            }

            public void UnmarkText() => _structured?.CommitComposition(null);

            public int GetCharacterIndexFromPoint(AvnPoint point)
            {
                if (_structured is null || Scope() is not { } scope ||
                    _structured.GetCharacterRangeAtPoint(point.ToAvaloniaPoint()) is not { } character ||
                    character.Start.CompareTo(scope.Range.Start) < 0 || character.Start.CompareTo(scope.Range.End) >= 0)
                {
                    return -1;
                }

                return scope.GetOffset(character.Start);
            }

            public unsafe void GetFirstRectForRange(int start, int length, AvnRect* rect, int* actualStart,
                int* actualLength)
            {
                *rect = default;
                *actualStart = start;
                *actualLength = 0;

                if (_structured is null || Scope() is not { } scope)
                {
                    return;
                }

                var range = Range(scope, start, length);

                // AppKit asks for the first rectangle and the part of the range it covers, so a range
                // crossing a line break is cut at the end of its first line.
                var line = _structured.GetRangeEnclosing(range.Start, TextUnit.Line);
                var first = range.End.CompareTo(line.End) <= 0 ? range : _structured.GetRange(range.Start, line.End);

                *rect = (first.IsEmpty
                    ? _structured.GetCaretRect(first.Start)
                    : _structured.GetFirstRectForRange(first)).ToAvnRect();
                *actualStart = Offset(scope, first.Start);
                *actualLength = Offset(scope, first.End) - *actualStart;
            }

            private ILinearTextScope? Scope() => _structured is null ? null : _owner.EnsureScope(_structured);

            // The composition AppKit addresses by the replacement range: committed text there becomes the
            // composition (reconversion), and an existing composition moves only when the range differs.
            private void RetargetComposition(ILinearTextScope scope, int start, int length)
            {
                var target = Range(scope, start, length);

                if (_structured!.CompositionRange is { } composition &&
                    composition.Start.CompareTo(target.Start) == 0 && composition.End.CompareTo(target.End) == 0)
                {
                    return;
                }

                _structured.CompositionRange = target;
            }

            private static int Offset(ILinearTextScope scope, ITextPointer position)
            {
                if (position.CompareTo(scope.Range.Start) <= 0)
                {
                    return 0;
                }

                return position.CompareTo(scope.Range.End) >= 0 ? scope.Length : scope.GetOffset(position);
            }

            private static ITextPointer Position(ILinearTextScope scope, int offset) =>
                scope.GetPosition(Math.Clamp(offset, 0, scope.Length), LogicalDirection.Forward);

            private static ITextRange Range(ILinearTextScope scope, int start, int length)
            {
                start = Math.Clamp(start, 0, scope.Length);
                return scope.GetRange(start, Math.Clamp(length, 0, scope.Length - start));
            }
        }
    }
}
