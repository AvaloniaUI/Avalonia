using System;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    internal class TextSelectionHandleCanvas : Canvas
    {
        private const int ContextMenuPadding = 16;

        private readonly TextSelectionHandle _caretHandle;
        private readonly TextSelectionHandle _startHandle;
        private readonly TextSelectionHandle _endHandle;
        private TextPresenter? _presenter;
        private TextBox? _textBox;
        private bool _showHandle;

        internal bool ShowHandles
        {
            get => _showHandle;
            set
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

        public TextSelectionHandleCanvas()
        {
            _caretHandle = new TextSelectionHandle() { SelectionHandleType = SelectionHandleType.Caret };
            _startHandle = new TextSelectionHandle();
            _endHandle = new TextSelectionHandle();

            Children.Add(_caretHandle);
            Children.Add(_startHandle);
            Children.Add(_endHandle);

            _caretHandle.DragStarted += Handle_DragStarted;
            _caretHandle.DragDelta += CaretHandle_DragDelta;
            _caretHandle.DragCompleted += Handle_DragCompleted;
            _startHandle.DragDelta += StartHandle_DragDelta;
            _startHandle.DragCompleted += Handle_DragCompleted;
            _startHandle.DragStarted += Handle_DragStarted;
            _endHandle.DragDelta += EndHandle_DragDelta;
            _endHandle.DragCompleted += Handle_DragCompleted;
            _endHandle.DragStarted += Handle_DragStarted;

            _caretHandle.Classes.Add("caret");
            _startHandle.Classes.Add("start");
            _endHandle.Classes.Add("end");

            _startHandle.SetTopLeft(default);
            _caretHandle.SetTopLeft(default);
            _endHandle.SetTopLeft(default);

            IsVisible = ShowHandles;

            ClipToBounds = false;
        }

        private void Handle_DragStarted(object? sender, VectorEventArgs e)
        {
            if (_textBox?.ContextFlyout is { } flyout)
            {
                flyout.Hide();
            }
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

        private void CaretHandle_DragDelta(object? sender, VectorEventArgs e)
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

        private void Handle_DragCompleted(object? sender, VectorEventArgs e)
        {
            MoveHandlesToSelection();

            ShowContextMenu();
        }

        private void EnsureVisible()
        {
            if (_textBox is { } t && t.VisualRoot is Visual r)
            {
                var bounds = t.Bounds;
                var topLeft = t.TranslatePoint(default, r) ?? default;
                bounds = bounds.WithX(topLeft.X).WithY(topLeft.Y);

                var hasSelection = _textBox.SelectionStart != _textBox.SelectionEnd;

                _startHandle.IsVisible = bounds.Contains(new Point(GetLeft(_startHandle), GetTop(_startHandle))) &&
                                         ShowHandles && hasSelection;
                _endHandle.IsVisible = bounds.Contains(new Point(GetLeft(_endHandle), GetTop(_endHandle))) &&
                                       ShowHandles && hasSelection;
                _caretHandle.IsVisible = bounds.Contains(new Point(GetLeft(_caretHandle), GetTop(_caretHandle))) &&
                                         ShowHandles && !hasSelection;
            }
        }

        private void DragSelectionHandle(TextSelectionHandle handle)
        {
            if (_presenter != null && _textBox != null)
            {
                if (_textBox.ContextFlyout is { } flyout)
                {
                    flyout.Hide();
                }

                var point = ToPresenter(handle.IndicatorPosition);
                point = point.WithY(point.Y - _presenter.FontSize / 2);
                var hit = _presenter.TextLayout.HitTestPoint(point);
                var position = hit.CharacterHit.FirstCharacterIndex + hit.CharacterHit.TrailingLength;

                var otherHandle = handle == _startHandle ? _endHandle : _startHandle;

                if (handle.SelectionHandleType == SelectionHandleType.Start)
                {
                    if (position >= _textBox.SelectionEnd)
                        position = _textBox.SelectionEnd - 1;
                        _textBox.SelectionStart = position;
                }
                else
                {
                    if (position <= _textBox.SelectionStart)
                        position = _textBox.SelectionStart + 1;
                    _textBox.SelectionEnd = position;
                }

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

                _presenter?.MoveCaretToTextPosition(position);
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

        private Point ToTextBox(Point point)
        {
            return (_textBox is { } p) ? (p.VisualRoot as Visual)?.TranslatePoint(point, p) ?? point : point;
        }

        public void MoveHandlesToSelection()
        {
            if (_presenter == null || _textBox == null || _startHandle.IsDragging || _endHandle.IsDragging)
            {
                return;
            }

            var hasSelection = _textBox.SelectionStart != _textBox.SelectionEnd;

            var points = _presenter.GetCaretPoints();

            _caretHandle.SetTopLeft(ToLayer(points.Item2));

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

                    if (!_startHandle.IsDragging)
                    {
                        _startHandle.SetTopLeft(ToLayer(first.BottomLeft));
                        _startHandle.SelectionHandleType = selectionStart < selectionEnd ?
                            SelectionHandleType.Start :
                            SelectionHandleType.End;
                    }

                    if (!_endHandle.IsDragging)
                    {
                        _endHandle.SetTopLeft(ToLayer(last.BottomRight));
                        _endHandle.SelectionHandleType = selectionStart > selectionEnd ?
                            SelectionHandleType.Start :
                            SelectionHandleType.End;
                    }
                }
            }
        }

        internal void SetPresenter(TextPresenter? textPresenter)
        {
            if (_presenter == textPresenter)
                return;
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
                    _textBox.AddHandler(Gestures.HoldingEvent, TextBoxHolding, handledEventsToo: true);

                    _textBox.PropertyChanged += TextBoxPropertyChanged;
                    _textBox.EffectiveViewportChanged += TextBoxEffectiveViewportChanged;
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
                    _textBox.RemoveHandler(Gestures.HoldingEvent, TextBoxHolding);

                    _textBox.PropertyChanged -= TextBoxPropertyChanged;
                    _textBox.EffectiveViewportChanged -= TextBoxEffectiveViewportChanged;
                }

                _textBox = null;
            }
        }

        private void TextBoxEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            if (ShowHandles)
            {
                MoveHandlesToSelection();
                EnsureVisible();
            }
        }

        private void TextBoxHolding(object? sender, HoldingRoutedEventArgs e)
        {
            if (ShowContextMenu())
                e.Handled = true;
        }

        internal bool ShowContextMenu()
        {
            if (_textBox != null)
            {
                if (_textBox.ContextFlyout is PopupFlyoutBase flyout)
                {
                    var verticalOffset = (double.IsNaN(_textBox.LineHeight) ? _textBox.FontSize : _textBox.LineHeight) +
                                         ContextMenuPadding;

                    TextSelectionHandle? handle = null;

                    if (_textBox.SelectionStart != _textBox.SelectionEnd)
                    {
                        if (_startHandle.IsEffectivelyVisible)
                            handle = _startHandle;
                        else if (_endHandle.IsEffectivelyVisible)
                            handle = _endHandle;
                    }
                    else
                    {
                        if (_caretHandle.IsEffectivelyVisible)
                        {
                            handle = _caretHandle;
                        }
                    }

                    if (handle != null)
                    {
                        var topLeft = ToTextBox(handle.GetTopLeft());
                        flyout.VerticalOffset = topLeft.Y - verticalOffset;
                        flyout.HorizontalOffset = topLeft.X;
                        flyout.Placement = PlacementMode.TopEdgeAlignedLeft;
                        _textBox.RaiseEvent(new ContextRequestedEventArgs());

                        return true;
                    }
                }
                else
                {
                    _textBox.RaiseEvent(new ContextRequestedEventArgs());
                }
            }

            return false;
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

        private void TextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (ShowHandles && (e.Property == TextBox.SelectionStartProperty ||
                                e.Property == TextBox.SelectionEndProperty))
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
            if (_textBox?.ContextFlyout is { } flyout && flyout.IsOpen)
                flyout.Hide();
        }
    }
}
