using Avalonia.Media.Immutable;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="Drawing"/>.
    /// </summary>
    public class DrawingBrush : TileBrush, ISceneBrush, IAffectsRender
    {
        /// <summary>
        /// Defines the <see cref="Drawing"/> property.
        /// </summary>
        public static readonly StyledProperty<Drawing?> DrawingProperty =
            AvaloniaProperty.Register<DrawingBrush, Drawing?>(nameof(Drawing));

        static DrawingBrush()
        {
            AffectsRender<DrawingBrush>(DrawingProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingBrush"/> class.
        /// </summary>
        public DrawingBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingBrush"/> class.
        /// </summary>
        /// <param name="visual">The visual to draw.</param>
        public DrawingBrush(Drawing visual)
        {
            Drawing = visual;
        }

        /// <summary>
        /// Gets or sets the visual to draw.
        /// </summary>
        public Drawing? Drawing
        {
            get { return GetValue(DrawingProperty); }
            set { SetValue(DrawingProperty, value); }
        }

        ISceneBrushContent? ISceneBrush.CreateContent()
        {
            if (Drawing == null)
                return null;
            
            
            var recorder = new CompositionDrawingContext();
            recorder.BeginUpdate(null);
            Drawing?.Draw(recorder);
            var drawList = recorder.EndUpdate();
            if (drawList == null)
                return null;
            
            return new CompositionDrawListSceneBrushContent(new ImmutableSceneBrush(this), drawList,
                drawList.CalculateBounds(), true);
        }
    }
}
