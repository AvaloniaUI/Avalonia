using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    internal class TextSelectionHandleCanvas : Canvas
    {
        private static readonly bool s_shouldWrapAroundSelection;

        private const int ContextMenuPadding = 16;

        private readonly TextSelectionHandle _caretHandle;
        private readonly TextSelectionHandle _handle1;
        private readonly TextSelectionHandle _handle2;
        private TextPresenter? _presenter;
        private TextBox? _textBox;
        private bool _showHandle;
        private IDisposable? _showDisposable;
        private PresenterVisualListener _layoutListener;
        private int? _savedSelectionStart;
        private int? _saveSelectionEnd;
        private SelectionHandleType? _initialHandleType;
        private bool _isInTouchMode;

        internal bool ShowHandles
        {
            get => _showHandle;
            set
            {
                _showHandle = value;

                if (!value)
                {
                    _handle1.IsVisible = false;
                    _handle2.IsVisible = false;
                    _caretHandle.IsVisible = false;
                }

                IsVisible = !string.IsNullOrEmpty(_presenter?.Text) && value;
            }
        }

        static TextSelectionHandleCanvas()
        {
            s_shouldWrapAroundSelection = !OperatingSystem.IsAndroid();
        }

        public TextSelectionHandleCanvas()
        {
            _caretHandle = new TextSelectionHandle() { SelectionHandleType = SelectionHandleType.Caret };
            _handle1 = new TextSelectionHandle();
            _handle2 = new TextSelectionHandle();

            _caretHandle.SelectionHandleType = SelectionHandleType.Caret;
            _handle1.SelectionHandleType = SelectionHandleType.Start;
            _handle2.SelectionHandleType = SelectionHandleType.End;

            Children.Add(_caretHandle);
            Children.Add(_handle1);
            Children.Add(_handle2);

            _caretHandle.DragStarted += Handle_DragStarted;
            _caretHandle.DragDelta += CaretHandle_DragDelta;
            _caretHandle.DragCompleted += Handle_DragCompleted;
            _handle1.DragDelta += SelectionHandle_DragDelta;
            _handle1.DragCompleted += Handle_DragCompleted;
            _handle1.DragStarted += Handle_DragStarted;
            _handle2.DragDelta += SelectionHandle_DragDelta;
            _handle2.DragCompleted += Handle_DragCompleted;
            _handle2.DragStarted += Handle_DragStarted;

            _handle1.SetTopLeft(default);
            _caretHandle.SetTopLeft(default);
            _handle2.SetTopLeft(default);

            _handle1.ContextCanceled += Caret_ContextCanceled;
            _caretHandle.ContextCanceled += Caret_ContextCanceled;
            _handle2.ContextCanceled += Caret_ContextCanceled;
            _handle1.ContextRequested += Caret_ContextRequested;
            _caretHandle.ContextRequested += Caret_ContextRequested;
            _handle2.ContextRequested += Caret_ContextRequested;

            IsVisible = ShowHandles;

            ClipToBounds = false;

            _layoutListener = new PresenterVisualListener();
            _layoutListener.Invalidated += LayoutListener_Invalidated;
        }

        private void LayoutListener_Invalidated(object? sender, EventArgs e)
        {
            if (ShowHandles)
                MoveHandlesToSelection();
        }

        private void Caret_ContextCanceled(object? sender, RoutedEventArgs e)
        {
            CloseFlyout();
        }

        private void Caret_ContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            ShowFlyout();
            e.Handled = true;
        }

        private void Handle_DragStarted(object? sender, VectorEventArgs e)
        {
            _savedSelectionStart = _textBox?.SelectionStart;
            _saveSelectionEnd = _textBox?.SelectionEnd;
            _initialHandleType = (sender as TextSelectionHandle)?.SelectionHandleType;
            CloseFlyout();
        }

        private void CloseFlyout()
        {
            _textBox?.RaiseEvent(new Interactivity.RoutedEventArgs(InputElement.ContextCanceledEvent));
        }

        private void SelectionHandle_DragDelta(object? sender, VectorEventArgs e)
        {
            if (sender is TextSelectionHandle handle)
            {
                DragSelectionHandle(handle);
                e.Handled = true;
            }
        }

        private void CaretHandle_DragDelta(object? sender, VectorEventArgs e)
        {
            if (_presenter != null && _textBox != null)
            {
                var indicatorPosition = GetSearchPoint(_caretHandle);
                var point = ToPresenter(indicatorPosition);
                using var _ = BeginChange();
                _presenter.MoveCaretToPoint(point);
                var caretIndex = _presenter.CaretIndex;
                _textBox.SetCurrentValue(TextBox.CaretIndexProperty, caretIndex);
                _textBox.SetCurrentValue(TextBox.SelectionStartProperty, caretIndex);
                _textBox.SetCurrentValue(TextBox.SelectionEndProperty, caretIndex);

                var caretBound = _presenter.GetCursorRectangle();
                _caretHandle.SetTopLeft(ToLayer(caretBound.BottomLeft));
            }
        }

        public void Hide()
        {
            ShowHandles = false;
            _isInTouchMode = false;
        }

        internal void Show(bool forceTouchMode = false)
        {
            _isInTouchMode = _isInTouchMode || forceTouchMode;
            if (_isInTouchMode)
            {
                ShowHandles = true;
                MoveHandlesToSelection();

                CheckStateAndShowFlyout();
            }
        }

        private IDisposable? BeginChange()
        {
            return _presenter?.CurrentImClient?.BeginChange();
        }

        private void Handle_DragCompleted(object? sender, VectorEventArgs e)
        {
            _savedSelectionStart = -1;
            _saveSelectionEnd = -1;

            if (!_handle1.IsDragging)
            {
                _handle1.NeedsIndicatorUpdate = true;
                _handle1.SelectionHandleType = SelectionHandleType.Start;
            }

            if (!_handle2.IsDragging)
            {
                _handle2.NeedsIndicatorUpdate = true;
                _handle2.SelectionHandleType = SelectionHandleType.End;
            }

            MoveHandlesToSelection();
            CheckStateAndShowFlyout();
        }

        private void CheckStateAndShowFlyout()
        {
            if (_textBox?.SelectionStart != _textBox?.SelectionEnd && !(_handle1.IsDragging || _handle2.IsDragging))
            {
                // Show flyout if there's a selection
                ShowFlyout();
            }
        }

        private void EnsureVisible()
        {
            _showDisposable?.Dispose();
            _showDisposable = null;

            if (_presenter is { } presenter && presenter.VisualRoot is InputElement root)
            {
                var bounds = presenter.GetTransformedBounds();

                if (bounds == null)
                    return;

                var clip = bounds.Value.Clip.Inflate(_textBox?.Padding ?? new Thickness(4, 0));

                var isSelectionDragging = _handle1.IsDragging || _handle2.IsDragging;

                var hasSelection = _presenter.SelectionStart != _presenter.SelectionEnd || isSelectionDragging;

                _handle1.IsVisible = ShowHandles && hasSelection &&
                    (_handle1.IsDragging || !IsOccluded(_handle1.IndicatorPosition));
                _handle2.IsVisible = ShowHandles && hasSelection &&
                    (_handle2.IsDragging || !IsOccluded(_handle2.IndicatorPosition));
                _caretHandle.IsVisible = ShowHandles && (!hasSelection) &&
                    (_caretHandle.IsDragging || !IsOccluded(_caretHandle.IndicatorPosition));

                bool IsOccluded(Point point)
                {
                    return !clip.Contains(point);
                }

                if (ShowHandles && !hasSelection)
                {
                    _showDisposable = DispatcherTimer.RunOnce(() =>
                        {
                            ShowHandles = false;
                            _showDisposable?.Dispose();
                        }, TimeSpan.FromSeconds(5), DispatcherPriority.Background);
                }
            }
        }

        private Point GetSearchPoint(TextSelectionHandle handle)
        {
            if (_presenter == null)
                return default;

            var caretBounds = _presenter.GetCursorRectangle();
            var searchOffset = caretBounds.Height / 2;
            var indicator = handle.IndicatorPosition;
            return indicator.WithY(indicator.Y - searchOffset);
        }

        private void DragSelectionHandle(TextSelectionHandle handle)
        {
            if (_presenter is { } presenter && _textBox is { } textbox)
            {
                CloseFlyout();

                var position = GetTextPosition(handle);

                var otherHandle = handle == _handle1 ? _handle2 : _handle1;

                using var _ = BeginChange();

                // Some platforms do not allow handles to cause selection bounds to reverse, i.e. allow handles to move over each other.
                if (!s_shouldWrapAroundSelection)
                {
                    if (handle.SelectionHandleType == SelectionHandleType.Start)
                    {
                        position = position >= textbox.SelectionEnd ? textbox.SelectionEnd - 1 : position;
                        textbox.SetCurrentValue(TextBox.SelectionStartProperty, position);
                    }
                    else
                    {
                        position = position <= textbox.SelectionStart ? textbox.SelectionStart + 1 : position;
                        textbox.SetCurrentValue(TextBox.SelectionEndProperty, position);
                    }
                }
                else
                {
                    // For platforms that do, update the handle types for each and adjust selection.

                    if (_initialHandleType == SelectionHandleType.Start)
                    {
                        // If handle was previously the selection end, set new selection end to the previous selection start.
                        bool hasPositionSwapped = position > _saveSelectionEnd;
                        var otherPosition = _saveSelectionEnd;

                        if (position == otherPosition)
                        {
                            position = hasPositionSwapped ? Math.Min(position + 1, (textbox.Text?.Length - 1) ?? 0)
                                : Math.Max(position - 1, 0);
                        }

                        textbox.SetCurrentValue(TextBox.SelectionStartProperty, hasPositionSwapped ? otherPosition : position);
                        textbox.SetCurrentValue(TextBox.SelectionEndProperty, hasPositionSwapped ? position : otherPosition);

                        otherHandle.SelectionHandleType = hasPositionSwapped ? SelectionHandleType.Start : SelectionHandleType.End;
                    }
                    else
                    {
                        // If handle was previously the selection start, set new selection start to the previous selection end.
                        bool hasPositionSwapped = position < _savedSelectionStart;
                        var otherPosition = _savedSelectionStart;

                        if (position == otherPosition)
                        {
                            position = !hasPositionSwapped ? Math.Min(position + 1, (textbox.Text?.Length - 1) ?? 0)
                                : Math.Max(position - 1, 0);
                        }

                        textbox.SetCurrentValue(TextBox.SelectionStartProperty, hasPositionSwapped ? position : otherPosition);
                        textbox.SetCurrentValue(TextBox.SelectionEndProperty, hasPositionSwapped ? otherPosition : position);

                        otherHandle.SelectionHandleType = hasPositionSwapped ? SelectionHandleType.End : SelectionHandleType.Start;
                    }
                }

                presenter.MoveCaretToTextPosition(position);
                var caretBound = presenter.GetCursorRectangle();
                handle.SetTopLeft(ToLayer(handle.SelectionHandleType == SelectionHandleType.Start ? caretBound.BottomLeft : caretBound.BottomLeft));

                MoveHandlesToSelection();
            }

            int GetTextPosition(TextSelectionHandle handle)
            {
                var indicatorPosition = GetSearchPoint(handle);
                var point = ToPresenter(indicatorPosition);
                var hit = presenter.TextLayout.HitTestPoint(point);
                var position = hit.CharacterHit.FirstCharacterIndex + hit.CharacterHit.TrailingLength;
                return position;
            }
        }

        private Point ToLayer(Point point)
        {
            return (_presenter?.VisualRoot is Visual v) ? _presenter?.TranslatePoint(point, v) ?? point : point;
        }

        private Point ToPresenter(Point point)
        {
            return (_presenter is { } presenter) ? (presenter.VisualRoot as Visual)?.TranslatePoint(point, presenter) ?? point : point;
        }

        private Point ToTextBox(Point point)
        {
            return (_textBox is { } textbox) ? (textbox.VisualRoot as Visual)?.TranslatePoint(point, textbox) ?? point : point;
        }

        public void MoveHandlesToSelection()
        {
            if (_presenter == null)
            {
                return;
            }

            var selectionStart = _presenter.SelectionStart;
            var selectionEnd = _presenter.SelectionEnd;
            var hasSelection = selectionStart != selectionEnd;

            if (!_caretHandle.IsDragging)
            {
                var points = _presenter.GetCaretPoints();

                _caretHandle.SetTopLeft(ToLayer(points.Item2));
            }

            if (hasSelection)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var end = Math.Max(selectionStart, selectionEnd);

                var startPoint = GetPosition(start, true);
                var endPoint = GetPosition(end - 1, false);

                if (!_handle1.IsDragging)
                {
                    var pos = _handle1.SelectionHandleType == SelectionHandleType.Start ? startPoint : endPoint;
                    if (pos.isRtl != _handle1.IsRtl)
                        _handle1.NeedsIndicatorUpdate = true;
                    _handle1.IsRtl = pos.isRtl;
                    _handle1.SetTopLeft(ToLayer(pos.position));
                }

                if (!_handle2.IsDragging)
                {
                    var pos = _handle2.SelectionHandleType == SelectionHandleType.Start ? startPoint : endPoint;
                    if (pos.isRtl != _handle2.IsRtl)
                        _handle2.NeedsIndicatorUpdate = true;
                    _handle2.IsRtl = pos.isRtl;
                    _handle2.SetTopLeft(ToLayer(pos.position));
                }

                (Point position, bool isRtl) GetPosition(int index, bool start)
                {
                    var rect = new List<Rect>(_presenter!.TextLayout.HitTestTextRange(index, 1)).FirstOrDefault();

                    var lineIndex = _presenter.TextLayout.GetLineIndexFromCharacterIndex(index, false);
                    var textLine = _presenter.TextLayout.TextLines[lineIndex];
                    var lineStart = textLine.FirstTextSourceIndex;
                    var characterLineIndex = Math.Max(0, index - lineStart);
                    TextRun? run = null;
                    int searchLength = 0;

                    for (var i = 0; i < textLine.TextRuns.Count; i++)
                    {
                        run = textLine.TextRuns[i];

                        searchLength += run.Length;
                        if (searchLength > characterLineIndex)
                            break;
                    }

                    bool isRtl;
                    var reversed = isRtl = !((run as ShapedTextRun)?.ShapedBuffer.IsLeftToRight ?? true);
                    if (!start)
                        reversed = !reversed;

                    return (reversed ? rect.BottomRight : rect.BottomLeft, isRtl);
                }
            }

            EnsureVisible();
        }

        internal void SetPresenter(TextPresenter? textPresenter)
        {
            if (_presenter == textPresenter)
                return;

            if (_presenter != null)
            {
                _layoutListener.Detach();
                _presenter.RemoveHandler(KeyDownEvent, PresenterKeyDown);
                _presenter.RemoveHandler(TappedEvent, PresenterTapped);
                _presenter.RemoveHandler(PointerPressedEvent, PresenterPressed);
                _presenter.RemoveHandler(GotFocusEvent, PresenterFocused);

                if (_textBox != null)
                {
                    _textBox.PropertyChanged -= TextBox_PropertyChanged;
                    _textBox.RemoveHandler(ScrollGestureEvent, TextBoxScrolling);
                }
                _textBox = null;

                _presenter = null;
            }

            _presenter = textPresenter;
            if (_presenter != null)
            {
                _layoutListener.Attach(_presenter);
                _presenter.AddHandler(KeyDownEvent, PresenterKeyDown, handledEventsToo: true);
                _presenter.AddHandler(TappedEvent, PresenterTapped);
                _presenter.AddHandler(PointerPressedEvent, PresenterPressed, handledEventsToo: true);
                _presenter.AddHandler(GotFocusEvent, PresenterFocused, handledEventsToo: true);

                _textBox = _presenter.FindAncestorOfType<TextBox>();

                if (_textBox != null)
                {
                    _textBox.PropertyChanged += TextBox_PropertyChanged;
                    _textBox.AddHandler(ScrollGestureEvent, TextBoxScrolling, handledEventsToo: true);
                }
            }
        }

        private void TextBoxScrolling(object? sender, ScrollGestureEventArgs e)
        {
            CloseFlyout();
        }

        private void PresenterPressed(object? sender, PointerPressedEventArgs e)
        {
            _isInTouchMode = e.Pointer.Type != PointerType.Mouse;
        }

        private void PresenterFocused(object? sender, FocusChangedEventArgs e)
        {
            if (_presenter != null && _presenter.SelectionStart != _presenter.SelectionEnd)
            {
                ShowHandles = true;
                EnsureVisible();
            }
        }

        private void PresenterTapped(object? sender, TappedEventArgs e)
        {
            _isInTouchMode = e.Pointer.Type != PointerType.Mouse;

            if (_isInTouchMode)
                MoveHandlesToSelection();
            else
            {
                ShowHandles = false;
                _showDisposable?.Dispose();
                _showDisposable = null;
            }
        }

        private void Presenter_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            InvalidateMeasure();
        }

        internal bool ShowFlyout()
        {
            if (_textBox != null)
            {
                if (_textBox.ContextFlyout is PopupFlyoutBase flyout)
                {
                    var verticalOffset = (double.IsNaN(_textBox.LineHeight) ? _textBox.FontSize : _textBox.LineHeight) +
                                         ContextMenuPadding;

                    Point? topleft = default;

                    if (_textBox.SelectionStart != _textBox.SelectionEnd)
                    {
                        if (_handle1.IsEffectivelyVisible && _handle2.IsEffectivelyVisible)
                        {
                            var p1 = _handle1.IndicatorPosition;
                            var p2 = _handle2.IndicatorPosition;

                            topleft = new Point((p1.X + p2.X) / 2, Math.Min(p1.Y, p2.Y));
                        }
                        else
                        {
                            var visibleHandle = _handle1.IsEffectivelyVisible ? _handle1: _handle2.IsEffectivelyVisible ? _handle2 : null;

                            topleft = visibleHandle?.IndicatorPosition;
                        }
                    }
                    else
                    {
                        if (_caretHandle.IsEffectivelyVisible)
                        {
                            topleft = _caretHandle.IndicatorPosition;
                        }
                    }

                    if (topleft != null)
                    {
                        var oldPlacement = flyout.Placement;
                        var oldCallback = flyout.CustomPopupPlacementCallback;
                        var oldShowMode = flyout.ShowMode;
                        var point = ToTextBox(topleft.Value);
                        point = point.WithY(Math.Max(0, point.Y));


                        flyout.CustomPopupPlacementCallback = (x) => Place(x);
                        flyout.Placement = PlacementMode.Custom;
                        flyout.ShowMode = FlyoutShowMode.Transient;

                        _textBox.RaiseEvent(new ContextRequestedEventArgs());

                        flyout.Placement = oldPlacement;
                        flyout.CustomPopupPlacementCallback = oldCallback;
                        flyout.ShowMode = oldShowMode;

                        void Place(CustomPopupPlacement parameters)
                        {
                            parameters.Anchor = PopupAnchor.TopLeft;
                            var offset = parameters.Offset;
                            parameters.Offset = offset.WithX(point.X)
                                .WithY(point.Y - verticalOffset);
                        }
                        return true;
                    }
                }
            }

            return false;
        }

        private void TextBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextPresenter.TextProperty)
            {
                ShowHandles = false;
                CloseFlyout();
            }
            else if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty
                || e.Property == TextBox.CaretIndexProperty)
            {
                MoveHandlesToSelection();
            }
        }

        private void PresenterKeyDown(object? sender, KeyEventArgs e)
        {
            ShowHandles = false;
            _isInTouchMode = false;
        }

        // Listener to layout changes for presenter.
        private class PresenterVisualListener
        {
            private TextPresenter? _presenter;

            public event EventHandler? Invalidated;

            public void Attach(TextPresenter presenter)
            {
                if (_presenter != null)
                    throw new InvalidOperationException("Listener is already attached to a TextPresenter");

                _presenter = presenter;
                presenter.SizeChanged += Presenter_SizeChanged;
                presenter.EffectiveViewportChanged += Visual_EffectiveViewportChanged;
            }

            private void Visual_EffectiveViewportChanged(object? sender, Layout.EffectiveViewportChangedEventArgs e)
            {
                OnInvalidated();
            }

            private void Presenter_SizeChanged(object? sender, SizeChangedEventArgs e)
            {
                OnInvalidated();
            }

            public void Detach()
            {
                if (_presenter is { } presenter)
                {
                    presenter.SizeChanged -= Presenter_SizeChanged;
                    presenter.EffectiveViewportChanged -= Visual_EffectiveViewportChanged;
                }

                _presenter = null;
            }

            private void OnInvalidated()
            {
                Invalidated?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
