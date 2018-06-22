using System;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Styling;

namespace Avalonia.Documents
{
    /// <summary>
    /// Base class for <see cref="Inline"/> text elements.
    /// </summary>
    public abstract class TextElement : StyledElement
    {
        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, FontFamily>(
                nameof(FontFamily),
                defaultValue: Media.FontFamily.Default,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> FontSizeProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, double>(
                nameof(FontSize),
                defaultValue: 12,
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, FontStyle>(
                nameof(FontStyle),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, FontWeight>(
                nameof(FontWeight),
                inherits: true,
                defaultValue: FontWeight.Normal);

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly AttachedProperty<IBrush> ForegroundProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, IBrush>(
                nameof(Foreground),
                new SolidColorBrush(0xff000000),
                inherits: true);

        /// <summary>
        /// Defines the <see cref="TextDecorations"/> property.
        /// </summary>
        public static readonly AttachedProperty<TextDecorations> TextDecorationsProperty =
            AvaloniaProperty.RegisterAttached<TextElement, AvaloniaObject, TextDecorations>(
                nameof(TextDecorations));

        bool _isAttachedToLogicalTree;
        ILogical _parent;

        static TextElement()
        {
            InvalidatesTextElement<TextElement>(
                FontSizeProperty,
                FontStyleProperty,
                FontWeightProperty,
                ForegroundProperty,
                TextDecorationsProperty);
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get => GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get => GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get => GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get => GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground brush used to paint the element.
        /// </summary>
        public IBrush Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the decorations applied to the element.
        /// </summary>
        public TextDecorations TextDecorations
        {
            get => GetValue(TextDecorationsProperty);
            set => SetValue(TextDecorationsProperty, value);
        }

        public event EventHandler Invalidated;

        protected static void InvalidatesTextElement<T>(params AvaloniaProperty[] properties)
            where T : TextElement
        {
            void Handler(AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Sender is T i)
                {
                    i.Invalidate();
                }
            }

            foreach (var property in properties)
            {
                property.Changed.Subscribe(Handler);
            }
        }

        protected void Invalidate() => Invalidated?.Invoke(this, EventArgs.Empty);
    }
}
