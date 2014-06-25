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

    internal class TextBoxView : Control
    {
        private TextBox parent;

        private FormattedText formattedText;

        //private DispatcherTimer caretTimer;

        private bool caretBlink;

        public TextBoxView(TextBox parent)
        {
            //this.caretTimer = new DispatcherTimer();
            //this.caretTimer.Interval = PlatformInterface.Instance.CaretBlinkTime;
            //this.caretTimer.Tick += this.CaretTimerTick;
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

        public void GotFocus()
        {
            this.caretBlink = true;
            //this.caretTimer.Start();
        }

        public void LostFocus()
        {
            //this.caretTimer.Stop();
            this.InvalidateVisual();
        }

        public void InvalidateText()
        {
            this.formattedText = null;
            this.InvalidateMeasure();
        }

        internal void CaretMoved()
        {
            //this.caretBlink = true;
            //this.caretTimer.Stop();
            //this.caretTimer.Start();
            this.InvalidateVisual();
        }

        public override void Render(IDrawingContext context)
        {
            Rect rect = new Rect(this.ActualSize);

            context.DrawText(Brushes.Black, rect, this.FormattedText);

            //if (this.parent.IsKeyboardFocused)
            //{
            //    Point caretPos = this.FormattedText.GetCaretPosition(this.parent.CaretIndex);
            //    Brush caretBrush = this.parent.CaretBrush;

            //    if (caretBrush == null)
            //    {
            //        Color color = Colors.Black;
            //        SolidColorBrush background = this.parent.Background as SolidColorBrush;

            //        if (background != null)
            //        {
            //            color = Color.FromUInt32(0x00ffffffu ^ background.Color.ToUint32());
            //        }

            //        caretBrush = new SolidColorBrush(color);
            //    }

            //    if (this.caretBlink)
            //    {
            //        drawingContext.DrawLine(
            //            new Pen(caretBrush, 1),
            //            caretPos,
            //            caretPos + new Vector(0, this.FormattedText.Height));
            //    }
            //}
        }

        protected override Size MeasureOverride(Size constraint)
        {
            return new Size(this.FormattedText.Size.Width, this.FormattedText.Size.Height);
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
