using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control that displays a block of formatted text.
    /// </summary>
    public class SelectableTextBlock : TextBlock, IInlineHost
    {
        public static readonly DirectProperty<SelectableTextBlock, int> SelectionStartProperty =
            AvaloniaProperty.RegisterDirect<SelectableTextBlock, int>(
                nameof(SelectionStart),
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<SelectableTextBlock, int> SelectionEndProperty =
            AvaloniaProperty.RegisterDirect<SelectableTextBlock, int>(
                nameof(SelectionEnd),
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        public static readonly DirectProperty<SelectableTextBlock, string> SelectedTextProperty =
            AvaloniaProperty.RegisterDirect<SelectableTextBlock, string>(
                nameof(SelectedText),
                o => o.SelectedText);

        public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
            AvaloniaProperty.Register<SelectableTextBlock, IBrush?>(nameof(SelectionBrush), Brushes.Blue);


        public static readonly DirectProperty<SelectableTextBlock, bool> CanCopyProperty =
            AvaloniaProperty.RegisterDirect<SelectableTextBlock, bool>(
                nameof(CanCopy),
                o => o.CanCopy);

        public static readonly RoutedEvent<RoutedEventArgs> CopyingToClipboardEvent =
            RoutedEvent.Register<SelectableTextBlock, RoutedEventArgs>(
                nameof(CopyingToClipboard), RoutingStrategies.Bubble);

        private bool _canCopy;
        private int _selectionStart;
        private int _selectionEnd;
        private int _wordSelectionStart = -1;

        static SelectableTextBlock()
        {
            FocusableProperty.OverrideDefaultValue(typeof(SelectableTextBlock), true);
            AffectsRender<SelectableTextBlock>(SelectionStartProperty, SelectionEndProperty, SelectionBrushProperty);
        }

        public event EventHandler<RoutedEventArgs>? CopyingToClipboard
        {
            add => AddHandler(CopyingToClipboardEvent, value);
            remove => RemoveHandler(CopyingToClipboardEvent, value);
        }

        /// <summary>
        /// Gets or sets the brush that highlights selected text.
        /// </summary>
        public IBrush? SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets a character index for the beginning of the current selection.
        /// </summary>
        public int SelectionStart
        {
            get => _selectionStart;
            set
            {
                if (SetAndRaise(SelectionStartProperty, ref _selectionStart, value))
                {
                    RaisePropertyChanged(SelectedTextProperty, "", "");

                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets or sets a character index for the end of the current selection.
        /// </summary>
        public int SelectionEnd
        {
            get => _selectionEnd;
            set
            {
                if (SetAndRaise(SelectionEndProperty, ref _selectionEnd, value))
                {
                    RaisePropertyChanged(SelectedTextProperty, "", "");

                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// Gets the content of the current selection.
        /// </summary>
        public string SelectedText
        {
            get => GetSelection();
        }

        /// <summary>
        /// Property for determining if the Copy command can be executed.
        /// </summary>
        public bool CanCopy
        {
            get => _canCopy;
            private set => SetAndRaise(CanCopyProperty, ref _canCopy, value);
        }

        /// <summary>
        /// Copies the current selection to the Clipboard.
        /// </summary>
        public async void Copy()
        {
            if (!_canCopy)
            {
                return;
            }

            var text = GetSelection();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var eventArgs = new RoutedEventArgs(CopyingToClipboardEvent);

            RaiseEvent(eventArgs);

            if (!eventArgs.Handled)
            {
                await ((IClipboard)AvaloniaLocator.Current.GetRequiredService(typeof(IClipboard)))
                    .SetTextAsync(text);
            }
        }        

        /// <summary>
        /// Select all text in the TextBox
        /// </summary>
        public void SelectAll()
        {
            var text = Text;

            SelectionStart = 0;
            SelectionEnd = text?.Length ?? 0;
        }

        /// <summary>
        /// Clears the current selection/>
        /// </summary>
        public void ClearSelection()
        {
            SelectionEnd = SelectionStart;
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            UpdateCommandStates();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if ((ContextFlyout == null || !ContextFlyout.IsOpen) &&
               (ContextMenu == null || !ContextMenu.IsOpen))
            {
                ClearSelection();
            }

            UpdateCommandStates();
        }

        protected override void RenderTextLayout(DrawingContext context, Point origin)
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var selectionBrush = SelectionBrush;

            if (selectionStart != selectionEnd && selectionBrush != null)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                var rects = TextLayout.HitTestTextRange(start, length);

                using (context.PushPostTransform(Matrix.CreateTranslation(origin)))
                {
                    foreach (var rect in rects)
                    {
                        context.FillRectangle(selectionBrush, PixelRect.FromRect(rect, 1).ToRect(1));
                    }
                }
            }

            base.RenderTextLayout(context, origin);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            var handled = false;
            var modifiers = e.KeyModifiers;
            var keymap = AvaloniaLocator.Current.GetRequiredService<PlatformHotkeyConfiguration>();

            bool Match(List<KeyGesture> gestures) => gestures.Any(g => g.Matches(e));

            if (Match(keymap.Copy))
            {
                Copy();
                handled = true;
            }
            else if (Match(keymap.SelectAll))
            {
                SelectAll();
                handled = true;
            }

            e.Handled = handled;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            var text = Text;
            var clickInfo = e.GetCurrentPoint(this);

            if (text != null && clickInfo.Properties.IsLeftButtonPressed)
            {
                var padding = Padding;

                var point = e.GetPosition(this) - new Point(padding.Left, padding.Top);

                var clickToSelect = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

                var oldIndex = SelectionStart;

                var hit = TextLayout.HitTestPoint(point);
                var index = hit.TextPosition;

                switch (e.ClickCount)
                {
                    case 1:
                        if (clickToSelect)
                        {
                            if (_wordSelectionStart >= 0)
                            {
                                var previousWord = StringUtils.PreviousWord(text, index);

                                if (index > _wordSelectionStart)
                                {
                                    SelectionEnd = StringUtils.NextWord(text, index);
                                }

                                if (index < _wordSelectionStart || previousWord == _wordSelectionStart)
                                {
                                    SelectionStart = previousWord;
                                }
                            }
                            else
                            {
                                SelectionStart = Math.Min(oldIndex, index);
                                SelectionEnd = Math.Max(oldIndex, index);
                            }
                        }
                        else
                        {
                            if (_wordSelectionStart == -1 || index < SelectionStart || index > SelectionEnd)
                            {
                                SelectionStart = SelectionEnd = index;

                                _wordSelectionStart = -1;
                            }
                        }

                        break;
                    case 2:
                        if (!StringUtils.IsStartOfWord(text, index))
                        {
                            SelectionStart = StringUtils.PreviousWord(text, index);
                        }

                        _wordSelectionStart = SelectionStart;

                        if (!StringUtils.IsEndOfWord(text, index))
                        {
                            SelectionEnd = StringUtils.NextWord(text, index);
                        }
                        
                        break;
                    case 3:
                        _wordSelectionStart = -1;

                        SelectAll();
                        break;
                }
            }

            e.Pointer.Capture(this);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            // selection should not change during pointer move if the user right clicks
            if (e.Pointer.Captured == this && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var text = Text;
                var padding = Padding;

                var point = e.GetPosition(this) - new Point(padding.Left, padding.Top);

                point = new Point(
                    MathUtilities.Clamp(point.X, 0, Math.Max(TextLayout.Bounds.Width, 0)),
                    MathUtilities.Clamp(point.Y, 0, Math.Max(TextLayout.Bounds.Height, 0)));

                var hit = TextLayout.HitTestPoint(point);
                var textPosition = hit.TextPosition;

                if (text != null && _wordSelectionStart >= 0)
                {
                    var distance = textPosition - _wordSelectionStart;

                    if (distance <= 0)
                    {
                        SelectionStart = StringUtils.PreviousWord(text, textPosition);
                    }

                    if (distance >= 0)
                    {
                        if (SelectionStart != _wordSelectionStart)
                        {
                            SelectionStart = _wordSelectionStart;
                        }

                        SelectionEnd = StringUtils.NextWord(text, textPosition);
                    }
                }
                else
                {
                    SelectionEnd = textPosition;
                }

            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (e.Pointer.Captured != this)
            {
                return;
            }

            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                var padding = Padding;

                var point = e.GetPosition(this) - new Point(padding.Left, padding.Top);

                var hit = TextLayout.HitTestPoint(point);

                var caretIndex = hit.TextPosition;

                // see if mouse clicked inside current selection
                // if it did not, we change the selection to where the user clicked
                var firstSelection = Math.Min(SelectionStart, SelectionEnd);
                var lastSelection = Math.Max(SelectionStart, SelectionEnd);
                var didClickInSelection = SelectionStart != SelectionEnd &&
                                          caretIndex >= firstSelection && caretIndex <= lastSelection;
                if (!didClickInSelection)
                {
                    SelectionStart = SelectionEnd = caretIndex;
                }
            }

            e.Pointer.Capture(null);
        }

        private void UpdateCommandStates()
        {
            var text = GetSelection();

            CanCopy = !string.IsNullOrEmpty(text);
        }

        private string GetSelection()
        {
            var text = GetText();

            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var end = Math.Max(selectionStart, selectionEnd);

            if (start == end || text.Length < end)
            {
                return "";
            }

            var length = Math.Max(0, end - start);

            var selectedText = text.Substring(start, length);

            return selectedText;
        }
    }
}
