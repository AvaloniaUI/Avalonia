using Avalonia.Collections;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Utils;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control which decorates a child with a border and background.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    public partial class Border : Decorator, IVisualWithRoundRectClip
#pragma warning restore CS0618 // Type or member is obsolete
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            AvaloniaProperty.Register<Border, IBrush>(nameof(Background));

        /// <summary>
        /// Defines the <see cref="BorderBrush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BorderBrushProperty =
            AvaloniaProperty.Register<Border, IBrush>(nameof(BorderBrush));

        /// <summary>
        /// Gets or sets a collection of <see cref="double"/> values that indicate the pattern of dashes and gaps that is used to outline shapes.
        /// </summary>
        public AvaloniaList<double>? StrokeDashArray
        {
            get { return GetValue(StrokeDashArrayProperty); }
            set { SetValue(StrokeDashArrayProperty, value); }
        }
        
        /// <summary>
        /// Defines the <see cref="BorderThickness"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> BorderThicknessProperty =
            AvaloniaProperty.Register<Border, Thickness>(nameof(BorderThickness));

        /// <summary>
        /// Defines the <see cref="CornerRadius"/> property.
        /// </summary>
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
            AvaloniaProperty.Register<Border, CornerRadius>(nameof(CornerRadius));

        /// <summary>
        /// Defines the <see cref="BoxShadow"/> property.
        /// </summary>
        public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
            AvaloniaProperty.Register<Border, BoxShadows>(nameof(BoxShadow));
        
        /// <summary>
        /// Defines the <see cref="StrokeDashOffset"/> property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeDashOffsetProperty =
            AvaloniaProperty.Register<Shape, double>(nameof(StrokeDashOffset));
        
        /// <summary>
        /// Defines the <see cref="StrokeDashArray"/> property.
        /// </summary>
        public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty =
            AvaloniaProperty.Register<Shape, AvaloniaList<double>?>(nameof(StrokeDashArray));
        
        /// <summary>
        /// Defines the <see cref="StrokeLineCap"/> property.
        /// </summary>
        public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty =
            AvaloniaProperty.Register<Shape, PenLineCap>(nameof(StrokeLineCap), PenLineCap.Flat);

        /// <summary>
        /// Defines the <see cref="StrokeJoin"/> property.
        /// </summary>
        public static readonly StyledProperty<PenLineJoin> StrokeJoinProperty =
            AvaloniaProperty.Register<Shape, PenLineJoin>(nameof(StrokeJoin), PenLineJoin.Miter);
        
        private readonly BorderRenderHelper _borderRenderHelper = new BorderRenderHelper();

        /// <summary>
        /// Initializes static members of the <see cref="Border"/> class.
        /// </summary>
        static Border()
        {
            AffectsRender<Border>(
                BackgroundProperty,
                BorderBrushProperty,
                BorderThicknessProperty,
                CornerRadiusProperty,
                StrokeDashArrayProperty,
                StrokeLineCapProperty,
                StrokeJoinProperty,
                StrokeDashOffsetProperty,
                BoxShadowProperty);
            AffectsMeasure<Border>(BorderThicknessProperty);
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the background.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        /// <summary>
        /// Gets or sets a brush with which to paint the border.
        /// </summary>
        public IBrush BorderBrush
        {
            get { return GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        /// <summary>
        /// Gets or sets the thickness of the border.
        /// </summary>
        public Thickness BorderThickness
        {
            get { return GetValue(BorderThicknessProperty); }
            set { SetValue(BorderThicknessProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets a value that specifies the distance within the dash pattern where a dash begins.
        /// </summary>
        public double StrokeDashOffset
        {
            get { return GetValue(StrokeDashOffsetProperty); }
            set { SetValue(StrokeDashOffsetProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets a <see cref="PenLineCap"/> enumeration value that describes the shape at the ends of a line.
        /// </summary>
        public PenLineCap StrokeLineCap
        {
            get { return GetValue(StrokeLineCapProperty); }
            set { SetValue(StrokeLineCapProperty, value); }
        }

        /// <summary>
        /// Gets or sets a <see cref="PenLineJoin"/> enumeration value that specifies the type of join that is used at the vertices of a Shape.
        /// </summary>
        public PenLineJoin StrokeJoin
        {
            get { return GetValue(StrokeJoinProperty); }
            set { SetValue(StrokeJoinProperty, value); }
        }

        /// <summary>
        /// Gets or sets the radius of the border rounded corners.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }
        
        /// <summary>
        /// Gets or sets the box shadow effect parameters
        /// </summary>
        public BoxShadows BoxShadow
        {
            get => GetValue(BoxShadowProperty);
            set => SetValue(BoxShadowProperty, value);
        }
        
        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            _borderRenderHelper.Render(context, Bounds.Size, BorderThickness, CornerRadius,  Background, BorderBrush, 
                BoxShadow, StrokeDashOffset, StrokeLineCap, StrokeJoin, StrokeDashArray);
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <returns>The desired size of the control.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureChild(Child, availableSize, Padding, BorderThickness);
        }

        /// <summary>
        /// Arranges the control's child.
        /// </summary>
        /// <param name="finalSize">The size allocated to the control.</param>
        /// <returns>The space taken.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return LayoutHelper.ArrangeChild(Child, finalSize, Padding, BorderThickness);
        }

        public CornerRadius ClipToBoundsRadius => CornerRadius;
    }
}
