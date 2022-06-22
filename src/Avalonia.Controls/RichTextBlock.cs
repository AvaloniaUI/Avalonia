using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control that displays a block of text.
    /// </summary>
    public class RichTextBlock : TextBlock, IInlineHost
    {
        public static readonly StyledProperty<bool> IsTextSelectionEnabledProperty =
            AvaloniaProperty.Register<RichTextBlock, bool>(nameof(IsTextSelectionEnabled), false);

        public static readonly DirectProperty<RichTextBlock, int> CaretIndexProperty =
            AvaloniaProperty.RegisterDirect<RichTextBlock, int>(
                nameof(CaretIndex),
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

        public static readonly DirectProperty<RichTextBlock, int> SelectionStartProperty =
            AvaloniaProperty.RegisterDirect<RichTextBlock, int>(
                nameof(SelectionStart),
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<RichTextBlock, int> SelectionEndProperty =
            AvaloniaProperty.RegisterDirect<RichTextBlock, int>(
                nameof(SelectionEnd),
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        public static readonly DirectProperty<RichTextBlock, string> SelectedTextProperty =
            AvaloniaProperty.RegisterDirect<RichTextBlock, string>(
                nameof(SelectedText),
                o => o.SelectedText);

        public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
            AvaloniaProperty.Register<RichTextBlock, IBrush?>(nameof(SelectionBrush), Brushes.Blue);

        public static readonly StyledProperty<IBrush?> SelectionForegroundBrushProperty =
            AvaloniaProperty.Register<RichTextBlock, IBrush?>(nameof(SelectionForegroundBrush));

        /// <summary>
        /// Defines the <see cref="Inlines"/> property.
        /// </summary>
        public static readonly StyledProperty<InlineCollection> InlinesProperty =
            AvaloniaProperty.Register<RichTextBlock, InlineCollection>(
                nameof(Inlines));

        private int _caretIndex;
        private int _selectionStart;
        private int _selectionEnd;

        static RichTextBlock()
        {
            FocusableProperty.OverrideDefaultValue(typeof(RichTextBlock), true);

            AffectsRender<RichTextBlock>(SelectionStartProperty, SelectionEndProperty, SelectionForegroundBrushProperty, SelectionBrushProperty);
        }

        public RichTextBlock()
        {
            Inlines = new InlineCollection
            {
                Parent = this,
                InlineHost = this
            };
        }

        public IBrush? SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public IBrush? SelectionForegroundBrush
        {
            get => GetValue(SelectionForegroundBrushProperty);
            set => SetValue(SelectionForegroundBrushProperty, value);
        }

        public int CaretIndex
        {
            get => _caretIndex;
            set
            {
                if(SetAndRaise(CaretIndexProperty, ref _caretIndex, value))
                {
                    SelectionStart = SelectionEnd = value;
                }
            }
        }

        public int SelectionStart
        {
            get => _selectionStart;
            set
            {
                if (SetAndRaise(SelectionStartProperty, ref _selectionStart, value))
                {
                    RaisePropertyChanged(SelectedTextProperty, "", "");

                    if (SelectionEnd == value && CaretIndex != value)
                    {
                        CaretIndex = value;
                    }
                }
            }
        }

        public int SelectionEnd
        {
            get => _selectionEnd;
            set
            {
                if(SetAndRaise(SelectionEndProperty, ref _selectionEnd, value))
                {
                    RaisePropertyChanged(SelectedTextProperty, "", "");

                    if (SelectionStart == value && CaretIndex != value)
                    {
                        CaretIndex = value;
                    }
                }
            }
        }

        public string SelectedText
        {
            get => GetSelection();
        }

        public bool IsTextSelectionEnabled
        {
            get => GetValue(IsTextSelectionEnabledProperty);
            set => SetValue(IsTextSelectionEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the inlines.
        /// </summary>
        [Content]
        public InlineCollection Inlines
        {
            get => GetValue(InlinesProperty);
            set => SetValue(InlinesProperty, value);
        }

        /// <summary>
        /// Creates the <see cref="TextLayout"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to format.</param>
        /// <returns>A <see cref="TextLayout"/> object.</returns>
        protected override TextLayout CreateTextLayout(Size constraint, string? text)
        {
            var defaultProperties = new GenericTextRunProperties(
                new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
                FontSize,
                TextDecorations,
                Foreground);

            var paragraphProperties = new GenericTextParagraphProperties(FlowDirection, TextAlignment, true, false,
                defaultProperties, TextWrapping, LineHeight, 0);

            ITextSource textSource;

            var inlines = Inlines;

            if (inlines is not null && inlines.HasComplexContent)
            {
                var textRuns = new List<TextRun>();

                foreach (var inline in inlines)
                {
                    inline.BuildTextRun(textRuns);
                }

                textSource = new InlinesTextSource(textRuns);
            }
            else
            {
                textSource = new SimpleTextSource((text ?? "").AsMemory(), defaultProperties);
            }

            return new TextLayout(
                textSource,
                paragraphProperties,
                TextTrimming,
                constraint.Width,
                constraint.Height,
                maxLines: MaxLines,
                lineHeight: LineHeight);
        }

        public override void Render(DrawingContext context)
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var selectionBrush = SelectionBrush;

            var selectionEnabled = IsTextSelectionEnabled;

            if (selectionEnabled && selectionStart != selectionEnd && selectionBrush != null)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                var rects = TextLayout.HitTestTextRange(start, length);

                foreach (var rect in rects)
                {
                    context.FillRectangle(selectionBrush, PixelRect.FromRect(rect, 1).ToRect(1));
                }
            }

            base.Render(context);
        }

        /// <summary>
        /// Select all text in the TextBox
        /// </summary>
        public void SelectAll()
        {
            if (!IsTextSelectionEnabled)
            {
                return;
            }

            var text = Inlines.Text ?? Text;

            SelectionStart = 0;
            SelectionEnd = text?.Length ?? 0;
        }

        /// <summary>
        /// Clears the current selection/>
        /// </summary>
        public void ClearSelection()
        {
            if (!IsTextSelectionEnabled)
            {
                return;
            }

            SelectionEnd = SelectionStart;
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            ClearSelection();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (!IsTextSelectionEnabled)
            {
                return;
            }

            var text = Inlines.Text;
            var clickInfo = e.GetCurrentPoint(this);

            if (text != null && clickInfo.Properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(this);

                var clickToSelect = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

                var hit = TextLayout.HitTestPoint(point);

                var oldIndex = CaretIndex;
                var index = hit.TextPosition;
                CaretIndex = index;

#pragma warning disable CS0618 // Type or member is obsolete
                switch (e.ClickCount)
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    case 1:
                        if (clickToSelect)
                        {
                            SelectionStart = Math.Min(oldIndex, index);
                            SelectionEnd = Math.Max(oldIndex, index);
                        }
                        else
                        {
                            SelectionStart = SelectionEnd = index;
                        }

                        break;
                    case 2:
                        if (!StringUtils.IsStartOfWord(text, index))
                        {
                            SelectionStart = StringUtils.PreviousWord(text, index);
                        }

                        SelectionEnd = StringUtils.NextWord(text, index);
                        break;
                    case 3:
                        SelectAll();
                        break;
                }
            }

            e.Pointer.Capture(this);
            e.Handled = true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (!IsTextSelectionEnabled)
            {
                return;
            }

            // selection should not change during pointer move if the user right clicks
            if (e.Pointer.Captured == this && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                var point = e.GetPosition(this);

                point = new Point(
                    MathUtilities.Clamp(point.X, 0, Math.Max(Bounds.Width - 1, 0)),
                    MathUtilities.Clamp(point.Y, 0, Math.Max(Bounds.Height - 1, 0)));

                var hit = TextLayout.HitTestPoint(point);

                SelectionEnd = hit.TextPosition;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (!IsTextSelectionEnabled)
            {
                return;
            }

            if (e.Pointer.Captured != this)
            {
                return;
            }

            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                var point = e.GetPosition(this);

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
                    _caretIndex = SelectionEnd = SelectionStart = caretIndex;
                }
            }

            e.Pointer.Capture(null);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(InlinesProperty):
                    {
                        OnInlinesChanged(change.OldValue as InlineCollection, change.NewValue as InlineCollection);
                        InvalidateTextLayout();
                        break;
                    }
                case nameof(TextProperty):
                    {
                        OnTextChanged(change.OldValue as string, change.NewValue as string);
                        break;
                    }
            }
        }

        private void OnTextChanged(string? oldValue, string? newValue)
        {
            if (oldValue == newValue)
            {
                return;
            }

            if (Inlines is null)
            {
                return;
            }

            Inlines.Text = newValue;
        }

        private string GetSelection()
        {
            var text = Inlines.Text ?? Text;

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

        private void OnInlinesChanged(InlineCollection? oldValue, InlineCollection? newValue)
        {
            if (oldValue is not null)
            {
                oldValue.Parent = null;
                oldValue.InlineHost = null;
                oldValue.Invalidated -= (s, e) => InvalidateTextLayout();
            }

            if (newValue is not null)
            {
                newValue.Parent = this;
                newValue.InlineHost = this;
                newValue.Invalidated += (s, e) => InvalidateTextLayout();
            }
        }

        void IInlineHost.AddVisualChild(IControl child)
        {
            if (child.VisualParent == null)
            {
                VisualChildren.Add(child);
            }
        }

        void IInlineHost.Invalidate()
        {
            InvalidateTextLayout();
        }

        private readonly struct InlinesTextSource : ITextSource
        {
            private readonly IReadOnlyList<TextRun> _textRuns;

            public InlinesTextSource(IReadOnlyList<TextRun> textRuns)
            {
                _textRuns = textRuns;
            }

            public TextRun? GetTextRun(int textSourceIndex)
            {
                var currentPosition = 0;

                foreach (var textRun in _textRuns)
                {
                    if (textRun.TextSourceLength == 0)
                    {
                        continue;
                    }

                    if (currentPosition >= textSourceIndex)
                    {
                        return textRun;
                    }

                    currentPosition += textRun.TextSourceLength;
                }

                return null;
            }
        }
    }
}
