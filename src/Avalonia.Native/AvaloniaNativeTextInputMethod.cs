using System;
using Avalonia.Input.TextInput;
using Avalonia.Native.Interop;

namespace Avalonia.Native
{
    internal class AvaloniaNativeTextInputMethod : ITextInputMethodImpl, IDisposable
    {
        private TextInputMethodClient? _client;
        private IAvnTextInputMethodClient? _nativeClient;
        private readonly IAvnTextInputMethod _inputMethod;
        
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

            _nativeClient = null;
            _client = client;

            if (_client != null)
            {
                _nativeClient = new AvnTextInputMethodClient(_client);

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
            if (_client == null)
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
            if (_client is null)
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
           
        }

        private class AvnTextInputMethodClient : NativeCallbackBase, IAvnTextInputMethodClient
        {
            private readonly TextInputMethodClient _client;
            private readonly IStructuredTextInput? _structured;

            public AvnTextInputMethodClient(TextInputMethodClient client)
            {
                _client = client;
                _structured = client as IStructuredTextInput;
            }

            public int IsStructured() => _structured is null ? 0 : 1;

            public void SetPreeditText(string preeditText)
                => SetCompositionText(string.IsNullOrEmpty(preeditText) ? null : preeditText, -1);

            public void SelectInSurroundingText(int start, int end)
            {
                if (_client.SupportsSurroundingText)
                {
                    _client.Selection = new TextSelection(start, end);
                }
            }

            public void SetCompositionText(string? text, int cursorOffset)
            {
                if (_structured is null)
                {
                    // Legacy client: the composition stays outside the document.
                    if (_client.SupportsPreedit)
                    {
                        _client.SetPreeditText(text, cursorOffset < 0 ? null : cursorOffset);
                    }

                    return;
                }

                _structured.SetCompositionText(text, cursorOffset < 0 ? 0 : cursorOffset);
            }

            public void CommitComposition()
            {
                if (_structured is null)
                {
                    if (_client.SupportsPreedit)
                    {
                        _client.SetPreeditText(null, null);
                    }

                    return;
                }

                _structured.CommitComposition();
            }

            public unsafe void GetCompositionRange(int* start, int* end)
            {
                *start = -1;
                *end = -1;

                if (_structured?.CompositionRange is not { } range)
                {
                    return;
                }

                *start = range.Start.Offset;
                *end = range.End.Offset;
            }

            // The structured geometry contract is top-level coordinates - the same space
            // the native side works in - so points and rects pass through untransformed.

            public int GetCharacterIndexFromPoint(AvnPoint point)
            {
                if (_structured is null)
                {
                    return -1;
                }

                return _structured.GetClosestPosition(point.ToAvaloniaPoint())?.Offset ?? -1;
            }

            public int GetCharacterIndexFromPointWithinRange(AvnPoint point, int start, int end)
            {
                if (_structured is null)
                {
                    return -1;
                }

                // Returns null when the point is outside the range, which is exactly the containment
                // answer the plain nearest-index query cannot give.
                var range = _structured.RangeAt(start, Math.Max(0, end - start));

                return _structured.GetClosestPosition(point.ToAvaloniaPoint(), range)?.Offset ?? -1;
            }

            public unsafe void GetFirstRectForRange(int start, int end, AvnRect* rect)
            {
                *rect = default;

                if (_structured is null)
                {
                    return;
                }

                *rect = _structured.GetFirstRectForRange(_structured.RangeAt(start, Math.Max(0, end - start))).ToAvnRect();
            }
        }
    }
}
