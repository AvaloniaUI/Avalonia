using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Media.TextFormatting;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Sandbox
{
    public partial class MainWindow : Window
    {
        private int[] clockCodepoints = new int[]
{
    0x1F550, // 🕐 Clock Face One O’Clock
    0x1F551, // 🕑 Clock Face Two O’Clock
    0x1F552, // 🕒 Clock Face Three O’Clock
    0x1F553, // 🕓 Clock Face Four O’Clock
    0x1F554, // 🕔 Clock Face Five O’Clock
    0x1F555, // 🕕 Clock Face Six O’Clock
    0x1F556, // 🕖 Clock Face Seven O’Clock
    0x1F557, // 🕗 Clock Face Eight O’Clock
    0x1F558, // 🕘 Clock Face Nine O’Clock
    0x1F559, // 🕙 Clock Face Ten O’Clock
    0x1F55A, // 🕚 Clock Face Eleven O’Clock
    0x1F55B, // 🕛 Clock Face Twelve O’Clock

    0x1F55C, // 🕜 Clock Face One-Thirty
    0x1F55D, // 🕝 Clock Face Two-Thirty
    0x1F55E, // 🕞 Clock Face Three-Thirty
    0x1F55F, // 🕟 Clock Face Four-Thirty
    0x1F560, // 🕠 Clock Face Five-Thirty
    0x1F561, // 🕡 Clock Face Six-Thirty
    0x1F562, // 🕢 Clock Face Seven-Thirty
    0x1F563, // 🕣 Clock Face Eight-Thirty
    0x1F564, // 🕤 Clock Face Nine-Thirty
    0x1F565, // 🕥 Clock Face Ten-Thirty
    0x1F566, // 🕦 Clock Face Eleven-Thirty
    0x1F567  // 🕧 Clock Face Twelve-Thirty
};

        public MainWindow()
        {
            InitializeComponent();

            var fontCollection = new EmbeddedFontCollection(new Uri("fonts:colr"), new Uri("resm:Sandbox?assembly=Sandbox", UriKind.Absolute));

            FontManager.Current.AddFontCollection(fontCollection);

            var notoColorEmojiTypeface = new Typeface("Noto Color Emoji");
            var notoColorEmojiGlyphTypeface = notoColorEmojiTypeface.GlyphTypeface;

            var segoeUiEmojiTypeface = new Typeface("Segoe UI Emoji");
            var segoeUiEmojiGlyphTypeface = segoeUiEmojiTypeface.GlyphTypeface;

            var wrap = new WrapPanel
            {
                Margin = new Thickness(8),
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            foreach (var (c, g) in notoColorEmojiGlyphTypeface.CharacterToGlyphMap)
            {
                // Create a glyph control for each glyph
                var glyphControl = new GlyphControl
                {
                    GlyphTypeface = notoColorEmojiGlyphTypeface,
                    GlyphId = g,
                    Width = 66,
                    Height = 66,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var border = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    MinHeight = 80,
                    MinWidth = 80,
                    Padding = new Thickness(4),
                    Child = new Grid
                    {
                        Children = { glyphControl }
                    },
                    Margin = new Thickness(4)
                };

                wrap.Children.Add(border);


                if (segoeUiEmojiGlyphTypeface.CharacterToGlyphMap.TryGetValue(c, out var sg))
                {
                    wrap.Children.Add(new Border
                    {
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = new Thickness(1),
                        MinHeight = 80,
                        MinWidth = 80,
                        Padding = new Thickness(4),
                        Child = new Grid
                        {
                            Children = {
                                new GlyphControl
                                {
                                    GlyphTypeface = segoeUiEmojiGlyphTypeface,
                                    GlyphId = sg,
                                    Width = 66,
                                    Height = 66,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    VerticalAlignment = VerticalAlignment.Center
                                } }
                        },
                        Margin = new Thickness(4)
                    });
                }
            }

            Content = new ScrollViewer { Content = wrap };
        }
    }

    /// <summary>
    /// Custom control that renders a single glyph using GetGlyphDrawing for color glyphs
    /// and GetGlyphOutline for outline glyphs.
    /// </summary>
    public class GlyphControl : Control
    {
        public static readonly StyledProperty<IGlyphTypeface?> GlyphTypefaceProperty =
            AvaloniaProperty.Register<GlyphControl, IGlyphTypeface?>(nameof(GlyphTypeface));

        public static readonly StyledProperty<ushort> GlyphIdProperty =
            AvaloniaProperty.Register<GlyphControl, ushort>(nameof(GlyphId));

        public static readonly StyledProperty<IBrush?> ForegroundProperty =
            AvaloniaProperty.Register<GlyphControl, IBrush?>(nameof(Foreground), Brushes.Black);

        static GlyphControl()
        {
            AffectsRender<GlyphControl>(GlyphTypefaceProperty, GlyphIdProperty, ForegroundProperty);
        }

        public IGlyphTypeface? GlyphTypeface
        {
            get => GetValue(GlyphTypefaceProperty);
            set => SetValue(GlyphTypefaceProperty, value);
        }

        public ushort GlyphId
        {
            get => GetValue(GlyphIdProperty);
            set => SetValue(GlyphIdProperty, value);
        }

        public IBrush? Foreground
        {
            get => GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var glyphTypeface = GlyphTypeface;
            if (glyphTypeface == null)
            {
                return;
            }

            var glyphId = GlyphId;

            // Calculate scale
            var targetSize = Math.Min(Width, Height);
            var designEmHeight = glyphTypeface.Metrics.DesignEmHeight;
            var scale = targetSize / designEmHeight;

            // Try to get color glyph drawing first
            var glyphDrawing = glyphTypeface.GetGlyphDrawing(glyphId);

            if (glyphDrawing != null)
            {
                var bounds = glyphDrawing.Bounds;

                var offsetX = (Width / scale - bounds.Width) / 2 - bounds.Left;
                var offsetY = (Height / scale - bounds.Height) / 2 - bounds.Top;

                using (context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY) * Matrix.CreateScale(scale, scale)))
                {
                    glyphDrawing.Draw(context, new Point());
                }
            }
            else
            {
                // Outline glyph
                var glyphOutline = glyphTypeface.GetGlyphOutline(glyphId, Matrix.CreateScale(1, -1));

                if (glyphOutline != null)
                {
                    // Get tight bounds of scaled geometry
                    var bounds = glyphOutline.Bounds;

                    // Calculate transform based on bounds
                    var offsetX = (Width / scale - bounds.Width) / 2 - bounds.Left;
                    var offsetY = (Height / scale - bounds.Height) / 2 - bounds.Top;

                    // Apply transform and render
                    using (context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY) * Matrix.CreateScale(scale, scale)))
                    {
                        context.DrawGeometry(Foreground, null, glyphOutline);
                    }
                }
            }
        }
    }
}
