using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace ControlCatalog
{
	public enum AxisPlotMode
	{
		Normal,
		EvenlySpaced,
		Logarithmic
	}

	public partial class LineChart
	{
		// Area

		public static readonly StyledProperty<Thickness> AreaMarginProperty =
			AvaloniaProperty.Register<LineChart, Thickness>(nameof(AreaMargin));

		public static readonly StyledProperty<IBrush?> AreaFillProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(AreaFill));

		public static readonly StyledProperty<IBrush?> AreaStrokeProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(AreaStroke));

		public static readonly StyledProperty<double> AreaStrokeThicknessProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(AreaStrokeThickness), 1.0);

		public static readonly StyledProperty<IDashStyle?> AreaStrokeDashStyleProperty =
			AvaloniaProperty.Register<LineChart, IDashStyle?>(nameof(AreaStrokeDashStyle));

		public static readonly StyledProperty<PenLineCap> AreaStrokeLineCapProperty =
			AvaloniaProperty.Register<LineChart, PenLineCap>(nameof(AreaStrokeLineCap), PenLineCap.Round);

		public static readonly StyledProperty<PenLineJoin> AreaStrokeLineJoinProperty =
			AvaloniaProperty.Register<LineChart, PenLineJoin>(nameof(AreaStrokeLineJoin), PenLineJoin.Miter);

		public static readonly StyledProperty<double> AreaStrokeMiterLimitProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(AreaStrokeMiterLimit), 10.0);

		public static readonly StyledProperty<double> AreaMinViableHeightProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(AreaMinViableHeight), double.MinValue);

		public static readonly StyledProperty<double> AreaMinViableWidthProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(AreaMinViableWidth), double.MinValue);

		public static readonly StyledProperty<bool> SmoothCurveProperty =
			AvaloniaProperty.Register<LineChart, bool>(nameof(SmoothCurve));

		// XAxis

		public static readonly StyledProperty<IList<double>?> XAxisValuesProperty =
			AvaloniaProperty.Register<LineChart, IList<double>?>(nameof(XAxisValues));

		public static readonly StyledProperty<double?> XAxisMinimumProperty =
			AvaloniaProperty.Register<LineChart, double?>(nameof(XAxisMinimum));

		public static readonly StyledProperty<bool> XAxisLogarithmicScaleProperty =
			AvaloniaProperty.Register<LineChart, bool>(nameof(XAxisLogarithmicScale));

		public static readonly StyledProperty<double> XAxisCurrentValueProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisCurrentValue), double.NaN);

		public static readonly StyledProperty<double> XAxisOpacityProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisOpacity), 1.0);

		public static readonly StyledProperty<Point> XAxisOffsetProperty =
			AvaloniaProperty.Register<LineChart, Point>(nameof(XAxisOffset));

		public static readonly StyledProperty<IBrush?> XAxisStrokeProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(XAxisStroke));

		public static readonly StyledProperty<double> XAxisStrokeThicknessProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisStrokeThickness), 1.0);

		public static readonly StyledProperty<double> XAxisArrowSizeProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisArrowSize), 3.5);

		public static readonly StyledProperty<double> XAxisMinViableHeightProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisMinViableHeight), double.MinValue);

		public static readonly StyledProperty<double> XAxisMinViableWidthProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisMinViableWidth), double.MinValue);

		public static readonly StyledProperty<AxisPlotMode> XAxisPlotModeProperty =
			AvaloniaProperty.Register<LineChart, AxisPlotMode>(nameof(XAxisPlotMode), AxisPlotMode.Normal);

		// XAxis Label

		public static readonly StyledProperty<IList<string>?> XAxisLabelsProperty =
			AvaloniaProperty.Register<LineChart, IList<string>?>(nameof(XAxisLabels));

		public static readonly StyledProperty<IBrush?> XAxisLabelForegroundProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(XAxisLabelForeground));

		public static readonly StyledProperty<double> XAxisLabelOpacityProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisLabelOpacity), 1.0);

		public static readonly StyledProperty<Point> XAxisLabelOffsetProperty =
			AvaloniaProperty.Register<LineChart, Point>(nameof(XAxisLabelOffset), new Point(0, 5));

		public static readonly StyledProperty<TextAlignment> XAxisLabelAlignmentProperty =
			AvaloniaProperty.Register<LineChart, TextAlignment>(nameof(XAxisLabelAlignment), TextAlignment.Center);

		public static readonly StyledProperty<double> XAxisLabelAngleProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisLabelAngle));

		public static readonly StyledProperty<FontFamily> XAxisLabelFontFamilyProperty =
			AvaloniaProperty.Register<LineChart, FontFamily>(nameof(XAxisLabelFontFamily), FontFamily.Default);

		public static readonly StyledProperty<FontStyle> XAxisLabelFontStyleProperty =
			AvaloniaProperty.Register<LineChart, FontStyle>(nameof(XAxisLabelFontStyle));

		public static readonly StyledProperty<FontWeight> XAxisLabelFontWeightProperty =
			AvaloniaProperty.Register<LineChart, FontWeight>(nameof(XAxisLabelFontWeight), FontWeight.Normal);

		public static readonly StyledProperty<double> XAxisLabelFontSizeProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisLabelFontSize), 10.0);

		// XAxis Title

		public static readonly StyledProperty<string> XAxisTitleProperty =
			AvaloniaProperty.Register<LineChart, string>(nameof(XAxisTitle));

		public static readonly StyledProperty<IBrush?> XAxisTitleForegroundProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(XAxisTitleForeground));

		public static readonly StyledProperty<double> XAxisTitleOpacityProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisTitleOpacity), 1.0);

		public static readonly StyledProperty<Point> XAxisTitleOffsetProperty =
			AvaloniaProperty.Register<LineChart, Point>(nameof(XAxisTitleOffset));

		public static readonly StyledProperty<Size> XAxisTitleSizeProperty =
			AvaloniaProperty.Register<LineChart, Size>(nameof(XAxisTitleSize));

		public static readonly StyledProperty<TextAlignment> XAxisTitleAlignmentProperty =
			AvaloniaProperty.Register<LineChart, TextAlignment>(nameof(XAxisTitleAlignment), TextAlignment.Center);

		public static readonly StyledProperty<double> XAxisTitleAngleProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisTitleAngle));

		public static readonly StyledProperty<FontFamily> XAxisTitleFontFamilyProperty =
			AvaloniaProperty.Register<LineChart, FontFamily>(nameof(XAxisTitleFontFamily), FontFamily.Default);

		public static readonly StyledProperty<FontStyle> XAxisTitleFontStyleProperty =
			AvaloniaProperty.Register<LineChart, FontStyle>(nameof(XAxisTitleFontStyle));

		public static readonly StyledProperty<FontWeight> XAxisTitleFontWeightProperty =
			AvaloniaProperty.Register<LineChart, FontWeight>(nameof(XAxisTitleFontWeight), FontWeight.Normal);

		public static readonly StyledProperty<double> XAxisTitleFontSizeProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(XAxisTitleFontSize), 12.0);

		// YAxis

		public static readonly StyledProperty<IList<double>?> YAxisValuesProperty =
			AvaloniaProperty.Register<LineChart, IList<double>?>(nameof(YAxisValues));

		public static readonly StyledProperty<bool> YAxisLogarithmicScaleProperty =
			AvaloniaProperty.Register<LineChart, bool>(nameof(YAxisLogarithmicScale));

		public static readonly StyledProperty<double> YAxisMinValueProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisMinValue), double.NaN);

		public static readonly StyledProperty<double> YAxisMaxValueProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisMaxValue), double.NaN);

		public static readonly StyledProperty<double> YAxisCurrentValueProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisCurrentValue), double.NaN);

		public static readonly StyledProperty<double> YAxisOpacityProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisOpacity), 1.0);

		public static readonly StyledProperty<Point> YAxisOffsetProperty =
			AvaloniaProperty.Register<LineChart, Point>(nameof(YAxisOffset));

		public static readonly StyledProperty<IBrush?> YAxisStrokeProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(YAxisStroke));

		public static readonly StyledProperty<double> YAxisStrokeThicknessProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisStrokeThickness), 1.0);

		public static readonly StyledProperty<double> YAxisArrowSizeProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisArrowSize), 3.5);

		public static readonly StyledProperty<double> YAxisMinViableHeightProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisMinViableHeight), double.MinValue);

		public static readonly StyledProperty<double> YAxisMinViableWidthProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisMinViableWidth), double.MinValue);

		// YAxis Label

		public static readonly StyledProperty<IList<string>?> YAxisLabelsProperty =
			AvaloniaProperty.Register<LineChart, IList<string>?>(nameof(YAxisLabels));

		public static readonly StyledProperty<IBrush?> YAxisLabelForegroundProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(YAxisLabelForeground));

		public static readonly StyledProperty<double> YAxisLabelOpacityProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisLabelOpacity), 1.0);

		public static readonly StyledProperty<Point> YAxisLabelOffsetProperty =
			AvaloniaProperty.Register<LineChart, Point>(nameof(YAxisLabelOffset), new Point(-5, 0));

		public static readonly StyledProperty<TextAlignment> YAxisLabelAlignmentProperty =
			AvaloniaProperty.Register<LineChart, TextAlignment>(nameof(YAxisLabelAlignment), TextAlignment.Right);

		public static readonly StyledProperty<double> YAxisLabelAngleProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisLabelAngle));

		public static readonly StyledProperty<FontFamily> YAxisLabelFontFamilyProperty =
			AvaloniaProperty.Register<LineChart, FontFamily>(nameof(YAxisLabelFontFamily), FontFamily.Default);

		public static readonly StyledProperty<FontStyle> YAxisLabelFontStyleProperty =
			AvaloniaProperty.Register<LineChart, FontStyle>(nameof(YAxisLabelFontStyle));

		public static readonly StyledProperty<FontWeight> YAxisLabelFontWeightProperty =
			AvaloniaProperty.Register<LineChart, FontWeight>(nameof(YAxisLabelFontWeight), FontWeight.Normal);

		public static readonly StyledProperty<double> YAxisLabelFontSizeProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisLabelFontSize), 10.0);

		// YAxis Title

		public static readonly StyledProperty<string> YAxisTitleProperty =
			AvaloniaProperty.Register<LineChart, string>(nameof(YAxisTitle));

		public static readonly StyledProperty<IBrush?> YAxisTitleForegroundProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(YAxisTitleForeground));

		public static readonly StyledProperty<double> YAxisTitleOpacityProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisTitleOpacity), 1.0);

		public static readonly StyledProperty<Point> YAxisTitleOffsetProperty =
			AvaloniaProperty.Register<LineChart, Point>(nameof(YAxisTitleOffset));

		public static readonly StyledProperty<Size> YAxisTitleSizeProperty =
			AvaloniaProperty.Register<LineChart, Size>(nameof(YAxisTitleSize));

		public static readonly StyledProperty<TextAlignment> YAxisTitleAlignmentProperty =
			AvaloniaProperty.Register<LineChart, TextAlignment>(nameof(YAxisTitleAlignment), TextAlignment.Center);

		public static readonly StyledProperty<double> YAxisTitleAngleProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisTitleAngle));

		public static readonly StyledProperty<FontFamily> YAxisTitleFontFamilyProperty =
			AvaloniaProperty.Register<LineChart, FontFamily>(nameof(YAxisTitleFontFamily), FontFamily.Default);

		public static readonly StyledProperty<FontStyle> YAxisTitleFontStyleProperty =
			AvaloniaProperty.Register<LineChart, FontStyle>(nameof(YAxisTitleFontStyle));

		public static readonly StyledProperty<FontWeight> YAxisTitleFontWeightProperty =
			AvaloniaProperty.Register<LineChart, FontWeight>(nameof(YAxisTitleFontWeight), FontWeight.Normal);

		public static readonly StyledProperty<double> YAxisTitleFontSizeProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(YAxisTitleFontSize), 12.0);

		// Cursor

		public static readonly StyledProperty<IBrush?> CursorStrokeProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(CursorStroke));

		public static readonly StyledProperty<double> CursorStrokeThicknessProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(CursorStrokeThickness), 1.0);

		public static readonly StyledProperty<IDashStyle?> CursorStrokeDashStyleProperty =
			AvaloniaProperty.Register<LineChart, IDashStyle?>(nameof(CursorStrokeDashStyle));

		public static readonly StyledProperty<PenLineCap> CursorStrokeLineCapProperty =
			AvaloniaProperty.Register<LineChart, PenLineCap>(nameof(CursorStrokeLineCap), PenLineCap.Round);

		public static readonly StyledProperty<PenLineJoin> CursorStrokeLineJoinProperty =
			AvaloniaProperty.Register<LineChart, PenLineJoin>(nameof(CursorStrokeLineJoin), PenLineJoin.Miter);

		public static readonly StyledProperty<double> CursorStrokeMiterLimitProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(CursorStrokeMiterLimit), 10.0);

		// Border

		public static readonly StyledProperty<IBrush?> BorderBrushProperty =
			AvaloniaProperty.Register<LineChart, IBrush?>(nameof(BorderBrush));

		public static readonly StyledProperty<double> BorderThicknessProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(BorderThickness), 1.0);

		public static readonly StyledProperty<double> BorderRadiusXProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(BorderRadiusX));

		public static readonly StyledProperty<double> BorderRadiusYProperty =
			AvaloniaProperty.Register<LineChart, double>(nameof(BorderRadiusY));

		// Fields

		private bool _captured;

		// ctor

		static LineChart()
		{
			AffectsMeasure<LineChart>(AreaMarginProperty);

			AffectsRender<LineChart>(
				AreaMarginProperty,
				AreaFillProperty,
				AreaStrokeProperty,
				AreaStrokeThicknessProperty,
				AreaStrokeDashStyleProperty,
				AreaStrokeLineCapProperty,
				AreaStrokeLineJoinProperty,
				AreaStrokeMiterLimitProperty);

			AffectsRender<LineChart>(
				XAxisValuesProperty,
				XAxisLogarithmicScaleProperty,
				XAxisCurrentValueProperty,
				XAxisOpacityProperty,
				XAxisOffsetProperty,
				XAxisStrokeProperty,
				XAxisStrokeThicknessProperty,
				XAxisArrowSizeProperty,
				XAxisLabelsProperty,
				XAxisLabelForegroundProperty,
				XAxisLabelOpacityProperty,
				XAxisLabelOffsetProperty,
				XAxisLabelAlignmentProperty,
				XAxisLabelAngleProperty,
				XAxisLabelFontFamilyProperty,
				XAxisLabelFontStyleProperty,
				XAxisLabelFontWeightProperty,
				XAxisLabelFontSizeProperty,
				XAxisTitleProperty,
				XAxisTitleForegroundProperty,
				XAxisTitleOpacityProperty,
				XAxisTitleOffsetProperty,
				XAxisTitleSizeProperty,
				XAxisTitleAlignmentProperty,
				XAxisTitleAngleProperty,
				XAxisTitleFontFamilyProperty,
				XAxisTitleFontStyleProperty,
				XAxisTitleFontWeightProperty,
				XAxisTitleFontSizeProperty);

			AffectsRender<LineChart>(
				YAxisValuesProperty,
				YAxisLogarithmicScaleProperty,
				YAxisMinValueProperty,
				YAxisMaxValueProperty,
				YAxisCurrentValueProperty,
				YAxisOpacityProperty,
				YAxisOffsetProperty,
				YAxisStrokeProperty,
				YAxisStrokeThicknessProperty,
				YAxisArrowSizeProperty,
				YAxisLabelsProperty,
				YAxisLabelForegroundProperty,
				YAxisLabelOpacityProperty,
				YAxisLabelOffsetProperty,
				YAxisLabelAlignmentProperty,
				YAxisLabelAngleProperty,
				YAxisLabelFontFamilyProperty,
				YAxisLabelFontStyleProperty,
				YAxisLabelFontWeightProperty,
				YAxisLabelFontSizeProperty,
				YAxisTitleProperty,
				YAxisTitleForegroundProperty,
				YAxisTitleOpacityProperty,
				YAxisTitleOffsetProperty,
				YAxisTitleSizeProperty,
				YAxisTitleAlignmentProperty,
				YAxisTitleAngleProperty,
				YAxisTitleFontFamilyProperty,
				YAxisTitleFontStyleProperty,
				YAxisTitleFontWeightProperty,
				YAxisTitleFontSizeProperty);

			AffectsRender<LineChart>(
				CursorStrokeProperty,
				CursorStrokeThicknessProperty,
				CursorStrokeDashStyleProperty,
				CursorStrokeLineCapProperty,
				CursorStrokeLineJoinProperty,
				CursorStrokeMiterLimitProperty);

			AffectsRender<LineChart>(
				BorderBrushProperty,
				BorderThicknessProperty,
				BorderRadiusXProperty,
				BorderRadiusYProperty);
		}

		// Area

		public Thickness AreaMargin
		{
			get => GetValue(AreaMarginProperty);
			set => SetValue(AreaMarginProperty, value);
		}

		public IBrush? AreaFill
		{
			get => GetValue(AreaFillProperty);
			set => SetValue(AreaFillProperty, value);
		}

		public IBrush? AreaStroke
		{
			get => GetValue(AreaStrokeProperty);
			set => SetValue(AreaStrokeProperty, value);
		}

		public double AreaStrokeThickness
		{
			get => GetValue(AreaStrokeThicknessProperty);
			set => SetValue(AreaStrokeThicknessProperty, value);
		}

		public IDashStyle? AreaStrokeDashStyle
		{
			get => GetValue(AreaStrokeDashStyleProperty);
			set => SetValue(AreaStrokeDashStyleProperty, value);
		}

		public PenLineCap AreaStrokeLineCap
		{
			get => GetValue(AreaStrokeLineCapProperty);
			set => SetValue(AreaStrokeLineCapProperty, value);
		}

		public PenLineJoin AreaStrokeLineJoin
		{
			get => GetValue(AreaStrokeLineJoinProperty);
			set => SetValue(AreaStrokeLineJoinProperty, value);
		}

		public double AreaStrokeMiterLimit
		{
			get => GetValue(AreaStrokeMiterLimitProperty);
			set => SetValue(AreaStrokeMiterLimitProperty, value);
		}

		public double AreaMinViableHeight
		{
			get => GetValue(AreaMinViableHeightProperty);
			set => SetValue(AreaMinViableHeightProperty, value);
		}

		public double AreaMinViableWidth
		{
			get => GetValue(AreaMinViableWidthProperty);
			set => SetValue(AreaMinViableWidthProperty, value);
		}

		public bool SmoothCurve
		{
			get => GetValue(SmoothCurveProperty);
			set => SetValue(SmoothCurveProperty, value);
		}

		// XAxis

		public IList<double>? XAxisValues
		{
			get => GetValue(XAxisValuesProperty);
			set => SetValue(XAxisValuesProperty, value);
		}

		public double? XAxisMinimum
		{
			get => GetValue(XAxisMinimumProperty);
			set => SetValue(XAxisMinimumProperty, value);
		}

		public bool XAxisLogarithmicScale
		{
			get => GetValue(XAxisLogarithmicScaleProperty);
			set => SetValue(XAxisLogarithmicScaleProperty, value);
		}

		public double XAxisCurrentValue
		{
			get => GetValue(XAxisCurrentValueProperty);
			set => SetValue(XAxisCurrentValueProperty, value);
		}

		public double XAxisOpacity
		{
			get => GetValue(XAxisOpacityProperty);
			set => SetValue(XAxisOpacityProperty, value);
		}

		public Point XAxisOffset
		{
			get => GetValue(XAxisOffsetProperty);
			set => SetValue(XAxisOffsetProperty, value);
		}

		public IBrush? XAxisStroke
		{
			get => GetValue(XAxisStrokeProperty);
			set => SetValue(XAxisStrokeProperty, value);
		}

		public double XAxisStrokeThickness
		{
			get => GetValue(XAxisStrokeThicknessProperty);
			set => SetValue(XAxisStrokeThicknessProperty, value);
		}

		public double XAxisArrowSize
		{
			get => GetValue(XAxisArrowSizeProperty);
			set => SetValue(XAxisArrowSizeProperty, value);
		}

		public double XAxisMinViableHeight
		{
			get => GetValue(XAxisMinViableHeightProperty);
			set => SetValue(XAxisMinViableHeightProperty, value);
		}

		public double XAxisMinViableWidth
		{
			get => GetValue(XAxisMinViableWidthProperty);
			set => SetValue(XAxisMinViableWidthProperty, value);
		}

		public AxisPlotMode XAxisPlotMode
		{
			get => GetValue(XAxisPlotModeProperty);
			set => SetValue(XAxisPlotModeProperty, value);
		}

		// XAxis Label

		public IList<string>? XAxisLabels
		{
			get => GetValue(XAxisLabelsProperty);
			set => SetValue(XAxisLabelsProperty, value);
		}

		public IBrush? XAxisLabelForeground
		{
			get => GetValue(XAxisLabelForegroundProperty);
			set => SetValue(XAxisLabelForegroundProperty, value);
		}

		public double XAxisLabelOpacity
		{
			get => GetValue(XAxisLabelOpacityProperty);
			set => SetValue(XAxisLabelOpacityProperty, value);
		}

		public double XAxisLabelAngle
		{
			get => GetValue(XAxisLabelAngleProperty);
			set => SetValue(XAxisLabelAngleProperty, value);
		}

		public Point XAxisLabelOffset
		{
			get => GetValue(XAxisLabelOffsetProperty);
			set => SetValue(XAxisLabelOffsetProperty, value);
		}

		public TextAlignment XAxisLabelAlignment
		{
			get => GetValue(XAxisLabelAlignmentProperty);
			set => SetValue(XAxisLabelAlignmentProperty, value);
		}

		public FontFamily XAxisLabelFontFamily
		{
			get => GetValue(XAxisLabelFontFamilyProperty);
			set => SetValue(XAxisLabelFontFamilyProperty, value);
		}

		public FontStyle XAxisLabelFontStyle
		{
			get => GetValue(XAxisLabelFontStyleProperty);
			set => SetValue(XAxisLabelFontStyleProperty, value);
		}

		public FontWeight XAxisLabelFontWeight
		{
			get => GetValue(XAxisLabelFontWeightProperty);
			set => SetValue(XAxisLabelFontWeightProperty, value);
		}

		public double XAxisLabelFontSize
		{
			get => GetValue(XAxisLabelFontSizeProperty);
			set => SetValue(XAxisLabelFontSizeProperty, value);
		}

		// XAxis Title

		public string XAxisTitle
		{
			get => GetValue(XAxisTitleProperty);
			set => SetValue(XAxisTitleProperty, value);
		}

		public IBrush? XAxisTitleForeground
		{
			get => GetValue(XAxisTitleForegroundProperty);
			set => SetValue(XAxisTitleForegroundProperty, value);
		}

		public double XAxisTitleOpacity
		{
			get => GetValue(XAxisTitleOpacityProperty);
			set => SetValue(XAxisTitleOpacityProperty, value);
		}

		public double XAxisTitleAngle
		{
			get => GetValue(XAxisTitleAngleProperty);
			set => SetValue(XAxisTitleAngleProperty, value);
		}

		public Point XAxisTitleOffset
		{
			get => GetValue(XAxisTitleOffsetProperty);
			set => SetValue(XAxisTitleOffsetProperty, value);
		}

		public Size XAxisTitleSize
		{
			get => GetValue(XAxisTitleSizeProperty);
			set => SetValue(XAxisTitleSizeProperty, value);
		}

		public TextAlignment XAxisTitleAlignment
		{
			get => GetValue(XAxisTitleAlignmentProperty);
			set => SetValue(XAxisTitleAlignmentProperty, value);
		}

		public FontFamily XAxisTitleFontFamily
		{
			get => GetValue(XAxisTitleFontFamilyProperty);
			set => SetValue(XAxisTitleFontFamilyProperty, value);
		}

		public FontStyle XAxisTitleFontStyle
		{
			get => GetValue(XAxisTitleFontStyleProperty);
			set => SetValue(XAxisTitleFontStyleProperty, value);
		}

		public FontWeight XAxisTitleFontWeight
		{
			get => GetValue(XAxisTitleFontWeightProperty);
			set => SetValue(XAxisTitleFontWeightProperty, value);
		}

		public double XAxisTitleFontSize
		{
			get => GetValue(XAxisTitleFontSizeProperty);
			set => SetValue(XAxisTitleFontSizeProperty, value);
		}

		// YAxis

		public IList<double>? YAxisValues
		{
			get => GetValue(YAxisValuesProperty);
			set => SetValue(YAxisValuesProperty, value);
		}

		public bool YAxisLogarithmicScale
		{
			get => GetValue(YAxisLogarithmicScaleProperty);
			set => SetValue(YAxisLogarithmicScaleProperty, value);
		}

		public double YAxisMinValue
		{
			get => GetValue(YAxisMinValueProperty);
			set => SetValue(YAxisMinValueProperty, value);
		}

		public double YAxisMaxValue
		{
			get => GetValue(YAxisMaxValueProperty);
			set => SetValue(YAxisMaxValueProperty, value);
		}

		public double YAxisCurrentValue
		{
			get => GetValue(YAxisCurrentValueProperty);
			set => SetValue(YAxisCurrentValueProperty, value);
		}

		public double YAxisOpacity
		{
			get => GetValue(YAxisOpacityProperty);
			set => SetValue(YAxisOpacityProperty, value);
		}

		public Point YAxisOffset
		{
			get => GetValue(YAxisOffsetProperty);
			set => SetValue(YAxisOffsetProperty, value);
		}

		public IBrush? YAxisStroke
		{
			get => GetValue(YAxisStrokeProperty);
			set => SetValue(YAxisStrokeProperty, value);
		}

		public double YAxisStrokeThickness
		{
			get => GetValue(YAxisStrokeThicknessProperty);
			set => SetValue(YAxisStrokeThicknessProperty, value);
		}

		public double YAxisArrowSize
		{
			get => GetValue(YAxisArrowSizeProperty);
			set => SetValue(YAxisArrowSizeProperty, value);
		}

		public double YAxisMinViableHeight
		{
			get => GetValue(YAxisMinViableHeightProperty);
			set => SetValue(YAxisMinViableHeightProperty, value);
		}

		public double YAxisMinViableWidth
		{
			get => GetValue(YAxisMinViableWidthProperty);
			set => SetValue(YAxisMinViableWidthProperty, value);
		}

		// YAxis Label

		public IList<string>? YAxisLabels
		{
			get => GetValue(YAxisLabelsProperty);
			set => SetValue(YAxisLabelsProperty, value);
		}

		public IBrush? YAxisLabelForeground
		{
			get => GetValue(YAxisLabelForegroundProperty);
			set => SetValue(YAxisLabelForegroundProperty, value);
		}

		public double YAxisLabelOpacity
		{
			get => GetValue(YAxisLabelOpacityProperty);
			set => SetValue(YAxisLabelOpacityProperty, value);
		}

		public double YAxisLabelAngle
		{
			get => GetValue(YAxisLabelAngleProperty);
			set => SetValue(YAxisLabelAngleProperty, value);
		}

		public Point YAxisLabelOffset
		{
			get => GetValue(YAxisLabelOffsetProperty);
			set => SetValue(YAxisLabelOffsetProperty, value);
		}

		public TextAlignment YAxisLabelAlignment
		{
			get => GetValue(YAxisLabelAlignmentProperty);
			set => SetValue(YAxisLabelAlignmentProperty, value);
		}

		public FontFamily YAxisLabelFontFamily
		{
			get => GetValue(YAxisLabelFontFamilyProperty);
			set => SetValue(YAxisLabelFontFamilyProperty, value);
		}

		public FontStyle YAxisLabelFontStyle
		{
			get => GetValue(YAxisLabelFontStyleProperty);
			set => SetValue(YAxisLabelFontStyleProperty, value);
		}

		public FontWeight YAxisLabelFontWeight
		{
			get => GetValue(YAxisLabelFontWeightProperty);
			set => SetValue(YAxisLabelFontWeightProperty, value);
		}

		public double YAxisLabelFontSize
		{
			get => GetValue(YAxisLabelFontSizeProperty);
			set => SetValue(YAxisLabelFontSizeProperty, value);
		}

		// YAxis Title

		public string YAxisTitle
		{
			get => GetValue(YAxisTitleProperty);
			set => SetValue(YAxisTitleProperty, value);
		}

		public IBrush? YAxisTitleForeground
		{
			get => GetValue(YAxisTitleForegroundProperty);
			set => SetValue(YAxisTitleForegroundProperty, value);
		}

		public double YAxisTitleOpacity
		{
			get => GetValue(YAxisTitleOpacityProperty);
			set => SetValue(YAxisTitleOpacityProperty, value);
		}

		public double YAxisTitleAngle
		{
			get => GetValue(YAxisTitleAngleProperty);
			set => SetValue(YAxisTitleAngleProperty, value);
		}

		public Point YAxisTitleOffset
		{
			get => GetValue(YAxisTitleOffsetProperty);
			set => SetValue(YAxisTitleOffsetProperty, value);
		}

		public Size YAxisTitleSize
		{
			get => GetValue(YAxisTitleSizeProperty);
			set => SetValue(YAxisTitleSizeProperty, value);
		}

		public TextAlignment YAxisTitleAlignment
		{
			get => GetValue(YAxisTitleAlignmentProperty);
			set => SetValue(YAxisTitleAlignmentProperty, value);
		}

		public FontFamily YAxisTitleFontFamily
		{
			get => GetValue(YAxisTitleFontFamilyProperty);
			set => SetValue(YAxisTitleFontFamilyProperty, value);
		}

		public FontStyle YAxisTitleFontStyle
		{
			get => GetValue(YAxisTitleFontStyleProperty);
			set => SetValue(YAxisTitleFontStyleProperty, value);
		}

		public FontWeight YAxisTitleFontWeight
		{
			get => GetValue(YAxisTitleFontWeightProperty);
			set => SetValue(YAxisTitleFontWeightProperty, value);
		}

		public double YAxisTitleFontSize
		{
			get => GetValue(YAxisTitleFontSizeProperty);
			set => SetValue(YAxisTitleFontSizeProperty, value);
		}

		// Cursor

		public IBrush? CursorStroke
		{
			get => GetValue(CursorStrokeProperty);
			set => SetValue(CursorStrokeProperty, value);
		}

		public double CursorStrokeThickness
		{
			get => GetValue(CursorStrokeThicknessProperty);
			set => SetValue(CursorStrokeThicknessProperty, value);
		}

		public IDashStyle? CursorStrokeDashStyle
		{
			get => GetValue(CursorStrokeDashStyleProperty);
			set => SetValue(CursorStrokeDashStyleProperty, value);
		}

		public PenLineCap CursorStrokeLineCap
		{
			get => GetValue(CursorStrokeLineCapProperty);
			set => SetValue(CursorStrokeLineCapProperty, value);
		}

		public PenLineJoin CursorStrokeLineJoin
		{
			get => GetValue(CursorStrokeLineJoinProperty);
			set => SetValue(CursorStrokeLineJoinProperty, value);
		}

		public double CursorStrokeMiterLimit
		{
			get => GetValue(CursorStrokeMiterLimitProperty);
			set => SetValue(CursorStrokeMiterLimitProperty, value);
		}

		// Border

		public IBrush? BorderBrush
		{
			get => GetValue(BorderBrushProperty);
			set => SetValue(BorderBrushProperty, value);
		}

		public double BorderThickness
		{
			get => GetValue(BorderThicknessProperty);
			set => SetValue(BorderThicknessProperty, value);
		}

		public double BorderRadiusX
		{
			get => GetValue(BorderRadiusXProperty);
			set => SetValue(BorderRadiusXProperty, value);
		}

		public double BorderRadiusY
		{
			get => GetValue(BorderRadiusYProperty);
			set => SetValue(BorderRadiusYProperty, value);
		}
	}
}
