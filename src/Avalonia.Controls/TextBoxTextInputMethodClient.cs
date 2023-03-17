using System;
using System.Diagnostics;
using Avalonia.Controls.Presenters;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    internal class TextBoxTextInputMethodClient : ITextInputMethodClient
    {
        private TextBox? _parent;
        private TextPresenter? _presenter;
        private ITextEditable? _textEditable;

        public Visual TextViewVisual => _presenter!;

        public bool SupportsPreedit => true;

        public bool SupportsSurroundingText => true;

        public Rect CursorRectangle
        {
            get
            {
                if (_parent == null || _presenter == null)
                {
                    return default;
                }

                var visualRoot = _presenter.VisualRoot!;

                var transform = _presenter.TransformToVisual((Visual)visualRoot);

                if (transform == null)
                {
                    return default;
                }

                var rect = _presenter.GetCursorRectangle().TransformToAABB(transform.Value);
                
                return rect;
            }
        }

        public TextInputMethodSurroundingText SurroundingText
        {
            get
            {
                if (_presenter is null || _parent is null)
                {
                    return default;
                }

                var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_presenter.CaretIndex, false);

                var textLine = _presenter.TextLayout.TextLines[lineIndex];

                var lineStart = textLine.FirstTextSourceIndex;

                var lineText = GetTextLineText(textLine);

                var anchorOffset = Math.Max(0, _parent.SelectionStart - lineStart);

                var cursorOffset = Math.Max(0, _presenter.SelectionEnd - lineStart);

                return new TextInputMethodSurroundingText
                {
                    Text = lineText ?? "",
                    AnchorOffset = anchorOffset,
                    CursorOffset = cursorOffset
                };
            }
        }

        public ITextEditable? TextEditable
        {
            get => _textEditable; set
            {
                if (_textEditable != null)
                {
                    _textEditable.TextChanged -= TextEditable_TextChanged;
                    _textEditable.SelectionChanged -= TextEditable_SelectionChanged;
                    _textEditable.CompositionChanged -= TextEditable_CompositionChanged;
                }

                _textEditable = value;

                if (_textEditable != null)
                {
                    _textEditable.TextChanged += TextEditable_TextChanged;
                    _textEditable.SelectionChanged += TextEditable_SelectionChanged;
                    _textEditable.CompositionChanged += TextEditable_CompositionChanged;

                    if (_presenter != null)
                    {
                        _textEditable.Text = _presenter.Text;
                        _textEditable.SelectionStart = _presenter.SelectionStart;
                        _textEditable.SelectionEnd = _presenter.SelectionEnd;
                    }
                }
            }
        }

        private void TextEditable_CompositionChanged(object? sender, EventArgs e)
        {
            if (_presenter != null && _textEditable != null)
            {
                _presenter.SetCurrentValue(TextPresenter.CompositionRegionProperty, new TextRange(_textEditable.CompositionStart, _textEditable.CompositionEnd));
            }
        }

        private void TextEditable_SelectionChanged(object? sender, EventArgs e)
        {
            if (_parent != null && _textEditable != null)
            {
                _parent.SelectionStart = _textEditable.SelectionStart;
                _parent.SelectionEnd = _textEditable.SelectionEnd;
            }
        }

        private void TextEditable_TextChanged(object? sender, EventArgs e)
        {
            if (_parent != null)
            {
                if (_parent.Text != _textEditable?.Text)
                {
                    _parent.Text = _textEditable?.Text;
                }
            }
        }

        private static string GetTextLineText(TextLine textLine)
        {
            var builder = StringBuilderCache.Acquire(textLine.Length);

            foreach (var run in textLine.TextRuns)
            {
                if (run.Length > 0)
                {
#if NET6_0_OR_GREATER
                    builder.Append(run.Text.Span);
#else
                    builder.Append(run.Text.Span.ToArray());
#endif
                }
            }

            var lineText = builder.ToString();

            StringBuilderCache.Release(builder);

            return lineText;
        }

        public event EventHandler? TextViewVisualChanged;

        public event EventHandler? CursorRectangleChanged;

        public event EventHandler? SurroundingTextChanged;

        private string? _presenterText;
        private int _compositionStart;

        public void SetPreeditText(string? preeditText)
        {
            if (_presenter == null || _parent == null)
            {
                return;
            }

            if (_presenterText is null)
            {
                _presenterText = _parent.Text ?? "";
                _compositionStart = _parent.CaretIndex;
            }

            var text = GetText(preeditText);

            _presenter.SetCurrentValue(TextPresenter.TextProperty, text);

            _presenter.SetCurrentValue(TextPresenter.PreeditTextProperty, preeditText);

            _presenter.UpdateCaret(new CharacterHit(_compositionStart + (preeditText != null ? preeditText.Length : 0)), false);

            if (string.IsNullOrEmpty(preeditText))
            {
                _presenterText = null;
            }
        }

        private string? GetText(string? preeditText)
        {
            if (string.IsNullOrEmpty(preeditText))
            {
                return _presenterText;
            }

            if (string.IsNullOrEmpty(_presenterText))
            {
                return preeditText;
            }

            var sb = StringBuilderCache.Acquire(_presenterText.Length + preeditText.Length);

            sb.Append(_presenterText);
            sb.Insert(_compositionStart, preeditText);

            return StringBuilderCache.GetStringAndRelease(sb);
        }

        public void SetComposingRegion(TextRange? region)
        {
            if (_presenter == null)
            {
                return;
            }

            _presenter.SetCurrentValue(TextPresenter.CompositionRegionProperty, region);
        }

        public void SelectInSurroundingText(int start, int end)
        {
            if (_parent is null || _presenter is null)
            {
                return;
            }

            var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(_presenter.CaretIndex, false);

            var textLine = _presenter.TextLayout.TextLines[lineIndex];

            var lineStart = textLine.FirstTextSourceIndex;

            var selectionStart = lineStart + start;
            var selectionEnd = lineStart + end;

            _parent.SelectionStart = selectionStart;
            _parent.SelectionEnd = selectionEnd;
        }

        public void SetPresenter(TextPresenter? presenter, TextBox? parent)
        {
            if (_parent != null)
            {
                _parent.PropertyChanged -= OnParentPropertyChanged;
            }

            _parent = parent;

            if (_parent != null)
            {
                _parent.PropertyChanged += OnParentPropertyChanged;
            }

            if (_presenter != null)
            {
                _presenter.ClearValue(TextPresenter.PreeditTextProperty);

                _presenter.ClearValue(TextPresenter.CompositionRegionProperty);

                _presenter.CaretBoundsChanged -= OnCaretBoundsChanged;
            }

            _presenter = presenter;

            if (_presenter != null)
            {
                _presenter.CaretBoundsChanged += OnCaretBoundsChanged;
            }

            TextViewVisualChanged?.Invoke(this, EventArgs.Empty);

            OnCaretBoundsChanged(this, EventArgs.Empty);
        }

        private void OnParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                if (SupportsSurroundingText)
                {
                    SurroundingTextChanged?.Invoke(this, e);
                }
                if (_textEditable != null)
                {
                    var value = (int)(e.NewValue ?? 0);
                    if (e.Property == TextBox.SelectionStartProperty)
                    {
                        _textEditable.SelectionStart = value;
                    }

                    if (e.Property == TextBox.SelectionEndProperty)
                    {
                        _textEditable.SelectionEnd = value;
                    }
                }
            }

            if (e.Property == TextBox.TextProperty)
            {
                if (_textEditable != null)
                {
                    _textEditable.Text = (string?)e.NewValue;
                }
            }
        }

        private void OnCaretBoundsChanged(object? sender, EventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                CursorRectangleChanged?.Invoke(this, e);

            }, DispatcherPriority.Input);
        }
    }
}
