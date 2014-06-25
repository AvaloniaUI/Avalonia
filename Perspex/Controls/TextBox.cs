// -----------------------------------------------------------------------
// <copyright file="TextBox.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using Perspex.Styling;

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
            this.GetObservable(TextProperty).Subscribe(_ => this.InvalidateVisual());
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
        }
    }
}
