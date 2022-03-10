using System;
using Avalonia.Media;

namespace Avalonia.Controls.Documents
{
    /// <summary>
    /// TextElement is an  base class for content in text based controls.
    /// TextElements span other content, applying property values or providing structural information.
    /// </summary>
    public abstract class TextElement : StyledElement
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            AvaloniaProperty.Register<TextElement, IBrush?>(nameof(Background), inherits: true);

        /// <summary>
        /// Defines the <see cref="FontFamily"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontFamily> FontFamilyProperty =
            TextBlock.FontFamilyProperty.AddOwner<TextElement>();

        /// <summary>
        /// Defines the <see cref="FontSize"/> property.
        /// </summary>
        public static readonly AttachedProperty<double> FontSizeProperty =
            TextBlock.FontSizeProperty.AddOwner<TextElement>();

        /// <summary>
        /// Defines the <see cref="FontStyle"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontStyle> FontStyleProperty =
            TextBlock.FontStyleProperty.AddOwner<TextElement>();

        /// <summary>
        /// Defines the <see cref="FontWeight"/> property.
        /// </summary>
        public static readonly AttachedProperty<FontWeight> FontWeightProperty =
            TextBlock.FontWeightProperty.AddOwner<TextElement>();

        /// <summary>
        /// Defines the <see cref="Foreground"/> property.
        /// </summary>
        public static readonly AttachedProperty<IBrush?> ForegroundProperty =
            TextBlock.ForegroundProperty.AddOwner<TextElement>();

        /// <summary>
        /// Gets or sets a brush used to paint the control's background.
        /// </summary>
        public IBrush? Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font family.
        /// </summary>
        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font size.
        /// </summary>
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font style.
        /// </summary>
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the font weight.
        /// </summary>
        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush used to paint the text.
        /// </summary>
        public IBrush? Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }
        
        /// <summary>
        /// Raised when the visual representation of the text element changes.
        /// </summary>
        public event EventHandler? Invalidated;

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(Background):
                case nameof(FontFamily):
                case nameof(FontSize):
                case nameof(FontStyle):
                case nameof(FontWeight):
                case nameof(Foreground):
                    Invalidate();
                    break;
            }
        }

        /// <summary>
        /// Raises the <see cref="Invalidate"/> event.
        /// </summary>
        protected void Invalidate() => Invalidated?.Invoke(this, EventArgs.Empty);
    }
}
