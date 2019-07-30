// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls.Presenters
{
    public class TextPresenter : TextBlock
    {
        public static readonly DirectProperty<TextPresenter, int> CaretIndexProperty =
            TextBox.CaretIndexProperty.AddOwner<TextPresenter>(
                o => o.CaretIndex,
                (o, v) => o.CaretIndex = v);

        public static readonly StyledProperty<char> PasswordCharProperty =
            AvaloniaProperty.Register<TextPresenter, char>(nameof(PasswordChar));

        public static readonly StyledProperty<IBrush> SelectionBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush>(nameof(SelectionBrushProperty));

        public static readonly StyledProperty<IBrush> SelectionForegroundBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush>(nameof(SelectionForegroundBrushProperty));

        public static readonly StyledProperty<IBrush> CaretBrushProperty =
            AvaloniaProperty.Register<TextPresenter, IBrush>(nameof(CaretBrushProperty));

        public static readonly DirectProperty<TextPresenter, int> SelectionStartProperty =
            TextBox.SelectionStartProperty.AddOwner<TextPresenter>(
                o => o.SelectionStart,
                (o, v) => o.SelectionStart = v);

        public static readonly DirectProperty<TextPresenter, int> SelectionEndProperty =
            TextBox.SelectionEndProperty.AddOwner<TextPresenter>(
                o => o.SelectionEnd,
                (o, v) => o.SelectionEnd = v);

        private readonly DispatcherTimer _caretTimer;
        private int _caretIndex;
        private int _selectionStart;
        private int _selectionEnd;
        private bool _caretBlink;

        static TextPresenter()
        {
            AffectsRender<TextPresenter>(PasswordCharProperty,
                SelectionBrushProperty, SelectionForegroundBrushProperty,
                SelectionStartProperty, SelectionEndProperty);

            Observable.Merge(
                SelectionStartProperty.Changed,
                SelectionEndProperty.Changed,
                PasswordCharProperty.Changed
            ).AddClassHandler<TextPresenter>((x,_) => x.InvalidateFormattedText());

            CaretIndexProperty.Changed.AddClassHandler<TextPresenter>((x, e) => x.CaretIndexChanged((int)e.NewValue));
        }

        public TextPresenter()
        {
            _caretTimer = new DispatcherTimer();
            _caretTimer.Interval = TimeSpan.FromMilliseconds(500);
            _caretTimer.Tick += CaretTimerTick;
        }

        public int CaretIndex
        {
            get
            {
                return _caretIndex;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(CaretIndexProperty, ref _caretIndex, value);
            }
        }

        public char PasswordChar
        {
            get => GetValue(PasswordCharProperty);
            set => SetValue(PasswordCharProperty, value);
        }

        public IBrush SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public IBrush SelectionForegroundBrush
        {
            get => GetValue(SelectionForegroundBrushProperty);
            set => SetValue(SelectionForegroundBrushProperty, value);
        }
        
        public IBrush CaretBrush
        {
            get => GetValue(CaretBrushProperty);
            set => SetValue(CaretBrushProperty, value);
        }

        public int SelectionStart
        {
            get
            {
                return _selectionStart;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(SelectionStartProperty, ref _selectionStart, value);
            }
        }

        public int SelectionEnd
        {
            get
            {
                return _selectionEnd;
            }

            set
            {
                value = CoerceCaretIndex(value);
                SetAndRaise(SelectionEndProperty, ref _selectionEnd, value);
            }
        }

        public int GetCaretIndex(Point point)
        {
            var hit = FormattedText.HitTestPoint(point);
            return hit.TextPosition + (hit.IsTrailing ? 1 : 0);
        }

        public override void Render(DrawingContext context)
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;

                // issue #600: set constraint before any FormattedText manipulation
                //             see base.Render(...) implementation
                FormattedText.Constraint = Bounds.Size;

                var rects = FormattedText.HitTestTextRange(start, length);

                foreach (var rect in rects)
                {
                    context.FillRectangle(SelectionBrush, rect);
                }
            }

            base.Render(context);

            if (selectionStart == selectionEnd)
            {
                var caretBrush = CaretBrush;

                if (caretBrush is null)
                {
                    var backgroundColor = (((Control)TemplatedParent).GetValue(BackgroundProperty) as SolidColorBrush)?.Color;
                    if (backgroundColor.HasValue)
                    {
                        byte red = (byte)~(backgroundColor.Value.R);
                        byte green = (byte)~(backgroundColor.Value.G);
                        byte blue = (byte)~(backgroundColor.Value.B);

                        caretBrush = new SolidColorBrush(Color.FromRgb(red, green, blue));
                    }
                    else
                        caretBrush = Brushes.Black;
                }

                if (_caretBlink)
                {
                    var charPos = FormattedText.HitTestTextPosition(CaretIndex);
                    var x = Math.Floor(charPos.X) + 0.5;
                    var y = Math.Floor(charPos.Y) + 0.5;
                    var b = Math.Ceiling(charPos.Bottom) - 0.5;

                    context.DrawLine(
                        new Pen(caretBrush, 1),
                        new Point(x, y),
                        new Point(x, b));
                }
            }
        }

        public void ShowCaret()
        {
            _caretBlink = true;
            _caretTimer.Start();
            InvalidateVisual();
        }

        public void HideCaret()
        {
            _caretBlink = false;
            _caretTimer.Stop();
            InvalidateVisual();
        }

        internal void CaretIndexChanged(int caretIndex)
        {
            if (this.GetVisualParent() != null)
            {
                if (_caretTimer.IsEnabled)
                {
                    _caretBlink = true;
                    _caretTimer.Stop();
                    _caretTimer.Start();
                    InvalidateVisual();
                }
                else
                {
                    _caretTimer.Start();
                    InvalidateVisual();
                    _caretTimer.Stop();
                }

                if (IsMeasureValid)
                {
                    var rect = FormattedText.HitTestTextPosition(caretIndex);
                    this.BringIntoView(rect);
                }
                else
                {
                    // The measure is currently invalid so there's no point trying to bring the 
                    // current char into view until a measure has been carried out as the scroll
                    // viewer extents may not be up-to-date.
                    Dispatcher.UIThread.Post(
                        () =>
                        {
                            var rect = FormattedText.HitTestTextPosition(caretIndex);
                            this.BringIntoView(rect);
                        },
                        DispatcherPriority.Normal);
                }
            }
        }

        /// <summary>
        /// Creates the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <param name="text">The text to generated the <see cref="FormattedText"/> for.</param>
        /// <returns>A <see cref="FormattedText"/> object.</returns>
        protected override FormattedText CreateFormattedText(Size constraint, string text)
        {
            FormattedText result = null;

            if (PasswordChar != default(char))
            {
                result = base.CreateFormattedText(constraint, new string(PasswordChar, text?.Length ?? 0));
            }
            else
            {
                result = base.CreateFormattedText(constraint, text);
            }

            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var length = Math.Max(selectionStart, selectionEnd) - start;

            if (length > 0)
            {
                result.Spans = new[]
                {
                    new FormattedTextStyleSpan(start, length, SelectionForegroundBrush),
                };
            }

            return result;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var text = Text;

            if (!string.IsNullOrEmpty(text))
            {
                return base.MeasureOverride(availableSize);
            }
            else
            {
                return new FormattedText
                {
                    Text = "X",
                    Typeface = new Typeface(FontFamily, FontSize, FontStyle, FontWeight),
                    TextAlignment = TextAlignment,
                    Constraint = availableSize,
                }.Bounds.Size;
            }
        }

        private int CoerceCaretIndex(int value)
        {
            var text = Text;
            var length = text?.Length ?? 0;
            return Math.Max(0, Math.Min(length, value));
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            _caretBlink = !_caretBlink;
            InvalidateVisual();
        }
    }
}
