using System;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    internal class TextSelectionCanvas : Canvas
    {
        private TextSelectionHandle _caretHandle;
        private TextSelectionHandle _startHandle;
        private TextSelectionHandle _endHandle;
        private TextPresenter? _presenter;
        private TextBox? _textBox;
        private bool _showHandle;

        internal bool ShowHandles
        {
            get => _showHandle; set
            {
                _showHandle = value;

                if (!value)
                {
                    _startHandle.IsVisible = false;
                    _endHandle.IsVisible = false;
                    _caretHandle.IsVisible = false;
                }

                IsVisible = value;
            }
        }

        public TextSelectionCanvas()
        {
            _caretHandle = new TextSelectionHandle()
            {
                SelectionHandleType = SelectionHandleType.Caret
            };
            _startHandle = new TextSelectionHandle();
            _endHandle = new TextSelectionHandle();

            ClipToBounds = true;

            Children.Add(_caretHandle);
            Children.Add(_startHandle);
            Children.Add(_endHandle);

            _caretHandle.DragDelta += CaretHandle_DragDelta;
            _caretHandle.DragCompleted += Handle_DragCompleted;

            _startHandle.DragDelta += StartHandle_DragDelta;
            _startHandle.DragCompleted += Handle_DragCompleted;
            _endHandle.DragDelta += EndHandle_DragDelta;
            _endHandle.DragCompleted += Handle_DragCompleted;

            _caretHandle.Classes.Add("caret");
            _startHandle.Classes.Add("start");
            _endHandle.Classes.Add("end");

            _startHandle.SetTopLeft(default);
            _caretHandle.SetTopLeft(default);
            _endHandle.SetTopLeft(default);

            IsVisible = ShowHandles;

            ClipToBounds = false;
        }

        private void EndHandle_DragDelta(object? sender, VectorEventArgs e)
        {
            if (sender is TextSelectionHandle handle)
                DragSelectionHandle(handle);
        }

        private void StartHandle_DragDelta(object? sender, VectorEventArgs e)
        {
            if (sender is TextSelectionHandle handle)
                DragSelectionHandle(handle);
        }

        private void CaretHandle_DragDelta(object? sender, Input.VectorEventArgs e)
        {
            if (_presenter != null && _textBox != null)
            {
                var point = ToPresenter(_caretHandle.IndicatorPosition);
                _presenter.MoveCaretToPoint(point);
                _textBox.SelectionStart = _textBox.SelectionEnd = _presenter.CaretIndex;
                var points = _presenter.GetCaretPoints();

                _caretHandle?.SetTopLeft(ToLayer(points.Item2));
            }
        }

        private void Handle_DragCompleted(object? sender, Input.VectorEventArgs e)
        {
            MoveHandlesToSelection();
        }

        private void EnsureVisible()
        {
            if (_textBox is { } t && t.VisualRoot is Visual r)
            {
                var bounds = t.Bounds;
                var topLeft = t.TranslatePoint(default, r as Visual) ?? default;
                bounds = bounds.WithX(topLeft.X).WithY(topLeft.Y);

                var hasSelection = _textBox.SelectionStart != _textBox.SelectionEnd;

                _startHandle.IsVisible = bounds.Contains(new Point(Canvas.GetLeft(_startHandle), Canvas.GetTop(_startHandle))) && ShowHandles && hasSelection;
                _endHandle.IsVisible = bounds.Contains(new Point(Canvas.GetLeft(_endHandle), Canvas.GetTop(_endHandle))) && ShowHandles && hasSelection;
                _caretHandle.IsVisible = bounds.Contains(new Point(Canvas.GetLeft(_caretHandle), Canvas.GetTop(_caretHandle))) && ShowHandles && !hasSelection;
            }
        }

        private void DragSelectionHandle(TextSelectionHandle handle)
        {
            if (_presenter != null && _textBox != null)
            {
                var point = ToPresenter(handle.IndicatorPosition);
                point = point.WithY(point.Y - _presenter.FontSize / 2);
                var hit = _presenter.TextLayout.HitTestPoint(point);
                var caret = hit.CharacterHit.FirstCharacterIndex + hit.CharacterHit.TrailingLength;

                var otherHandle = handle == _startHandle ? _endHandle : _startHandle;

                if (handle.SelectionHandleType == SelectionHandleType.Start)
                    _textBox.SelectionStart = caret;
                else
                    _textBox.SelectionEnd = caret;

                var selectionStart = _textBox.SelectionStart;
                var selectionEnd = _textBox.SelectionEnd;
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                var rects = _presenter.TextLayout.HitTestTextRange(start, length);

                if (rects.Any())
                {
                    var first = rects.First();
                    var last = rects.Last();

                    if (handle.SelectionHandleType == SelectionHandleType.Start)
                        handle?.SetTopLeft(ToLayer(first.BottomLeft));
                    else
                        handle?.SetTopLeft(ToLayer(last.BottomRight));

                    if (otherHandle.SelectionHandleType == SelectionHandleType.Start)
                        otherHandle?.SetTopLeft(ToLayer(first.BottomLeft));
                    else
                        otherHandle?.SetTopLeft(ToLayer(last.BottomRight));
                }

                _presenter?.MoveCaretToTextPosition(caret);
            }

            EnsureVisible();
        }

        private Point ToLayer(Point point)
        {
            return (_presenter?.VisualRoot is Visual v) ? _presenter?.TranslatePoint(point, v) ?? point : point;
        }

        private Point ToPresenter(Point point)
        {
            return (_presenter is { } p) ? (p.VisualRoot as Visual)?.TranslatePoint(point, p) ?? point : point;
        }

        public void MoveHandlesToSelection()
        {
            if (_presenter == null || _textBox == null || _startHandle.IsDragging || _endHandle.IsDragging)
            {
                return;
            }

            var hasSelection = _textBox.SelectionStart != _textBox.SelectionEnd;
            if (_caretHandle != null)
            {
                var points = _presenter.GetCaretPoints();

                _caretHandle.SetTopLeft(ToLayer(points.Item2));
            }

            if (hasSelection)
            {
                var selectionStart = _textBox.SelectionStart;
                var selectionEnd = _textBox.SelectionEnd;
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                var rects = _presenter.TextLayout.HitTestTextRange(start, length);

                if (rects.Any())
                {
                    var first = rects.First();
                    var last = rects.Last();

                    if (_startHandle != null && !_startHandle.IsDragging)
                    {
                        _startHandle.SetTopLeft(ToLayer(first.BottomLeft));
                        _startHandle.SelectionHandleType = selectionStart < selectionEnd ? SelectionHandleType.Start : SelectionHandleType.End;
                    }

                    if (_endHandle != null && !_endHandle.IsDragging)
                    {
                        _endHandle.SetTopLeft(ToLayer(last.BottomRight));
                        _endHandle.SelectionHandleType = selectionStart > selectionEnd ? SelectionHandleType.Start : SelectionHandleType.End;
                    }
                }
            }
        }

        internal void SetPresenter(TextPresenter? textPresenter)
        {
            _presenter = textPresenter;
            if (_presenter != null)
            {
                _textBox = _presenter.FindAncestorOfType<TextBox>();

                if (_textBox != null)
                {
                    _textBox.AddHandler(TextBox.TextChangingEvent, TextChanged, handledEventsToo: true);
                    _textBox.AddHandler(KeyDownEvent, TextBoxKeyDown, handledEventsToo: true);
                    _textBox.AddHandler(LostFocusEvent, TextBoxLostFocus, handledEventsToo: true);
                    _textBox.AddHandler(PointerReleasedEvent, TextBoxPointerReleased, handledEventsToo: true);

                    _textBox.PropertyChanged += _textBox_PropertyChanged;
                    _textBox.LayoutUpdated += _textBox_LayoutUpdated;
                }
            }
            else
            {
                if (_textBox != null)
                {
                    _textBox.RemoveHandler(TextBox.TextChangingEvent, TextChanged);
                    _textBox.RemoveHandler(KeyDownEvent, TextBoxKeyDown);
                    _textBox.RemoveHandler(PointerReleasedEvent, TextBoxPointerReleased);
                    _textBox.RemoveHandler(LostFocusEvent, TextBoxLostFocus);

                    _textBox.PropertyChanged -= _textBox_PropertyChanged;
                    _textBox.LayoutUpdated -= _textBox_LayoutUpdated;
                }

                _textBox = null;
            }
        }

        private void TextBoxPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Pointer.Type != PointerType.Mouse)
            {
                ShowHandles = true;

                MoveHandlesToSelection();
                EnsureVisible();
            }
        }

        private void _textBox_LayoutUpdated(object? sender, EventArgs e)
        {
            if (ShowHandles)
            {
                MoveHandlesToSelection();
                EnsureVisible();
            }
        }

        private void _textBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (ShowHandles && (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty))
            {
                MoveHandlesToSelection();
                EnsureVisible();
            }
        }

        private void TextBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            ShowHandles = false;
        }

        private void TextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            ShowHandles = false;
        }

        private void TextChanged(object? sender, TextChangingEventArgs e)
        {
            ShowHandles = false;
        }
    }
}
