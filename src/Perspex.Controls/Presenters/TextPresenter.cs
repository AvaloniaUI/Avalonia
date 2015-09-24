// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using Perspex.Media;
using Perspex.Threading;
using Perspex.VisualTree;

namespace Perspex.Controls.Presenters
{
    public class TextPresenter : TextBlock
    {
        public static readonly PerspexProperty<int> CaretIndexProperty =
            TextBox.CaretIndexProperty.AddOwner<TextPresenter>();

        public static readonly PerspexProperty<int> SelectionStartProperty =
            TextBox.SelectionStartProperty.AddOwner<TextPresenter>();

        public static readonly PerspexProperty<int> SelectionEndProperty =
            TextBox.SelectionEndProperty.AddOwner<TextPresenter>();

        private readonly DispatcherTimer _caretTimer;

        private bool _caretBlink;

        private IObservable<bool> _canScrollHorizontally;

        public TextPresenter()
        {
            _caretTimer = new DispatcherTimer();
            _caretTimer.Interval = TimeSpan.FromMilliseconds(500);
            _caretTimer.Tick += CaretTimerTick;

            _canScrollHorizontally = GetObservable(TextWrappingProperty)
                .Select(x => x == TextWrapping.NoWrap);

            Observable.Merge(
                GetObservable(SelectionStartProperty),
                GetObservable(SelectionEndProperty))
                .Subscribe(_ => InvalidateFormattedText());

            GetObservable(CaretIndexProperty)
                .Subscribe(CaretIndexChanged);
        }

        public int CaretIndex
        {
            get { return GetValue(CaretIndexProperty); }
            set { SetValue(CaretIndexProperty, value); }
        }

        public int SelectionStart
        {
            get { return GetValue(SelectionStartProperty); }
            set { SetValue(SelectionStartProperty, value); }
        }

        public int SelectionEnd
        {
            get { return GetValue(SelectionEndProperty); }
            set { SetValue(SelectionEndProperty, value); }
        }

        public int GetCaretIndex(Point point)
        {
            var hit = FormattedText.HitTestPoint(point);
            return hit.TextPosition + (hit.IsTrailing ? 1 : 0);
        }

        public override void Render(IDrawingContext context)
        {
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;
                var rects = FormattedText.HitTestTextRange(start, length);

                var brush = new SolidColorBrush(0xff086f9e);

                foreach (var rect in rects)
                {
                    context.FillRectange(brush, rect);
                }
            }

            base.Render(context);

            if (selectionStart == selectionEnd)
            {
                var charPos = FormattedText.HitTestTextPosition(CaretIndex);

                var background = this.GetVisualAncestors()
                .OfType<Control>()
                .FirstOrDefault(x => x.IsSet(BackgroundProperty))
                ?.GetValue(BackgroundProperty) as SolidColorBrush;

                byte red = (byte)~(background.Color.R);
                byte green = (byte)~(background.Color.G);
                byte blue = (byte)~(background.Color.B);

                var caretBrush = new SolidColorBrush(Color.FromRgb(red, green, blue));

                if (_caretBlink)
                {
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
                _caretBlink = true;
                _caretTimer.Stop();
                _caretTimer.Start();
                InvalidateVisual();

                var rect = FormattedText.HitTestTextPosition(caretIndex);
                this.BringIntoView(rect);
            }
        }

        protected override FormattedText CreateFormattedText(Size constraint)
        {
            var result = base.CreateFormattedText(constraint);
            var selectionStart = SelectionStart;
            var selectionEnd = SelectionEnd;
            var start = Math.Min(selectionStart, selectionEnd);
            var length = Math.Max(selectionStart, selectionEnd) - start;

            if (length > 0)
            {
                result.SetForegroundBrush(Brushes.White, start, length);
            }

            return result;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var text = Text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                return base.MeasureOverride(availableSize);
            }
            else
            {
                // TODO: Pretty sure that measuring "X" isn't the right way to do this...
                using (var formattedText = new FormattedText(
                    "X",
                    FontFamily,
                    FontSize,
                    FontStyle,
                    TextAlignment.Left,
                    FontWeight))
                {
                    return formattedText.Measure();
                }
            }
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            _caretBlink = !_caretBlink;
            InvalidateVisual();
        }
    }
}
