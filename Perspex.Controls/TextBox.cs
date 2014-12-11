// -----------------------------------------------------------------------
// <copyright file="TextBox.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.Media;
    using Perspex.Styling;

    public class TextBox : TemplatedControl
    {
        public static readonly PerspexProperty<bool> AcceptsReturnProperty =
            PerspexProperty.Register<TextBox, bool>("AcceptsReturn");

        public static readonly PerspexProperty<bool> AcceptsTabProperty =
            PerspexProperty.Register<TextBox, bool>("AcceptsTab");

        public static readonly PerspexProperty<int> CaretIndexProperty =
            PerspexProperty.Register<TextBox, int>("CaretIndex", coerce: CoerceCaretIndex);

        public static readonly PerspexProperty<int> SelectionStartProperty =
            PerspexProperty.Register<TextBox, int>("SelectionStart", coerce: CoerceCaretIndex);

        public static readonly PerspexProperty<int> SelectionEndProperty =
            PerspexProperty.Register<TextBox, int>("SelectionEnd", coerce: CoerceCaretIndex);

        public static readonly PerspexProperty<string> TextProperty =
            TextBlock.TextProperty.AddOwner<TextBox>();

        public static readonly PerspexProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        private TextBoxView textBoxView;

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
        }

        public TextBox()
        {
            this.GotFocus += (s, e) => this.textBoxView.GotFocus();
            this.LostFocus += (s, e) => this.textBoxView.LostFocus();
            this.KeyDown += this.OnKeyDown;
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

        public string Text
        {
            get { return this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
        }

        public TextWrapping TextWrapping
        {
            get { return this.GetValue(TextWrappingProperty); }
            set { this.SetValue(TextWrappingProperty, value); }
        }

        protected override void OnPointerPressed(PointerEventArgs e)
        {
            var point = e.GetPosition(this.textBoxView);
            this.CaretIndex = this.SelectionStart = this.SelectionEnd =
                this.textBoxView.GetCaretIndex(point);
            e.Device.Capture(this);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (e.Device.Captured == this)
            {
                var point = e.GetPosition(this.textBoxView);
                this.CaretIndex = this.SelectionEnd = this.textBoxView.GetCaretIndex(point);
            }
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            if (e.Device.Captured == this)
            {
                e.Device.Capture(null);
            }
        }

        protected override void OnTemplateApplied()
        {
            Decorator textContainer = this.GetVisualDescendents()
                .OfType<Decorator>()
                .FirstOrDefault(x => x.Id == "textContainer");

            if (textContainer == null)
            {
                throw new Exception(
                    "TextBox template doesn't contain a textContainer " +
                    "or textContainer is not a Decorator.");
            }

            textContainer.Content = this.textBoxView = new TextBoxView(this);
        }

        private static int CoerceCaretIndex(PerspexObject o, int value)
        {
            var text = o.GetValue(TextProperty);
            var length = (text != null) ? text.Length : 0;
            return Math.Max(0, Math.Min(length, value));
        }

        private void MoveHorizontal(int count, ModifierKeys modifiers)
        {
            var text = this.Text ?? string.Empty;
            var caretIndex = this.CaretIndex;

            if ((modifiers & ModifierKeys.Control) != 0)
            {
                count = this.NextWord(text, count, caretIndex);
            }

            this.CaretIndex = caretIndex += count;
        }

        private void MoveVertical(int count, ModifierKeys modifiers)
        {
            var formattedText = this.textBoxView.FormattedText;
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
                var lines = this.textBoxView.FormattedText.GetLines();
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
                var lines = this.textBoxView.FormattedText.GetLines();
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

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            string text = this.Text ?? string.Empty;
            int caretIndex = this.CaretIndex;
            bool movement = false;
            bool textEntered = false;
            var modifiers = e.Device.Modifiers;

            switch (e.Key)
            {
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

        private int NextWord(string text, int direction, int caretIndex)
        {
            int pos = caretIndex;
            bool foundNonWhiteSpace = false;

            for (; ;)
            {
                pos += direction;

                if (direction < 0 && pos <= 0)
                {
                    pos = 0;
                    break;
                }
                else if (direction > 0 && pos >= text.Length)
                {
                    pos = text.Length;
                    break;
                }
                else if (char.IsWhiteSpace(text[pos]))
                {
                    if (foundNonWhiteSpace)
                    {
                        if (direction < 0)
                        {
                            ++pos;
                            break;
                        }
                        else
                        {
                            while (pos < text.Length && char.IsWhiteSpace(text[pos]))
                            {
                                ++pos;
                            }

                            break;
                        }
                    }
                }
                else
                {
                    foundNonWhiteSpace = true;
                }
            }

            return pos - caretIndex;
        }
    }
}
