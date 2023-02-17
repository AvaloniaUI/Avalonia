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
        private TextSelectionHandle _caretThumb;
        private TextSelectionHandle _startThumb;
        private TextSelectionHandle _endThumb;
        private TextPresenter? _presenter;
        private TextBox? _textBox;
        private bool _showThumbs;

        public bool ShowThumbs
        {
            get => _showThumbs; set
            {
                _showThumbs = value;

                if (!value)
                {
                    _startThumb.IsVisible = false;
                    _endThumb.IsVisible = false;
                    _caretThumb.IsVisible = false;
                }

                IsVisible = value;
            }
        }

        public TextSelectionCanvas()
        {
            _caretThumb = new TextSelectionHandle()
            {
                SelectionHandleType = SelectionHandleType.Caret
            };
            _startThumb = new TextSelectionHandle();
            _endThumb = new TextSelectionHandle();

            ClipToBounds = true;

            Children.Add(_caretThumb);
            Children.Add(_startThumb);
            Children.Add(_endThumb);

            _caretThumb.DragDelta += CaretThumb_DragDelta;
            _caretThumb.DragCompleted += Thumb_DragCompleted;

            _startThumb.DragDelta += StartThumb_DragDelta;
            _startThumb.DragCompleted += Thumb_DragCompleted;
            _endThumb.DragDelta += EndThumb_DragDelta;
            _endThumb.DragCompleted += Thumb_DragCompleted;

            _caretThumb.Classes.Add("caret");
            _startThumb.Classes.Add("start");
            _endThumb.Classes.Add("end");

            _startThumb.SetTopLeft(default);
            _caretThumb.SetTopLeft(default);
            _endThumb.SetTopLeft(default);

            IsVisible = ShowThumbs;
        }

        private void EndThumb_DragDelta(object? sender, VectorEventArgs e)
        {
            if (sender is TextSelectionHandle handle)
                DragSelectionHandle(handle);
        }

        private void StartThumb_DragDelta(object? sender, VectorEventArgs e)
        {
            if (sender is TextSelectionHandle handle)
                DragSelectionHandle(handle);
        }

        private void CaretThumb_DragDelta(object? sender, Input.VectorEventArgs e)
        {
            if (_presenter != null && _textBox != null)
            {
                var point = ToPresenter(_caretThumb.IndicatorPosition);
                _presenter.MoveCaretToPoint(point);
                _textBox.SelectionStart = _textBox.SelectionEnd = _presenter.CaretIndex;
                var points = _presenter.GetCaretPoints();

                _caretThumb?.SetTopLeft(ToLayer(points.Item2));
            }
        }

        private void Thumb_DragCompleted(object? sender, Input.VectorEventArgs e)
        {
            MoveThumbsToSelection();
        }

        private void EnsureVisible()
        {
            if (_textBox is { } t && t.VisualRoot is Visual r)
            {
                var bounds = t.Bounds;
                var topLeft = t.TranslatePoint(default, r as Visual) ?? default;
                bounds = bounds.WithX(topLeft.X).WithY(topLeft.Y);

                var hasSelection = _textBox.SelectionStart != _textBox.SelectionEnd;

                _startThumb.IsVisible = bounds.Contains(new Point(Canvas.GetLeft(_startThumb), Canvas.GetTop(_startThumb))) && ShowThumbs && hasSelection;
                _endThumb.IsVisible = bounds.Contains(new Point(Canvas.GetLeft(_endThumb), Canvas.GetTop(_endThumb))) && ShowThumbs && hasSelection;
                _caretThumb.IsVisible = bounds.Contains(new Point(Canvas.GetLeft(_caretThumb), Canvas.GetTop(_caretThumb))) && ShowThumbs && !hasSelection;
            }
        }

        private void DragSelectionHandle(TextSelectionHandle thumb)
        {
            if (_presenter != null && _textBox != null)
            {
                var point = ToPresenter(thumb.IndicatorPosition);
                point = point.WithY(point.Y - _presenter.FontSize / 2);
                var hit = _presenter.TextLayout.HitTestPoint(point);
                var caret = hit.CharacterHit.FirstCharacterIndex + hit.CharacterHit.TrailingLength;

                var otherThumb = thumb == _startThumb ? _endThumb : _startThumb;

                if (thumb.SelectionHandleType == SelectionHandleType.Start)
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

                    if (thumb.SelectionHandleType == SelectionHandleType.Start)
                        thumb?.SetTopLeft(ToLayer(first.BottomLeft));
                    else
                        thumb?.SetTopLeft(ToLayer(last.BottomRight));

                    if (otherThumb.SelectionHandleType == SelectionHandleType.Start)
                        otherThumb?.SetTopLeft(ToLayer(first.BottomLeft));
                    else
                        otherThumb?.SetTopLeft(ToLayer(last.BottomRight));
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

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            AdornerLayer.SetIsClipEnabled(this, false);

            ClipToBounds = false;
        }

        public void MoveThumbsToSelection()
        {
            if (_presenter == null || _textBox == null || _startThumb.IsDragging || _endThumb.IsDragging)
            {
                return;
            }

            var hasSelection = _textBox.SelectionStart != _textBox.SelectionEnd;
            if (_caretThumb != null)
            {
                var points = _presenter.GetCaretPoints();

                _caretThumb.SetTopLeft(ToLayer(points.Item2));
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

                    if (_startThumb != null && !_startThumb.IsDragging)
                    {
                        _startThumb.SetTopLeft(ToLayer(first.BottomLeft));
                        _startThumb.SelectionHandleType = selectionStart < selectionEnd ? SelectionHandleType.Start : SelectionHandleType.End;
                    }

                    if (_endThumb != null && !_endThumb.IsDragging)
                    {
                        _endThumb.SetTopLeft(ToLayer(last.BottomRight));
                        _endThumb.SelectionHandleType = selectionStart > selectionEnd ? SelectionHandleType.Start : SelectionHandleType.End;
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
                    _textBox.AddHandler(TextBox.KeyDownEvent, TextBoxKeyDown, handledEventsToo: true);
                    _textBox.AddHandler(TextBox.LostFocusEvent, TextBoxLostFocus, handledEventsToo: true);
                    _textBox.AddHandler(TextBox.DoubleTappedEvent, TextBoxDoubleTapped);

                    _textBox.PropertyChanged += _textBox_PropertyChanged;
                    _textBox.LayoutUpdated += _textBox_LayoutUpdated;
                }
            }
            else
            {
                if (_textBox != null)
                {
                    _textBox.RemoveHandler(TextBox.TextChangingEvent, TextChanged);
                    _textBox.RemoveHandler(TextBox.KeyDownEvent, TextBoxKeyDown);
                    _textBox.RemoveHandler(TextBox.DoubleTappedEvent, TextBoxDoubleTapped);
                    _textBox.RemoveHandler(TextBox.LostFocusEvent, TextBoxLostFocus);

                    _textBox.PropertyChanged -= _textBox_PropertyChanged;
                    _textBox.LayoutUpdated -= _textBox_LayoutUpdated;
                }

                _textBox = null;
            }
        }

        private void _textBox_LayoutUpdated(object? sender, EventArgs e)
        {
            MoveThumbsToSelection();
            EnsureVisible();
        }

        private void _textBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
            {
                MoveThumbsToSelection();
                EnsureVisible();
            }
        }

        private void TextBoxLostFocus(object? sender, RoutedEventArgs e)
        {
            ShowThumbs = false;

            MoveThumbsToSelection();
        }

        private void TextBoxDoubleTapped(object? sender, TappedEventArgs e)
        {
            ShowThumbs = true;

            MoveThumbsToSelection();
        }

        private void TextBoxKeyDown(object? sender, KeyEventArgs e)
        {
            ShowThumbs = false;
        }

        private void TextChanged(object? sender, TextChangingEventArgs e)
        {
            ShowThumbs = false;
        }
    }
}
