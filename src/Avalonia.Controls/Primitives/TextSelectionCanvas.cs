using System;
using System.Collections.Generic;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Primitives
{
    internal class TextSelectionHandleCanvas : Canvas
    {
        private const int ContextMenuPadding = 16;
        private static bool s_isInTouchMode;

        private readonly TextSelectionHandle _caretHandle;
        private readonly TextSelectionHandle _handle1;
        private readonly TextSelectionHandle _handle2;
        private TextPresenter? _presenter;
        private TextBox? _textBox;
        private bool _showHandle;
        private IDisposable? _showDisposable;
        private PresenterVisualListener _layoutListener;

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
            _handle1.DragDelta += StartHandle_DragDelta;
            _handle1.DragCompleted += Handle_DragCompleted;
            _handle1.DragStarted += Handle_DragStarted;
            _handle2.DragDelta += EndHandle_DragDelta;
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
            ShowFlyout(e);
            e.Handled = true;
        }

        private void Handle_DragStarted(object? sender, VectorEventArgs e)
        {
            CloseFlyout();
        }

        private void CloseFlyout()
        {
            _presenter?.RaiseEvent(new Interactivity.RoutedEventArgs(InputElement.ContextCanceledEvent));
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
        }

        internal void ShowOnFocused()
        {
            if (s_isInTouchMode)
            {
                MoveHandlesToSelection();
            }
        }

        private IDisposable? BeginChange()
        {
            return _presenter?.CurrentImClient?.BeginChange();
        }

        private void Handle_DragCompleted(object? sender, VectorEventArgs e)
        {
            MoveHandlesToSelection();
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
                    !IsOccluded(_handle1.IndicatorPosition);
                _handle2.IsVisible = ShowHandles && hasSelection &&
                    !IsOccluded(_handle2.IndicatorPosition);
                _caretHandle.IsVisible = ShowHandles && !hasSelection &&
                    !IsOccluded(_caretHandle.IndicatorPosition);

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
            if (_presenter != null && _textBox != null)
            {
                CloseFlyout();

                var position = GetTextPosition(handle);

                var otherHandle = handle == _handle1 ? _handle2 : _handle1;

                var otherPosition = GetTextPosition(otherHandle);

                if (position == otherPosition)
                {
                    position = handle.SelectionHandleType == SelectionHandleType.Start ? position - 1 : position + 1;

                    position = Math.Clamp(position, 0, (_textBox.Text?.Length - 1) ?? 1);
                }

                using var _ = BeginChange();

                if (handle.SelectionHandleType == SelectionHandleType.Start)
                {
                    position = position > _textBox.SelectionEnd ? _textBox.SelectionEnd - 1 : position;
                    _textBox.SetCurrentValue(TextBox.SelectionStartProperty, position);
                }
                else
                {
                    position = position < _textBox.SelectionStart ? _textBox.SelectionStart + 1 : position;
                    _textBox.SetCurrentValue(TextBox.SelectionEndProperty, position);
                }

                otherHandle.InvalidateVisual();

                _presenter.MoveCaretToTextPosition(position);
                var caretBound = _presenter.GetCursorRectangle();
                handle.SetTopLeft(ToLayer(handle.SelectionHandleType == SelectionHandleType.Start ? caretBound.BottomLeft : caretBound.BottomLeft));

                EnsureVisible();
            }

            int GetTextPosition(TextSelectionHandle handle)
            {
                var indicatorPosition = GetSearchPoint(handle);
                var point = ToPresenter(indicatorPosition);
                var hit = _presenter.TextLayout.HitTestPoint(point);
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
            return (_presenter is { } p) ? (p.VisualRoot as Visual)?.TranslatePoint(point, p) ?? point : point;
        }

        public void MoveHandlesToSelection()
        {
            if (_presenter == null
                || _handle1.IsDragging
                || _caretHandle.IsDragging
                || _handle2.IsDragging)
            {
                return;
            }

            var selectionStart = _presenter.SelectionStart;
            var selectionEnd = _presenter.SelectionEnd;
            var hasSelection = selectionStart != selectionEnd;

            var points = _presenter.GetCaretPoints();

            _caretHandle.SetTopLeft(ToLayer(points.Item2));

            if (hasSelection)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                var rects = new List<Rect>(_presenter.TextLayout.HitTestTextRange(start, length));

                if (rects.Count > 0)
                {
                    var first = rects[0];
                    var last = rects[rects.Count - 1];

                    var position = _handle1.SelectionHandleType == SelectionHandleType.Start ? first.BottomLeft : last.BottomRight;
                    _handle1.SetTopLeft(ToLayer(position));

                    position = _handle2.SelectionHandleType == SelectionHandleType.Start ? first.BottomLeft : last.BottomRight;
                    _handle2.SetTopLeft(ToLayer(position));
                }
            }

            ShowHandles = true;

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
                _presenter.AddHandler(PointerPressedEvent, PresenterPressed);
                _presenter.AddHandler(GotFocusEvent, PresenterFocused, handledEventsToo: true);

                _textBox = _presenter.FindAncestorOfType<TextBox>();

                if (_textBox != null)
                {
                    _textBox.PropertyChanged += TextBox_PropertyChanged;
                }
            }
        }

        private void PresenterPressed(object? sender, PointerPressedEventArgs e)
        {
            s_isInTouchMode = e.Pointer.Type != PointerType.Mouse;
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
            s_isInTouchMode = e.Pointer.Type != PointerType.Mouse;

            if (s_isInTouchMode)
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

        internal bool ShowFlyout(ContextRequestedEventArgs e)
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
                        if (_handle1.IsEffectivelyVisible)
                            handle = _handle1;
                        else if (_handle2.IsEffectivelyVisible)
                            handle = _handle2;
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
                        var oldVerticalOffset = flyout.VerticalOffset;
                        var oldHorizontalOffset = flyout.HorizontalOffset;
                        var oldPlacement = flyout.Placement;
                        var topLeft = ToPresenter(handle.GetTopLeft());
                        flyout.VerticalOffset = topLeft.Y - verticalOffset;
                        flyout.HorizontalOffset = topLeft.X;
                        flyout.Placement = PlacementMode.TopEdgeAlignedLeft;
                        _textBox.RaiseEvent(new ContextRequestedEventArgs());
                        flyout.VerticalOffset = oldVerticalOffset;
                        flyout.HorizontalOffset = oldHorizontalOffset;
                        flyout.Placement = oldPlacement;

                        return true;
                    }
                }
                else
                {
                    _textBox.RaiseEvent(new ContextRequestedEventArgs(e));
                }
            }

            return false;
        }

        internal void Show()
        {
            ShowHandles = true;

            MoveHandlesToSelection();
            EnsureVisible();
        }

        private void TextBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (s_isInTouchMode && (e.Property == TextPresenter.SelectionStartProperty ||
                                e.Property == TextPresenter.SelectionEndProperty))
            {
                MoveHandlesToSelection();
                EnsureVisible();
            }
            else if (e.Property == TextPresenter.TextProperty)
            {
                ShowHandles = false;
                if (_presenter?.ContextFlyout is { } flyout && flyout.IsOpen)
                    flyout.Hide();
            }
        }

        private void PresenterKeyDown(object? sender, KeyEventArgs e)
        {
            ShowHandles = false;
            s_isInTouchMode = false;
        }

        private class PresenterVisualListener
        {
            private List<Visual> _attachedVisuals = new List<Visual>();
            private TextPresenter? _presenter;
            private object _lock = new object();

            public event EventHandler? Invalidated;

            public void Attach(TextPresenter presenter)
            {
                lock (_lock)
                {
                    if (_presenter != null)
                        throw new InvalidOperationException("Listener is already attached to a TextPresenter");

                    _presenter = presenter;
                    presenter.SizeChanged += Presenter_SizeChanged;
                    presenter.EffectiveViewportChanged += Visual_EffectiveViewportChanged;


                    void AttachViewportHandler(Visual visual)
                    {
                        if (visual is Layoutable layoutable)
                        {
                            layoutable.EffectiveViewportChanged += Visual_EffectiveViewportChanged;
                        }

                        _attachedVisuals.Add(visual);
                    }

                    var visualParent = presenter.VisualParent;
                    while (visualParent != null)
                    {
                        AttachViewportHandler(visualParent);

                        visualParent = visualParent.VisualParent;
                    }
                }
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
                lock (_lock)
                {
                    if (_presenter is { } presenter)
                    {
                        presenter.SizeChanged -= Presenter_SizeChanged;
                        presenter.EffectiveViewportChanged -= Visual_EffectiveViewportChanged;
                    }

                    foreach (var visual in _attachedVisuals)
                    {
                        if (visual is Layoutable layoutable)
                        {
                            layoutable.EffectiveViewportChanged -= Visual_EffectiveViewportChanged;
                        }
                    }

                    _presenter = null;
                    _attachedVisuals.Clear();
                }
            }

            private void OnInvalidated()
            {
                Invalidated?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
