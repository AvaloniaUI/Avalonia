using Avalonia.Media.Imaging;
using Avalonia.Reactive;

namespace Avalonia.Media
{
    public readonly record struct RenderOptions
    {
        static RenderOptions()
        {
            BitmapInterpolationModeProperty.Changed.Subscribe(BitmapInterpolationModeChanged);
            TextRenderingModeProperty.Changed.Subscribe(TextRenderingModeChanged);
            EdgeModeProperty.Changed.Subscribe(EdgeModeChanged);
            BitmapBlendingModeProperty.Changed.Subscribe(BitmapBlendingModeChanged);
            RequiresFullOpacityHandlingProperty.Changed.Subscribe(RequiresFullOpacityHandlingChanged);
        }

        /// <summary>
        /// Defines the <see cref="BitmapInterpolationMode"/> property.
        /// </summary>
        public static readonly AttachedProperty<BitmapInterpolationMode> BitmapInterpolationModeProperty =
            AvaloniaProperty.RegisterAttached<RenderOptions, Visual, BitmapInterpolationMode>(
                nameof(BitmapInterpolationMode), inherits: true);
        
        /// <summary>
        /// Defines the <see cref="EdgeMode"/> property.
        /// </summary>
        public static readonly AttachedProperty<EdgeMode> EdgeModeProperty =
            AvaloniaProperty.RegisterAttached<RenderOptions, Visual, EdgeMode>(
                nameof(EdgeMode), inherits: true);

        /// <summary>
        /// Defines the <see cref="TextRenderingMode"/> property.
        /// </summary>
        public static readonly AttachedProperty<TextRenderingMode> TextRenderingModeProperty =
            AvaloniaProperty.RegisterAttached<RenderOptions, Visual, TextRenderingMode>(
                nameof(TextRenderingMode), inherits: true);
        
        /// <summary>
        /// Defines the <see cref="BitmapBlendingMode"/> property.
        /// </summary>
        public static readonly AttachedProperty<BitmapBlendingMode> BitmapBlendingModeProperty =
            AvaloniaProperty.RegisterAttached<RenderOptions, Visual, BitmapBlendingMode>(
                nameof(BitmapBlendingMode), inherits: true);
        
        /// <summary>
        /// Defines the <see cref="BitmapBlendingMode"/> property.
        /// </summary>
        public static readonly AttachedProperty<bool?> RequiresFullOpacityHandlingProperty =
            AvaloniaProperty.RegisterAttached<RenderOptions, Visual, bool?>(
                nameof(RequiresFullOpacityHandling), inherits: true);

        
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
            return visual.GetValue(BitmapInterpolationModeProperty);
        }

        /// <summary>
        /// Sets the value of the BitmapInterpolationMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        public static void SetBitmapInterpolationMode(Visual visual, BitmapInterpolationMode value)
        {
            visual.SetValue(BitmapInterpolationModeProperty, value);
        }
        
        private static void BitmapInterpolationModeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is Visual visual)
            {
                visual.RenderOptions = visual.RenderOptions with
                {
                    BitmapInterpolationMode = e.GetNewValue<BitmapInterpolationMode>()
                };
                visual.InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets the value of the BitmapBlendingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static BitmapBlendingMode GetBitmapBlendingMode(Visual visual)
        {
            return visual.GetValue(BitmapBlendingModeProperty);
        }

        /// <summary>
        /// Sets the value of the BitmapBlendingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The left value.</param>
        public static void SetBitmapBlendingMode(Visual visual, BitmapBlendingMode value)
        {
            visual.SetValue(BitmapBlendingModeProperty, value);
        }

        private static void BitmapBlendingModeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is Visual visual)
            {
                visual.RenderOptions = visual.RenderOptions with
                {
                    BitmapBlendingMode = e.GetNewValue<BitmapBlendingMode>()
                };
                visual.InvalidateVisual();
            }
        }
        
        /// <summary>
        /// Gets the value of the EdgeMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static EdgeMode GetEdgeMode(Visual visual)
        {
            return visual.GetValue(EdgeModeProperty);
        }

        /// <summary>
        /// Sets the value of the EdgeMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        public static void SetEdgeMode(Visual visual, EdgeMode value)
        {
            visual.SetValue(EdgeModeProperty, value);
        }
        
        private static void EdgeModeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is Visual visual)
            {
                visual.RenderOptions = visual.RenderOptions with
                {
                    EdgeMode = e.GetNewValue<EdgeMode>()
                };
                visual.InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets the value of the TextRenderingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static TextRenderingMode GetTextRenderingMode(Visual visual)
        {
            return visual.GetValue(TextRenderingModeProperty);
        }

        /// <summary>
        /// Sets the value of the TextRenderingMode attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        public static void SetTextRenderingMode(Visual visual, TextRenderingMode value)
        {
            visual.SetValue(TextRenderingModeProperty, value);
        }
        
        private static void TextRenderingModeChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is Visual visual)
            {
                visual.RenderOptions = visual.RenderOptions with
                {
                    TextRenderingMode = e.GetNewValue<TextRenderingMode>()
                };
                visual.InvalidateVisual();
            }
        }

        /// <summary>
        /// Gets the value of the RequiresFullOpacityHandling attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <returns>The value.</returns>
        public static bool? GetRequiresFullOpacityHandling(Visual visual)
        {
            return visual.GetValue(RequiresFullOpacityHandlingProperty);
        }

        /// <summary>
        /// Sets the value of the RequiresFullOpacityHandling attached property for a visual.
        /// </summary>
        /// <param name="visual">The control.</param>
        /// <param name="value">The value.</param>
        public static void SetRequiresFullOpacityHandling(Visual visual, bool? value)
        {
            visual.SetValue(RequiresFullOpacityHandlingProperty, value);
        }

        private static void RequiresFullOpacityHandlingChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Sender is Visual visual)
            {
                visual.RenderOptions = visual.RenderOptions with
                {
                    RequiresFullOpacityHandling = e.GetNewValue<bool?>()
                };
                visual.InvalidateVisual();
            }
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
