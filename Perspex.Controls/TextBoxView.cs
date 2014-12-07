// -----------------------------------------------------------------------
// <copyright file="TextBoxView.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using Perspex.Media;
    using Perspex.Platform;
    using Perspex.Threading;
    using Splat;

    internal class TextBoxView : Control
    {
        private TextBox parent;

        private FormattedText formattedText;

        private DispatcherTimer caretTimer;

        private bool caretBlink;

        public TextBoxView(TextBox parent)
        {
            this.FormattedText = new FormattedText();

            // TODO: Implement TextBlock.FontFamilyName.
            this.FormattedText.FontFamilyName = "Segoe UI";

            parent.GetObservable(TextBox.TextProperty).Subscribe(x =>
            {
                this.FormattedText.Text = x;
                this.InvalidateMeasure();
            });

            this.GetObservable(TextBlock.FontSizeProperty).Subscribe(x =>
            {
                this.FormattedText.FontSize = x;
                this.InvalidateMeasure();
            });

            this.GetObservable(TextBlock.FontStyleProperty).Subscribe(x =>
            {
                this.FormattedText.FontStyle = x;
                this.InvalidateMeasure();
            });

            this.caretTimer = new DispatcherTimer();
            this.caretTimer.Interval = TimeSpan.FromMilliseconds(500);
            this.caretTimer.Tick += this.CaretTimerTick;
            this.parent = parent;
        }

        public FormattedText FormattedText
        {
            get;
            private set;
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
            Rect rect = new Rect(this.ActualSize);

            context.DrawText(Brushes.Black, rect, this.FormattedText);

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

        protected override Size MeasureOverride(Size constraint)
        {
            if (!string.IsNullOrEmpty(this.parent.Text))
            {
                this.FormattedText.Constraint = constraint;
                return this.FormattedText.Measure();
            }

            return new Size();
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            this.caretBlink = !this.caretBlink;
            this.InvalidateVisual();
        }
    }
}
