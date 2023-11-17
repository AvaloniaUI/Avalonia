using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    public readonly record struct RenderOptions
    {
        public BitmapInterpolationMode BitmapInterpolationMode { get; init; }
        public EdgeMode EdgeMode { get; init; }
        public TextRenderingMode TextRenderingMode { get; init; }
        public BitmapBlendingMode BitmapBlendingMode { get; init; }
        public bool? RequiresFullOpacityHandling { get; init; }

        /// <summary>
        /// Gets the value of the BitmapInterpolationMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static BitmapInterpolationMode GetBitmapInterpolationMode(Visual visual)
        {
            return visual.RenderOptions.BitmapInterpolationMode;
        }

        /// <summary>
        /// Sets the value of the BitmapInterpolationMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        public static void SetBitmapInterpolationMode(Visual visual, BitmapInterpolationMode value)
        {
            visual.RenderOptions = visual.RenderOptions with { BitmapInterpolationMode = value };
        }

        /// <summary>
        /// Gets the value of the BitmapBlendingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static BitmapBlendingMode GetBitmapBlendingMode(Visual visual)
        {
            return visual.RenderOptions.BitmapBlendingMode;
        }

        /// <summary>
        /// Sets the value of the BitmapBlendingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The left value.</param>
        public static void SetBitmapBlendingMode(Visual visual, BitmapBlendingMode value)
        {
            visual.RenderOptions = visual.RenderOptions with { BitmapBlendingMode = value };
        }

        /// <summary>
        /// Gets the value of the EdgeMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static EdgeMode GetEdgeMode(Visual visual)
        {
            return visual.RenderOptions.EdgeMode;
        }

        /// <summary>
        /// Sets the value of the EdgeMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        public static void SetEdgeMode(Visual visual, EdgeMode value)
        {
            visual.RenderOptions = visual.RenderOptions with { EdgeMode = value };
        }

        /// <summary>
        /// Gets the value of the TextRenderingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static TextRenderingMode GetTextRenderingMode(Visual visual)
        {
            return visual.RenderOptions.TextRenderingMode;
        }

        /// <summary>
        /// Sets the value of the TextRenderingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        public static void SetTextRenderingMode(Visual visual, TextRenderingMode value)
        {
            visual.RenderOptions = visual.RenderOptions with { TextRenderingMode = value };
        }

        /// <summary>
        /// Gets the value of the RequiresFullOpacityHandling attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static bool? GetRequiresFullOpacityHandling(Visual visual)
        {
            return visual.RenderOptions.RequiresFullOpacityHandling;
        }

        /// <summary>
        /// Sets the value of the RequiresFullOpacityHandling attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        public static void SetRequiresFullOpacityHandling(Visual visual, bool? value)
        {
            visual.RenderOptions = visual.RenderOptions with { RequiresFullOpacityHandling = value };
        }

        public RenderOptions MergeWith(RenderOptions other)
        {
            var bitmapInterpolationMode = BitmapInterpolationMode;

            if (bitmapInterpolationMode == BitmapInterpolationMode.Unspecified)
            {
                bitmapInterpolationMode = other.BitmapInterpolationMode;
            }

            var edgeMode = EdgeMode;

            if (edgeMode == EdgeMode.Unspecified)
            {
                edgeMode = other.EdgeMode;
            }

            var textRenderingMode = TextRenderingMode;

            if (textRenderingMode == TextRenderingMode.Unspecified)
            {
                textRenderingMode = other.TextRenderingMode;
            }

            var bitmapBlendingMode = BitmapBlendingMode;

            if (bitmapBlendingMode == BitmapBlendingMode.Unspecified)
            {
                bitmapBlendingMode = other.BitmapBlendingMode;
            }

            var requiresFullOpacityHandling = RequiresFullOpacityHandling;

            if (requiresFullOpacityHandling == null)
            {
                requiresFullOpacityHandling = other.RequiresFullOpacityHandling;
            }

            return new RenderOptions
            {
                BitmapInterpolationMode = bitmapInterpolationMode,
                EdgeMode = edgeMode,
                TextRenderingMode = textRenderingMode,
                BitmapBlendingMode = bitmapBlendingMode,
                RequiresFullOpacityHandling = requiresFullOpacityHandling
            };
        }
    }
}
