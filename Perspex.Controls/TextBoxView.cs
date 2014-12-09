// -----------------------------------------------------------------------
// <copyright file="TextBoxView.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Reactive.Linq;
    using Perspex.Media;
    using Perspex.Threading;

    internal class TextBoxView : TextBlock
    {
        private TextBox parent;

        private DispatcherTimer caretTimer;

        private bool caretBlink;

        public TextBoxView(TextBox parent)
        {
            this.caretTimer = new DispatcherTimer();
            this.caretTimer.Interval = TimeSpan.FromMilliseconds(500);
            this.caretTimer.Tick += this.CaretTimerTick;
            this.parent = parent;
            this[!TextProperty] = parent[!TextProperty];

            Observable.Merge(
                this.parent.GetObservable(TextBox.SelectionStartProperty),
                this.parent.GetObservable(TextBox.SelectionEndProperty))
                .Subscribe(_ => this.InvalidateVisual());

            parent.GetObservable(TextBox.CaretIndexProperty).Subscribe(_ => this.CaretMoved());
        }

        public int GetCaretIndex(Point point)
        {
            var hit = this.FormattedText.HitTestPoint(point);
            return hit.TextPosition + (hit.IsTrailing ? 1 : 0);
        }

        public new void GotFocus()
        {
            this.caretBlink = true;
            this.caretTimer.Start();
        }

        public new void LostFocus()
        {
            this.caretTimer.Stop();
            this.InvalidateVisual();
        }

        public override void Render(IDrawingContext context)
        {
            var selectionStart = this.parent.SelectionStart;
            var selectionEnd = this.parent.SelectionEnd;

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

            if (this.parent.IsFocused)
            {
                var charPos = this.FormattedText.HitTestTextPosition(this.parent.CaretIndex);
                Brush caretBrush = Brushes.Black;

                if (this.caretBlink)
                {
                    context.DrawLine(new Pen(caretBrush, 1), charPos.TopLeft, charPos.BottomLeft);
                }
            }
        }

        internal void CaretMoved()
        {
            this.caretBlink = true;
            this.caretTimer.Stop();
            this.caretTimer.Start();
            this.InvalidateVisual();
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            this.caretBlink = !this.caretBlink;
            this.InvalidateVisual();
        }
    }
}
