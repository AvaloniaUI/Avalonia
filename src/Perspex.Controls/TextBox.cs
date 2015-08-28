﻿// -----------------------------------------------------------------------
// <copyright file="TextBox.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Controls.Utils;
    using Perspex.Input;
    using Perspex.Interactivity;
    using Perspex.Media;

    public class TextBox : TemplatedControl
    {
        public static readonly PerspexProperty<bool> AcceptsReturnProperty =
            PerspexProperty.Register<TextBox, bool>("AcceptsReturn");

        public static readonly PerspexProperty<bool> AcceptsTabProperty =
            PerspexProperty.Register<TextBox, bool>("AcceptsTab");

        public static readonly PerspexProperty<int> CaretIndexProperty =
            PerspexProperty.Register<TextBox, int>("CaretIndex", validate: ValidateCaretIndex);

        public static readonly PerspexProperty<int> SelectionStartProperty =
            PerspexProperty.Register<TextBox, int>("SelectionStart", validate: ValidateCaretIndex);

        public static readonly PerspexProperty<int> SelectionEndProperty =
            PerspexProperty.Register<TextBox, int>("SelectionEnd", validate: ValidateCaretIndex);

        public static readonly PerspexProperty<string> TextProperty =
            TextBlock.TextProperty.AddOwner<TextBox>();

        public static readonly PerspexProperty<TextWrapping> TextWrappingProperty =
            TextBlock.TextWrappingProperty.AddOwner<TextBox>();

        private TextPresenter presenter;

        static TextBox()
        {
            FocusableProperty.OverrideDefaultValue(typeof(TextBox), true);
        }

        public TextBox()
        {
            var canScrollHorizontally = this.GetObservable(AcceptsReturnProperty)
                .Select(x => !x);

            this.Bind(
                ScrollViewer.CanScrollHorizontallyProperty,
                canScrollHorizontally,
                BindingPriority.Style);

            var horizontalScrollBarVisibility = this.GetObservable(AcceptsReturnProperty)
                .Select(x => x ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden);

            this.Bind(
                ScrollViewer.HorizontalScrollBarVisibilityProperty,
                horizontalScrollBarVisibility,
                BindingPriority.Style);
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

        protected override void OnTemplateApplied()
        {
            this.presenter = this.GetTemplateChild<TextPresenter>("textPresenter");
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            this.presenter.ShowCaret();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            this.SelectionStart = 0;
            this.SelectionEnd = 0;
            this.presenter.HideCaret();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            string text = this.Text ?? string.Empty;
            int caretIndex = this.CaretIndex;
            bool movement = false;
            bool textEntered = false;
            bool handled = true;
            var modifiers = e.Device.Modifiers;

            switch (e.Key)
            {
                case Key.A:
                    if (modifiers == ModifierKeys.Control)
                    {
                        this.SelectAll();
                    }
                    else
                    {
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
                    else
                    {
                        base.OnKeyDown(e);
                        handled = false;
                    }

                    break;

                default:
                    if (!string.IsNullOrEmpty(e.Text))
                    {
                        this.DeleteSelection();
                        caretIndex = this.CaretIndex;
                        text = this.Text ?? string.Empty;
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

            if (handled)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            if (e.Source == this.presenter)
            {
                var point = e.GetPosition(this.presenter);
                var index = this.CaretIndex = this.presenter.GetCaretIndex(point);
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

                e.Device.Capture(this.presenter);
                e.Handled = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (e.Device.Captured == this.presenter)
            {
                var point = e.GetPosition(this.presenter);
                this.CaretIndex = this.SelectionEnd = this.presenter.GetCaretIndex(point);
            }
        }

        protected override void OnPointerReleased(PointerEventArgs e)
        {
            if (e.Device.Captured == this.presenter)
            {
                e.Device.Capture(null);
            }
        }

        private static int ValidateCaretIndex(PerspexObject o, int value)
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
            var formattedText = this.presenter.FormattedText;
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
                this.CaretIndex = hit.TextPosition + (hit.IsTrailing ? 1 : 0);
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
                var lines = this.presenter.FormattedText.GetLines();
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
                var lines = this.presenter.FormattedText.GetLines();
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

        private void SelectAll()
        {
            this.SelectionStart = 0;
            this.SelectionEnd = this.Text.Length;
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
    }
}
