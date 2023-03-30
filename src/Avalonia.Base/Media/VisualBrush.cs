using Avalonia.Media.Immutable;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="Visual"/>.
    /// </summary>
    public class VisualBrush : TileBrush, ISceneBrush, IAffectsRender
    {
        /// <summary>
        /// Defines the <see cref="Visual"/> property.
        /// </summary>
        public static readonly StyledProperty<Visual?> VisualProperty =
            AvaloniaProperty.Register<VisualBrush, Visual?>(nameof(Visual));

        static VisualBrush()
        {
            AffectsRender<VisualBrush>(VisualProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        public VisualBrush()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualBrush"/> class.
        /// </summary>
        /// <param name="visual">The visual to draw.</param>
        public VisualBrush(Visual visual)
        {
            Visual = visual;
        }

        /// <summary>
        /// Gets or sets the visual to draw.
        /// </summary>
        public Visual? Visual
        {
            get { return GetValue(VisualProperty); }
            set { SetValue(VisualProperty, value); }
        }

        ISceneBrushContent? ISceneBrush.CreateContent()
        {
            if (Visual == null)
                return null;

            if (Visual is IVisualBrushInitialize initialize)
                initialize.EnsureInitialized();
            
            var recorder = new CompositionDrawingContext();
            recorder.BeginUpdate(null);
            ImmediateRenderer.Render(recorder, Visual, Visual.Bounds);
            var drawList = recorder.EndUpdate();
            if (drawList == null)
                return null;

            return new CompositionDrawListSceneBrushContent(new ImmutableSceneBrush(this), drawList,
                new(Visual.Bounds.Size), false);
        }
    }
}
