// -----------------------------------------------------------------------
// <copyright file="AccessText.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Primitives
{
    using System;
    using Perspex.Media;
    using Perspex.Input;
    using Perspex.Rendering;



    /// <summary>
    /// A text block that displays a character prefixed with an underscore as an access key.
    /// </summary>
    public class AccessText : TextBlock
    {
        /// <summary>
        /// Defines the <see cref="ShowAccessKey"/> attached property.
        /// </summary>
        public static readonly PerspexProperty<bool> ShowAccessKeyProperty =
            PerspexProperty.RegisterAttached<AccessText, Control, bool>("ShowAccessKey", inherits: true);

        /// <summary>
        /// The access key handler for the current window.
        /// </summary>
        private IAccessKeyHandler accessKeys;

        /// <summary>
        /// Initializes static members of the <see cref="AccessText"/> class.
        /// </summary>
        static AccessText()
        {
            AffectsRender(ShowAccessKeyProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessText"/> class.
        /// </summary>
        public AccessText()
        {
            this.GetObservable(TextProperty).Subscribe(this.TextChanged);
        }

        /// <summary>
        /// Gets the access key.
        /// </summary>
        public char AccessKey
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the access key should be underlined.
        /// </summary>
        public bool ShowAccessKey
        {
            get { return this.GetValue(ShowAccessKeyProperty); }
            set { this.SetValue(ShowAccessKeyProperty, value); }
        }

        /// <summary>
        /// Renders the <see cref="AccessText"/> to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(IDrawingContext context)
        {
            base.Render(context);

            int underscore = this.Text?.IndexOf('_') ?? -1;

            if (underscore != -1 && this.ShowAccessKey)
            {
                var rect = this.FormattedText.HitTestTextPosition(underscore);
                var offset = new Vector(0, -0.5);
                context.DrawLine(
                    new Pen(this.Foreground, 1),
                    rect.BottomLeft + offset,
                    rect.BottomRight + offset);
            }
        }

        /// <summary>
        /// Creates the <see cref="FormattedText"/> used to render the text.
        /// </summary>
        /// <param name="constraint">The constraint of the text.</param>
        /// <returns>A <see cref="FormattedText"/> object.</returns>
        protected override FormattedText CreateFormattedText(Size constraint)
        {
            var result = new FormattedText(
                this.StripAccessKey(this.Text),
                this.FontFamily,
                this.FontSize,
                this.FontStyle,
                this.TextAlignment,
                this.FontWeight);
            result.Constraint = constraint;
            return result;
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size for the control.</param>
        /// <returns>The desired size.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            var result = base.MeasureOverride(availableSize);
            return result.WithHeight(result.Height + 1);
        }

        /// <summary>
        /// Called when the control is attached to a visual tree.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);
            this.accessKeys = (root as IInputRoot)?.AccessKeyHandler;

            if (this.accessKeys != null && this.AccessKey != 0)
            {
                this.accessKeys.Register(this.AccessKey, this);
            }
        }

        /// <summary>
        /// Called when the control is detached from a visual tree.
        /// </summary>
        /// <param name="root">The root of the visual tree.</param>
        protected override void OnDetachedFromVisualTree(IRenderRoot root)
        {
            base.OnDetachedFromVisualTree(root);

            if (this.accessKeys != null && this.AccessKey != 0)
            {
                this.accessKeys.Unregister(this);
                this.accessKeys = null;
            }
        }

        /// <summary>
        /// Returns a string with the first underscore stripped.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The text with the first underscore stripped.</returns>
        private string StripAccessKey(string text)
        {
            var position = text.IndexOf('_');

            if (position == -1)
            {
                return text;
            }
            else
            {
                return text.Substring(0, position) + text.Substring(position + 1);
            }
        }

        /// <summary>
        /// Called when the <see cref="Text"/> property changes.
        /// </summary>
        /// <param name="text">The new text.</param>
        private void TextChanged(string text)
        {
            var key = (char)0;

            if (text != null)
            {
                int underscore = text.IndexOf('_');

                if (underscore != -1 && underscore < text.Length - 1)
                {
                    key = text[underscore + 1];
                }
            }

            this.AccessKey = key;

            if (this.accessKeys != null && this.AccessKey != 0)
            {
                this.accessKeys.Register(this.AccessKey, this);
            }
        }
    }
}
