using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// A text block that displays a character prefixed with an underscore as an access key.
    /// </summary>
    public class AccessText : TextBlock
    {
        /// <summary>
        /// Defines the <see cref="ShowAccessKey"/> attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> ShowAccessKeyProperty =
            AvaloniaProperty.RegisterAttached<AccessText, Control, bool>("ShowAccessKey", inherits: true);

        /// <summary>
        /// The access key handler for the current window.
        /// </summary>
        private IAccessKeyHandler? _accessKeys;

        /// <summary>
        /// Initializes static members of the <see cref="AccessText"/> class.
        /// </summary>
        static AccessText()
        {
            AffectsRender<AccessText>(ShowAccessKeyProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AccessText"/> class.
        /// </summary>
        public AccessText()
        {
            this.GetObservable(TextProperty).Subscribe(TextChanged);
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
            get { return GetValue(ShowAccessKeyProperty); }
            set { SetValue(ShowAccessKeyProperty, value); }
        }

        /// <summary>
        /// Renders the <see cref="AccessText"/> to a drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            int underscore = Text?.IndexOf('_') ?? -1;

            if (underscore != -1 && ShowAccessKey)
            {
                var rect = TextLayout!.HitTestTextPosition(underscore);
                var offset = new Vector(0, -1.5);
                context.DrawLine(
                    new Pen(Foreground, 1),
                    rect.BottomLeft + offset,
                    rect.BottomRight + offset);
            }
        }

        /// <inheritdoc/>
        protected override TextLayout? CreateTextLayout(Size constraint, string? text)
        {
            return base.CreateTextLayout(constraint, StripAccessKey(text));
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _accessKeys = (e.Root as IInputRoot)?.AccessKeyHandler;

            if (_accessKeys != null && AccessKey != 0)
            {
                _accessKeys.Register(AccessKey, this);
            }
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (_accessKeys != null && AccessKey != 0)
            {
                _accessKeys.Unregister(this);
                _accessKeys = null;
            }
        }

        /// <summary>
        /// Returns a string with the first underscore stripped.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The text with the first underscore stripped.</returns>
        [return: NotNullIfNotNull("text")]
        private string? StripAccessKey(string? text)
        {
            if (text is null)
            {
                return null;
            }

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
        /// Called when the <see cref="TextBlock.Text"/> property changes.
        /// </summary>
        /// <param name="text">The new text.</param>
        private void TextChanged(string? text)
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

            AccessKey = key;

            if (_accessKeys != null && AccessKey != 0)
            {
                _accessKeys.Register(AccessKey, this);
            }
        }
    }
}
