using System;
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
        private IAccessKeyHandler _accessKeys;

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
                var rect = HitTestTextPosition(underscore);
                var offset = new Vector(0, -0.5);
                context.DrawLine(
                    new Pen(Foreground, 1),
                    rect.BottomLeft + offset,
                    rect.BottomRight + offset);
            }
        }

        /// <summary>
        /// Get the pixel location relative to the top-left of the layout box given the text position.
        /// </summary>
        /// <param name="textPosition">The text position.</param>
        /// <returns></returns>
        private Rect HitTestTextPosition(int textPosition)
        {
            if (TextLayout == null)
            {
                return new Rect();
            }

            if (TextLayout.TextLines.Count == 0)
            {
                return new Rect();
            }

            if (textPosition < 0 || textPosition >= Text.Length)
            {
                var lastLine = TextLayout.TextLines[TextLayout.TextLines.Count - 1];

                var lineX = lastLine.LineMetrics.Size.Width;

                var lineY = Bounds.Height - lastLine.LineMetrics.Size.Height;

                return new Rect(lineX, lineY, 0, lastLine.LineMetrics.Size.Height);
            }

            var currentY = 0.0;

            foreach (var textLine in TextLayout.TextLines)
            {
                if (textLine.TextRange.End < textPosition)
                {
                    currentY += textLine.LineMetrics.Size.Height;

                    continue;
                }

                var currentX = 0.0;

                foreach (var textRun in textLine.TextRuns)
                {
                    if (!(textRun is ShapedTextCharacters shapedTextCharacters))
                    {
                        continue;
                    }

                    if (shapedTextCharacters.GlyphRun.Characters.End < textPosition)
                    {
                        currentX += shapedTextCharacters.GlyphRun.Bounds.Width;

                        continue;
                    }

                    var characterHit =
                        shapedTextCharacters.GlyphRun.FindNearestCharacterHit(textPosition, out var width);

                    var distance = shapedTextCharacters.GlyphRun.GetDistanceFromCharacterHit(characterHit);

                    currentX += distance - width;

                    if (characterHit.TrailingLength == 0)
                    {
                        width = 0.0;
                    }

                    return new Rect(currentX, currentY, width, shapedTextCharacters.GlyphRun.Bounds.Height);
                }
            }

            return new Rect();
        }

        /// <inheritdoc/>
        protected override TextLayout CreateTextLayout(Size constraint, string text)
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
        /// Called when the <see cref="TextBlock.Text"/> property changes.
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

            AccessKey = key;

            if (_accessKeys != null && AccessKey != 0)
            {
                _accessKeys.Register(AccessKey, this);
            }
        }
    }
}
