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

        private DispatcherTimer _caretTimer;

        private bool _caretBlink;

        private IObservable<bool> _canScrollHorizontally;

        public TextPresenter()
        {
            _caretTimer = new DispatcherTimer();
            _caretTimer.Interval = TimeSpan.FromMilliseconds(500);
            _caretTimer.Tick += this.CaretTimerTick;

            _canScrollHorizontally = this.GetObservable(TextWrappingProperty)
                .Select(x => x == TextWrapping.NoWrap);

            Observable.Merge(
                this.GetObservable(SelectionStartProperty),
                this.GetObservable(SelectionEndProperty))
                .Subscribe(_ => this.InvalidateFormattedText());

            this.GetObservable(TextPresenter.CaretIndexProperty)
                .Subscribe(this.CaretIndexChanged);
        }

        public int CaretIndex
        {
            get { return this.GetValue(CaretIndexProperty); }
            set { this.SetValue(CaretIndexProperty, value); }
        }

        public int SelectionStart
        {
            get { return this.GetValue(SelectionStartProperty); }
            set { this.SetValue(SelectionStartProperty, value); }
        }

        public int SelectionEnd
        {
            get { return this.GetValue(SelectionEndProperty); }
            set { this.SetValue(SelectionEndProperty, value); }
        }

        public int GetCaretIndex(Point point)
        {
            var hit = this.FormattedText.HitTestPoint(point);
            return hit.TextPosition + (hit.IsTrailing ? 1 : 0);
        }

        public override void Render(IDrawingContext context)
        {
            var selectionStart = this.SelectionStart;
            var selectionEnd = this.SelectionEnd;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var length = Math.Max(selectionStart, selectionEnd) - start;
                var rects = this.FormattedText.HitTestTextRange(start, length);

                var brush = new SolidColorBrush(0xff086f9e);

                foreach (var rect in rects)
                {
                    context.FillRectange(brush, rect);
                }
            }

            base.Render(context);

            if (selectionStart == selectionEnd)
            {
                var charPos = this.FormattedText.HitTestTextPosition(this.CaretIndex);
                Brush caretBrush = Brushes.Black;

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
            this.InvalidateVisual();
        }

        public void HideCaret()
        {
            _caretBlink = false;
            _caretTimer.Stop();
            this.InvalidateVisual();
        }

        internal void CaretIndexChanged(int caretIndex)
        {
            if (this.GetVisualParent() != null)
            {
                _caretBlink = true;
                _caretTimer.Stop();
                _caretTimer.Start();
                this.InvalidateVisual();

                var rect = this.FormattedText.HitTestTextPosition(caretIndex);
                this.BringIntoView(rect);
            }
        }

        protected override FormattedText CreateFormattedText(Size constraint)
        {
            var result = base.CreateFormattedText(constraint);
            var selectionStart = this.SelectionStart;
            var selectionEnd = this.SelectionEnd;
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
            var text = this.Text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                return base.MeasureOverride(availableSize);
            }
            else
            {
                // TODO: Pretty sure that measuring "X" isn't the right way to do this...
                using (var formattedText = new FormattedText(
                    "X",
                    this.FontFamily,
                    this.FontSize,
                    this.FontStyle,
                    TextAlignment.Left,
                    this.FontWeight))
                {
                    return formattedText.Measure();
                }
            }
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            _caretBlink = !_caretBlink;
            this.InvalidateVisual();
        }
    }
}
