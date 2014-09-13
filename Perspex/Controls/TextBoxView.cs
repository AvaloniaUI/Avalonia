// -----------------------------------------------------------------------
// <copyright file="TextBoxView.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Globalization;
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
            this.caretTimer = new DispatcherTimer();
            this.caretTimer.Interval = TimeSpan.FromMilliseconds(500);
            this.caretTimer.Tick += this.CaretTimerTick;
            this.parent = parent;
        }

        public FormattedText FormattedText
        {
            get
            {
                if (this.formattedText == null)
                {
                    this.formattedText = this.CreateFormattedText();
                }

                return this.formattedText;
            }
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

        public void InvalidateText()
        {
            this.formattedText = null;
            this.InvalidateMeasure();
        }

        internal void CaretMoved()
        {
            this.caretBlink = true;
            this.caretTimer.Stop();
            this.caretTimer.Start();
            this.InvalidateVisual();
        }

        public override void Render(IDrawingContext context)
        {
            Rect rect = new Rect(this.ActualSize);

            context.DrawText(Brushes.Black, rect, this.FormattedText);

            if (this.parent.IsFocused)
            {
                IPlatformRenderInterface platform = Locator.Current.GetService<IPlatformRenderInterface>();
                Point caretPos = platform.TextService.GetCaretPosition(
                    this.formattedText, 
                    this.parent.CaretIndex,
                    this.ActualSize);
                double[] lineHeights = platform.TextService.GetLineHeights(this.formattedText, this.ActualSize);
                Brush caretBrush = Brushes.Black;

                if (this.caretBlink)
                {
                    context.DrawLine(
                        new Pen(caretBrush, 1),
                        caretPos,
                        new Point(caretPos.X, caretPos.Y + lineHeights[0]));
                }
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return this.FormattedText.Measure(constraint);
        }

        private FormattedText CreateFormattedText()
        {
            return new FormattedText
            {
                FontFamilyName = "Segoe UI",
                FontSize = this.FontSize,
                Text = this.parent.Text,
            };
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            this.caretBlink = !this.caretBlink;
            this.InvalidateVisual();
        }
    }
}
