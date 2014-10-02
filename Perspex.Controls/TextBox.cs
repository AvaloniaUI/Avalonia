// -----------------------------------------------------------------------
// <copyright file="TextBox.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.Platform;
    using Perspex.Styling;
    using Splat;

    public class TextBox : TemplatedControl
    {
        public static readonly PerspexProperty<string> TextProperty =
            TextBlock.TextProperty.AddOwner<TextBox>();

        private int caretIndex;

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
            this.PointerPressed += this.OnPointerPressed;
        }

        public int CaretIndex
        {
            get
            {
                return this.caretIndex;
            }

            set
            {
                value = Math.Min(Math.Max(value, 0), this.Text.Length);

                if (this.caretIndex != value)
                {
                    this.caretIndex = value;
                    this.textBoxView.CaretMoved();
                }
            }
        }

        public string Text
        {
            get { return this.GetValue(TextProperty); }
            set { this.SetValue(TextProperty, value); }
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
            this.GetObservable(TextProperty).Subscribe(_ => this.textBoxView.InvalidateText());
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            string text = this.Text;

            switch (e.Key)
            {
                case Key.Left:
                    --this.CaretIndex;
                    break;

                case Key.Right:
                    ++this.CaretIndex;
                    break;

                case Key.Back:
                    if (this.caretIndex > 0)
                    {
                        this.Text = text.Substring(0, this.caretIndex - 1) + text.Substring(this.caretIndex);
                        --this.CaretIndex;
                    }

                    break;

                case Key.Delete:
                    if (this.caretIndex < text.Length)
                    {
                        this.Text = text.Substring(0, this.caretIndex) + text.Substring(this.caretIndex + 1);
                    }

                    break;

                default:
                    if (!string.IsNullOrEmpty(e.Text))
                    {
                        this.Text = text.Substring(0, this.caretIndex) + e.Text + text.Substring(this.caretIndex);
                        ++this.CaretIndex;
                    }

                    break;
            }

            e.Handled = true;
        }

        private void OnPointerPressed(object sender, PointerEventArgs e)
        {
            IPlatformRenderInterface platform = Locator.Current.GetService<IPlatformRenderInterface>();
            this.CaretIndex = platform.TextService.GetCaretIndex(
                this.textBoxView.FormattedText,
                e.GetPosition(this.textBoxView),
                this.ActualSize);
        }
    }
}
