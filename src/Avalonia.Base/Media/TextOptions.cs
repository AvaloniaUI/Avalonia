namespace Avalonia.Media
{
    /// <summary>
    /// Provides options for controlling text rendering behavior, including rendering mode, hinting mode, and baseline
    /// pixel alignment. Used to configure how text appears within visual elements.
    /// </summary>
    /// <remarks>TextOptions encapsulates settings that influence the clarity, sharpness, and positioning of
    /// rendered text. These options can be applied to visual elements to customize text appearance for different
    /// display scenarios, such as optimizing for readability at small font sizes or ensuring pixel-perfect alignment.
    /// The struct supports merging with other instances to inherit unspecified values, and exposes attached properties
    /// for use with visuals.</remarks>
    public readonly record struct TextOptions
    {
        /// <summary>
        /// Gets the text rendering mode used to control how text glyphs are rendered.
        /// </summary>
        public TextRenderingMode TextRenderingMode { get; init; }

        /// <summary>
        /// Gets the text rendering hinting mode used to optimize the display of text.
        /// </summary>
        /// <remarks>The hinting mode determines how text is rendered to improve clarity and readability,
        /// especially at small font sizes. Changing this value may affect the appearance of text depending on the
        /// rendering engine and display device.</remarks>
        public TextHintingMode TextHintingMode { get; init; }

        /// <summary>
        /// Gets a value indicating whether the text baseline should be aligned to the pixel grid.
        /// </summary>
        /// <remarks>
        /// When enabled, the vertical position of the text baseline is snapped to whole pixel boundaries.
        /// This ensures consistent sharpness and reduces blurriness caused by fractional positioning,
        /// particularly at small font sizes or low DPI settings.
        /// </remarks>
        public BaselinePixelAlignment BaselinePixelAlignment { get; init; }

        /// <summary>
        /// Merges this instance with <paramref name="other"/> using inheritance semantics: unspecified values on this
        /// instance are taken from <paramref name="other"/>.
        /// </summary>
        public TextOptions MergeWith(TextOptions other)
        {
            var textRenderingMode = TextRenderingMode;

            if (textRenderingMode == TextRenderingMode.Unspecified)
            {
                textRenderingMode = other.TextRenderingMode;
            }

            var textHintingMode = TextHintingMode;

            if (textHintingMode == TextHintingMode.Unspecified)
            {
                textHintingMode = other.TextHintingMode;
            }

            var baselinePixelAlignment = BaselinePixelAlignment;

            if (baselinePixelAlignment == BaselinePixelAlignment.Unspecified)
            {
                baselinePixelAlignment = other.BaselinePixelAlignment;
            }

            return new TextOptions
            {
                TextRenderingMode = textRenderingMode,
                TextHintingMode = textHintingMode,
                BaselinePixelAlignment = baselinePixelAlignment
            };
        }

        /// <summary>
        /// Gets the TextOptions attached value for a visual.
        /// </summary>
        public static TextOptions GetTextOptions(Visual visual)
        {
            return visual.TextOptions;
        }

        /// <summary>
        /// Sets the TextOptions attached value for a visual.
        /// </summary>
        public static void SetTextOptions(Visual visual, TextOptions value)
        {
            visual.TextOptions = value;
        }

        /// <summary>
        /// Gets the TextRenderingMode attached property for a visual.
        /// </summary>
        public static TextRenderingMode GetTextRenderingMode(Visual visual)
        {
            return visual.TextOptions.TextRenderingMode;
        }

        /// <summary>
        /// Sets the TextRenderingMode attached property for a visual.
        /// </summary>
        public static void SetTextRenderingMode(Visual visual, TextRenderingMode value)
        {
            visual.TextOptions = visual.TextOptions with { TextRenderingMode = value };
        }

        /// <summary>
        /// Gets the TextHintingMode attached property for a visual.
        /// </summary>
        public static TextHintingMode GetTextHintingMode(Visual visual)
        {
            return visual.TextOptions.TextHintingMode;
        }

        /// <summary>
        /// Sets the TextHintingMode attached property for a visual.
        /// </summary>
        public static void SetTextHintingMode(Visual visual, TextHintingMode value)
        {
            visual.TextOptions = visual.TextOptions with { TextHintingMode = value };
        }

        /// <summary>
        /// Gets the BaselinePixelAlignment attached property for a visual.
        /// </summary>
        public static BaselinePixelAlignment GetBaselinePixelAlignment(Visual visual)
        {
            return visual.TextOptions.BaselinePixelAlignment;
        }

        /// <summary>
        /// Sets the BaselinePixelAlignment attached property for a visual.
        /// </summary>
        public static void SetBaselinePixelAlignment(Visual visual, BaselinePixelAlignment value)
        {
            visual.TextOptions = visual.TextOptions with { BaselinePixelAlignment = value };
        }
    }
}
