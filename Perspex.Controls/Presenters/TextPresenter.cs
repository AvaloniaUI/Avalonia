// -----------------------------------------------------------------------
// <copyright file="TextPresenter.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Utils;
    using Perspex.Input;
    using Perspex.Media;
    using Perspex.Threading;

    public class TextPresenter : TextBlock
    {
        public static readonly PerspexProperty<bool> AcceptsReturnProperty =
            TextBox.AcceptsReturnProperty.AddOwner<TextPresenter>();

        public static readonly PerspexProperty<bool> AcceptsTabProperty =
            TextBox.AcceptsTabProperty.AddOwner<TextPresenter>();

        public static readonly PerspexProperty<int> CaretIndexProperty =
            TextBox.CaretIndexProperty.AddOwner<TextPresenter>();

        public static readonly PerspexProperty<int> SelectionStartProperty =
            TextBox.SelectionStartProperty.AddOwner<TextPresenter>();

        public static readonly PerspexProperty<int> SelectionEndProperty =
            TextBox.SelectionEndProperty.AddOwner<TextPresenter>();

        private DispatcherTimer caretTimer;

        private bool caretBlink;

        private IObservable<bool> canScrollHorizontally;

        static TextPresenter()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextPresenter), true);
        }

        public TextPresenter()
        {
            this.caretTimer = new DispatcherTimer();
            this.caretTimer.Interval = TimeSpan.FromMilliseconds(500);
            this.caretTimer.Tick += this.CaretTimerTick;

            this.canScrollHorizontally = this.GetObservable(TextWrappingProperty)
                .Select(x => x == TextWrapping.NoWrap);

            Observable.Merge(
                this.GetObservable(SelectionStartProperty),
                this.GetObservable(SelectionEndProperty))
                .Subscribe(_ => this.InvalidateFormattedText());

            this.GetObservable(TextPresenter.CaretIndexProperty)
                .Subscribe(this.CaretIndexChanged);
        }

        public bool AcceptsReturn
        {
            get { return this.GetValue(AcceptsReturnProperty); }
            set { this.SetValue(AcceptsReturnProperty, value); }
        }

        public bool AcceptsTab
        {
            get { return this.GetValue(AcceptsTabProperty); }
            set { this.SetValue(AcceptsTabProperty, value); }
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

        public new FormattedText FormattedText
        {
            get { return base.FormattedText; }
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
            this.SelectionStart = 0;
            this.SelectionEnd = 0;

            this.caretTimer.Stop();
            this.InvalidateVisual();
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

            if (this.IsFocused && selectionStart == selectionEnd)
            {
                var charPos = this.FormattedText.HitTestTextPosition(this.CaretIndex);
                Brush caretBrush = Brushes.Black;

                if (this.caretBlink)
                {
                    context.DrawLine(new Pen(caretBrush, 1), charPos.TopLeft, charPos.BottomLeft);
                }
            }
        }

        protected override FormattedText CreateFormattedText()
        {
            var result = base.CreateFormattedText();
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            string text = this.Text ?? string.Empty;
            int caretIndex = this.CaretIndex;
            bool movement = false;
            bool textEntered = false;
            var modifiers = e.Device.Modifiers;

            switch (e.Key)
            {
                case Key.A:
                    if (modifiers == ModifierKeys.Control)
                    {
                        SelectAll();
                        
                    }
                    else
                    {
                        textEntered = true;
                        goto default;
                    }

                    break;
                case Key.Left:
                    this.MoveHorizontal(-1, modifiers);
                    movement = true;
                    break;

                case Key.Right:
                    this.MoveHorizontal(1, modifiers);
                    movement = true;
                    break;

                case Key.Up:
                    this.MoveVertical(-1, modifiers);
                    movement = true;
                    break;

                case Key.Down:
                    this.MoveVertical(1, modifiers);
                    movement = true;
                    break;

                case Key.Home:
                    this.MoveHome(modifiers);
                    movement = true;
                    break;

                case Key.End:
                    this.MoveEnd(modifiers);
                    movement = true;
                    break;

                case Key.Back:
                    if (!this.DeleteSelection() && this.CaretIndex > 0)
                    {
                        this.Text = text.Substring(0, caretIndex - 1) + text.Substring(caretIndex);
                        --this.CaretIndex;
                    }

                    break;

                case Key.Delete:
                    if (!this.DeleteSelection() && caretIndex < text.Length)
                    {
                        this.Text = text.Substring(0, caretIndex) + text.Substring(caretIndex + 1);
                    }

                    break;

                case Key.Enter:
                    if (this.AcceptsReturn)
                    {
                        goto default;
                    }

                    break;

                case Key.Tab:
                    if (this.AcceptsTab)
                    {
                        goto default;
                    }

                    break;

                default:
                    if (!string.IsNullOrEmpty(e.Text))
                    {
                        this.DeleteSelection();
                        caretIndex = this.CaretIndex;
                        text = this.Text;
                        this.Text = text.Substring(0, caretIndex) + e.Text + text.Substring(caretIndex);
                        ++this.CaretIndex;
                        textEntered = true;
                    }

                    break;
            }

            if (movement && ((modifiers & ModifierKeys.Shift) != 0))
            {
                this.SelectionEnd = this.CaretIndex;
            }
            else if (movement || textEntered)
            {
                this.SelectionStart = this.SelectionEnd = this.CaretIndex;
            }

            e.Handled = true;
        }

        private void SelectAll()
        {
            this.SelectionStart = 0;
            this.SelectionEnd = this.Text.Length;
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            var point = e.GetPosition(this);
            var index = this.CaretIndex = this.GetCaretIndex(point);
            var text = this.Text;

            switch (e.ClickCount)
            {
                case 1:
                    this.SelectionStart = this.SelectionEnd = index;
                    break;
                case 2:
                    if (!StringUtils.IsStartOfWord(text, index))
                    {
                        this.SelectionStart = StringUtils.PreviousWord(text, index, false);
                    }

                    this.SelectionEnd = StringUtils.NextWord(text, index, false);
                    break;
                case 3:
                    this.SelectionStart = 0;
                    this.SelectionEnd = text.Length;
                    break;
            }

            e.Device.Capture(this);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (e.Device.Captured == this)
            {
                var point = e.GetPosition(this);
                this.CaretIndex = this.SelectionEnd = this.GetCaretIndex(point);
            }
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            if (e.Device.Captured == this)
            {
                e.Device.Capture(null);
            }
        }

        internal void CaretIndexChanged(int caretIndex)
        {
            this.caretBlink = true;
            this.caretTimer.Stop();
            this.caretTimer.Start();
            this.InvalidateVisual();

            var rect = this.FormattedText.HitTestTextPosition(caretIndex);
            this.BringIntoView(rect);
        }

        private void MoveHorizontal(int count, ModifierKeys modifiers)
        {
            var text = this.Text ?? string.Empty;
            var caretIndex = this.CaretIndex;

            if ((modifiers & ModifierKeys.Control) != 0)
            {
                if (count > 0)
                {
                    count = StringUtils.NextWord(text, caretIndex, false) - caretIndex;
                }
                else
                {
                    count = StringUtils.PreviousWord(text, caretIndex, false) - caretIndex;
                }
            }

            this.CaretIndex = caretIndex += count;
        }

        private void MoveVertical(int count, ModifierKeys modifiers)
        {
            var formattedText = this.FormattedText;
            var lines = formattedText.GetLines().ToList();
            var caretIndex = this.CaretIndex;
            var lineIndex = this.GetLine(caretIndex, lines) + count;

            if (lineIndex >= 0 && lineIndex < lines.Count)
            {
                var line = lines[lineIndex];
                var rect = formattedText.HitTestTextPosition(caretIndex);
                var y = count < 0 ? rect.Y : rect.Bottom;
                var point = new Point(rect.X, y + (count * (line.Height / 2)));
                var hit = formattedText.HitTestPoint(point);
                this.CaretIndex = caretIndex = hit.TextPosition + (hit.IsTrailing ? 1 : 0);
            }
        }

        private void MoveHome(ModifierKeys modifiers)
        {
            var text = this.Text ?? string.Empty;
            var caretIndex = this.CaretIndex;

            if ((modifiers & ModifierKeys.Control) != 0)
            {
                caretIndex = 0;
            }
            else
            {
                var lines = this.FormattedText.GetLines();
                var pos = 0;

                foreach (var line in lines)
                {
                    if (pos + line.Length > caretIndex || pos + line.Length == text.Length)
                    {
                        break;
                    }

                    pos += line.Length;
                }

                caretIndex = pos;
            }

            this.CaretIndex = caretIndex;
        }

        private void MoveEnd(ModifierKeys modifiers)
        {
            var text = this.Text ?? string.Empty;
            var caretIndex = this.CaretIndex;

            if ((modifiers & ModifierKeys.Control) != 0)
            {
                caretIndex = text.Length;
            }
            else
            {
                var lines = this.FormattedText.GetLines();
                var pos = 0;

                foreach (var line in lines)
                {
                    pos += line.Length;

                    if (pos > caretIndex)
                    {
                        if (pos < text.Length)
                        {
                            --pos;
                        }

                        break;
                    }
                }

                caretIndex = pos;
            }

            this.CaretIndex = caretIndex;
        }

        private bool DeleteSelection()
        {
            var selectionStart = this.SelectionStart;
            var selectionEnd = this.SelectionEnd;

            if (selectionStart != selectionEnd)
            {
                var start = Math.Min(selectionStart, selectionEnd);
                var end = Math.Max(selectionStart, selectionEnd);
                var text = this.Text;
                this.Text = text.Substring(0, start) + text.Substring(end);
                this.SelectionStart = this.SelectionEnd = this.CaretIndex = start;
                return true;
            }
            else
            {
                return false;
            }
        }

        private int GetLine(int caretIndex, IList<FormattedTextLine> lines)
        {
            int pos = 0;
            int i;

            for (i = 0; i < lines.Count; ++i)
            {
                var line = lines[i];
                pos += line.Length;

                if (pos > caretIndex)
                {
                    break;
                }
            }

            return i;
        }

        private void CaretTimerTick(object sender, EventArgs e)
        {
            this.caretBlink = !this.caretBlink;
            this.InvalidateVisual();
        }
    }
}
