using System;
using Avalonia.Media.Imaging;

namespace Avalonia.Media
{
    /// <summary>
    /// Provides a set of options that control rendering behavior for visuals, including text rendering, bitmap
    /// interpolation, edge rendering, blending, and opacity handling.
    /// </summary>
    /// <remarks>Use this structure to specify rendering preferences for visual elements. Each property
    /// corresponds to a specific aspect of rendering, allowing fine-grained control over how content is displayed.
    /// These options can be applied to visuals to influence quality, performance, and visual effects. When merging two
    /// instances, unspecified values are inherited from the other instance, enabling layered configuration.</remarks>
    public readonly record struct RenderOptions
    {
        /// <summary>
        /// Gets the text rendering mode used to control how text glyphs are rendered.
        /// </summary>
        [Obsolete("TextRenderingMode is obsolete. Use TextOptions.TextRenderingMode instead.")]
        public TextRenderingMode TextRenderingMode { get; init; }

        /// <summary>
        /// Gets the interpolation mode used when rendering bitmap images.
        /// </summary>
        /// <remarks>The interpolation mode determines how bitmap images are scaled or transformed during
        /// rendering. Selecting an appropriate mode can affect image quality and performance.
        /// </remarks>
        public BitmapInterpolationMode BitmapInterpolationMode { get; init; }

        /// <summary>
        /// Gets the edge rendering mode used for drawing operations.
        /// </summary>
        public EdgeMode EdgeMode { get; init; }

        /// <summary>
        /// Gets the blending mode used when rendering bitmap images.
        /// </summary>
        /// <remarks>The blending mode determines how bitmap pixels are composited with the background or
        /// other images. Select an appropriate mode based on the desired visual effect, such as transparency or
        /// additive blending.</remarks>
        public BitmapBlendingMode BitmapBlendingMode { get; init; }

        /// <summary>
        /// Gets a value indicating whether full opacity handling is required for the associated content.
        /// </summary>
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
        [Obsolete("TextRenderingMode is obsolete. Use TextOptions.TextRenderingMode instead.")]
        public static TextRenderingMode GetTextRenderingMode(Visual visual)
        {
            return visual.RenderOptions.TextRenderingMode;
        }

        /// <summary>
        /// Sets the value of the TextRenderingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        [Obsolete("TextRenderingMode is obsolete. Use TextOptions.TextRenderingMode instead.")]
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

#pragma warning disable CS0618
            var textRenderingMode = TextRenderingMode;
#pragma warning restore CS0618

            if (textRenderingMode == TextRenderingMode.Unspecified)
            {
#pragma warning disable CS0618
                textRenderingMode = other.TextRenderingMode;
#pragma warning disable CS0618
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
