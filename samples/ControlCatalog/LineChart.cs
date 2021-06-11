using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using MathNet;

namespace ControlCatalog
{
	public partial class LineChart : Control
	{
		private readonly Dictionary<INotifyCollectionChanged, IDisposable> _collectionChangedSubscriptions;

		public LineChart()
		{
			_collectionChangedSubscriptions = new Dictionary<INotifyCollectionChanged, IDisposable>();

			AddHandler(PointerPressedEvent, PointerPressedHandler, RoutingStrategies.Tunnel);
			AddHandler(PointerReleasedEvent, PointerReleasedHandler, RoutingStrategies.Tunnel);
			AddHandler(PointerMovedEvent, PointerMovedHandler, RoutingStrategies.Tunnel);
			AddHandler(PointerLeaveEvent, PointerLeaveHandler, RoutingStrategies.Tunnel);
		}

		private static double Clamp(double val, double min, double max)
		{
			return Math.Min(Math.Max(val, min), max);
		}

		private static Geometry CreateFillGeometry(IReadOnlyList<Point> points, double width, double height)
		{
			var geometry = new StreamGeometry();
			using var context = geometry.Open();
			context.BeginFigure(points[0], true);
			for (var i = 1; i < points.Count; i++)
			{
				context.LineTo(points[i]);
			}

			context.LineTo(new Point(width, height));
			context.LineTo(new Point(0, height));
			context.EndFigure(true);
			return geometry;
		}

		private static Geometry CreateStrokeGeometry(IReadOnlyList<Point> points)
		{
			var geometry = new StreamGeometry();
			using var context = geometry.Open();
			context.BeginFigure(points[0], false);
			for (var i = 1; i < points.Count; i++)
			{
				context.LineTo(points[i]);
			}

			context.EndFigure(false);
			return geometry;
		}

		private static FormattedText CreateFormattedText(string text, Typeface typeface, TextAlignment alignment,
			double fontSize, Size constraint)
		{
			return new FormattedText()
			{
				Typeface = typeface,
				Text = text,
				TextAlignment = alignment,
				TextWrapping = TextWrapping.NoWrap,
				FontSize = fontSize,
				Constraint = constraint
			};
		}

		private void UpdateXAxisCursorPosition(double x)
		{
			var xAxisValues = XAxisValues;
			if (xAxisValues is null || xAxisValues.Count == 0)
			{
				XAxisCurrentValue = double.NaN;
				return;
			}

			var areaWidth = Bounds.Width - AreaMargin.Left - AreaMargin.Right;
			var value = Clamp(x - AreaMargin.Left, 0, areaWidth);
			var factor = value / areaWidth;
			var index = (int)((xAxisValues.Count - 1) * factor);
			var currentValue = xAxisValues[index];
			XAxisCurrentValue = currentValue;
		}

		private Rect? GetXAxisCursorHitTestRect()
		{
			var chartWidth = Bounds.Width;
			var chartHeight = Bounds.Height;
			var areaMargin = AreaMargin;
			var areaWidth = chartWidth - areaMargin.Left - areaMargin.Right;
			var areaHeight = chartHeight - areaMargin.Top - areaMargin.Bottom;
			var areaRect = new Rect(areaMargin.Left, areaMargin.Top, areaWidth, areaHeight);
			var cursorPosition = GetCursorPosition(areaWidth);
			if (double.IsNaN(cursorPosition))
			{
				return null;
			}

			var cursorHitTestSize = 5;
			var cursorStrokeThickness = CursorStrokeThickness;
			var cursorHitTestRect = new Rect(
				areaMargin.Left + cursorPosition - cursorHitTestSize + cursorStrokeThickness / 2,
				areaRect.Top,
				cursorHitTestSize + cursorHitTestSize,
				areaRect.Height);
			return cursorHitTestRect;
		}

		private void PointerLeaveHandler(object? sender, PointerEventArgs e)
		{
			Cursor = new Cursor(StandardCursorType.Arrow);
		}

		private void PointerMovedHandler(object? sender, PointerEventArgs e)
		{
			var position = e.GetPosition(this);
			if (_captured)
			{
				UpdateXAxisCursorPosition(position.X);
			}
			else
			{
				if (CursorStroke is null)
				{
					return;
				}

				var cursorHitTestRect = GetXAxisCursorHitTestRect();
				var cursorSizeWestEast = cursorHitTestRect is not null && cursorHitTestRect.Value.Contains(position);
				Cursor = cursorSizeWestEast
					? new Cursor(StandardCursorType.SizeWestEast)
					: new Cursor(StandardCursorType.Arrow);
			}
		}

		private void PointerReleasedHandler(object? sender, PointerReleasedEventArgs e)
		{
			if (!_captured)
			{
				return;
			}

			var position = e.GetPosition(this);
			var cursorHitTestRect = GetXAxisCursorHitTestRect();
			var cursorSizeWestEast = cursorHitTestRect is not null && cursorHitTestRect.Value.Contains(position);
			if (!cursorSizeWestEast)
			{
				Cursor = new Cursor(StandardCursorType.Arrow);
			}

			_captured = false;
		}

		private void PointerPressedHandler(object? sender, PointerPressedEventArgs e)
		{
			var position = e.GetPosition(this);
			UpdateXAxisCursorPosition(position.X);
			Cursor = new Cursor(StandardCursorType.SizeWestEast);
			_captured = true;
		}

		private LineChartState CreateChartState(double width, double height)
		{
			var state = new LineChartState
			{
				ChartWidth = width,
				ChartHeight = height,
				AreaMargin = AreaMargin
			};

			state.AreaWidth = width - state.AreaMargin.Left - state.AreaMargin.Right;
			state.AreaHeight = height - state.AreaMargin.Top - state.AreaMargin.Bottom;

			SetStateAreaPoints(state);

			SetStateXAxisLabels(state);
			SetStateYAxisLabels(state);

			SetStateXAxisCursor(state);

			return state;
		}

		private static IEnumerable<(double x, double y)> SplineInterpolate(double[] xs, double[] ys)
		{
			const int Divisions = 256;

			if (xs.Length > 2)
			{
				var spline = CubicSpline.InterpolatePchipSorted(xs, ys);

				for (var i = 0; i < xs.Length - 1; i++)
				{
					var a = xs[i];
					var b = xs[i + 1];
					var range = b - a;
					var step = range / Divisions;

					var t0 = xs[i];

					var xt0 = spline.Interpolate(xs[i]);

					yield return (t0, xt0);

					for (var t = a + step; t < b; t += step)
					{
						var xt = spline.Interpolate(t);

						yield return (t, xt);
					}
				}

				var tn = xs[xs.Length - 1];
				var xtn = spline.Interpolate(xs[xs.Length - 1]);

				yield return (tn, xtn);
			}
			else
			{
				for (var i = 0; i < xs.Length; i++)
				{
					yield return (xs[i], ys[i]);
				}
			}
		}

		private void SetStateAreaPoints(LineChartState state)
		{
			var xAxisValues = XAxisValues;
			var yAxisValues = YAxisValues;

			if (xAxisValues is null || xAxisValues.Count <= 1 || yAxisValues is null || yAxisValues.Count <= 1)
			{
				state.XAxisLabelStep = double.NaN;
				state.YAxisLabelStep = double.NaN;
				state.Points = null;
				return;
			}

			var logarithmicScale = YAxisLogarithmicScale;

			var yAxisValuesLogScaled = logarithmicScale
				? yAxisValues.Select(y => Math.Log(y)).ToList()
				: yAxisValues.ToList();

			var yAxisValuesLogScaledMax = yAxisValuesLogScaled.Max();

			var yAxisScaler = new StraightLineFormula();
			yAxisScaler.CalculateFrom(yAxisValuesLogScaledMax, 0, 0, state.AreaHeight);

			var yAxisValuesScaled = yAxisValuesLogScaled
				.Select(y => yAxisScaler.GetYforX(y))
				.ToList();

			var xAxisValuesEnumerable = xAxisValues as IEnumerable<double>;

			switch (XAxisPlotMode)
			{
				case AxisPlotMode.Normal:
					var min = XAxisMinimum ?? xAxisValues.Min();
					var max = xAxisValues.Max();

					var xAxisScaler = new StraightLineFormula();
					xAxisScaler.CalculateFrom(min, max, 0, state.AreaWidth);

					xAxisValuesEnumerable = xAxisValuesEnumerable.Select(x => xAxisScaler.GetYforX(x));
					break;

				case AxisPlotMode.EvenlySpaced:
					var pointStep = state.AreaWidth / (xAxisValues.Count - 1);

					xAxisValuesEnumerable = Enumerable.Range(0, xAxisValues.Count).Select(x => pointStep * x);
					break;

				case AxisPlotMode.Logarithmic:
					break;
			}

			if (SmoothCurve)
			{
				var interpolated = SplineInterpolate(xAxisValuesEnumerable.ToArray(), yAxisValuesScaled.ToArray());

				state.Points = interpolated.Select(pt => new Point(pt.x, pt.y)).ToArray();
			}
			else
			{
				state.Points = new Point[xAxisValues.Count];

				using (var enumerator = xAxisValuesEnumerable.GetEnumerator())
				{
					for (var i = 0; i < yAxisValuesScaled.Count; i++)
					{
						enumerator.MoveNext();
						state.Points[i] = new Point(enumerator.Current, yAxisValuesScaled[i]);
					}
				}
			}
		}

		private void SetStateXAxisLabels(LineChartState state)
		{
			var xAxisLabels = XAxisLabels;

			if (xAxisLabels is not null)
			{
				state.XAxisLabelStep = xAxisLabels.Count <= 1
					? double.NaN
					: state.AreaWidth / (xAxisLabels.Count - 1);

				state.XAxisLabels = xAxisLabels.ToList();
			}
			else
			{
				AutoGenerateXAxisLabels(state);
			}
		}

		private void SetStateYAxisLabels(LineChartState state)
		{
			var yAxisLabels = YAxisLabels;

			if (yAxisLabels is not null)
			{
				state.YAxisLabelStep = yAxisLabels.Count <= 1
					? double.NaN
					: state.AreaHeight / (yAxisLabels.Count - 1);

				state.YAxisLabels = yAxisLabels.ToList();
			}
			else
			{
				AutoGenerateYAxisLabels(state);
			}
		}

		private void AutoGenerateXAxisLabels(LineChartState state)
		{
			var xAxisValues = XAxisValues;

			state.XAxisLabelStep = xAxisValues is null || xAxisValues.Count <= 1
				? double.NaN
				: state.AreaWidth / (xAxisValues.Count - 1);

			if (XAxisStroke is not null && XAxisValues is not null)
			{
				state.XAxisLabels = XAxisValues.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList();
			}
		}

		private void AutoGenerateYAxisLabels(LineChartState state)
		{
			var yAxisValues = YAxisValues;

			state.YAxisLabelStep = yAxisValues is null || yAxisValues.Count <= 1
				? double.NaN
				: state.AreaHeight / (yAxisValues.Count - 1);

			if (YAxisStroke is not null && YAxisValues is not null)
			{
				state.YAxisLabels = YAxisValues.Select(x => x.ToString(CultureInfo.InvariantCulture)).ToList();
			}
		}

		private double GetCursorPosition(double areaWidth)
		{
			var xAxisCurrentValue = XAxisCurrentValue;
			var xAxisValues = XAxisValues;
			if (double.IsNaN(xAxisCurrentValue) || xAxisValues is null || xAxisValues.Count == 0)
			{
				return double.NaN;
			}

			for (var i = 0; i < xAxisValues.Count; i++)
			{
				if (xAxisValues[i] <= xAxisCurrentValue)
				{
					return areaWidth / xAxisValues.Count * i;
				}
			}

			return double.NaN;
		}

		private void SetStateXAxisCursor(LineChartState state)
		{
			state.XAxisCursorPosition = GetCursorPosition(state.AreaWidth);
		}

		private void DrawAreaFill(DrawingContext context, LineChartState state)
		{
			var brush = AreaFill;
			if (brush is null
				|| state.Points is null
				|| state.AreaWidth <= 0
				|| state.AreaHeight <= 0
				|| state.AreaWidth < AreaMinViableWidth
				|| state.AreaHeight < AreaMinViableHeight)
			{
				return;
			}

			var deflate = 0.5;
			var geometry = CreateFillGeometry(state.Points, state.AreaWidth, state.AreaHeight);
			var transform = context.PushPreTransform(
				Matrix.CreateTranslation(
					state.AreaMargin.Left + deflate,
					state.AreaMargin.Top + deflate));
			context.DrawGeometry(brush, null, geometry);
			transform.Dispose();
		}

		private void DrawAreaStroke(DrawingContext context, LineChartState state)
		{
			var brush = AreaStroke;
			if (brush is null
				|| state.Points is null
				|| state.AreaWidth <= 0
				|| state.AreaHeight <= 0
				|| state.AreaWidth < AreaMinViableWidth
				|| state.AreaHeight < AreaMinViableHeight)
			{
				return;
			}

			var thickness = AreaStrokeThickness;
			var dashStyle = AreaStrokeDashStyle;
			var lineCap = AreaStrokeLineCap;
			var lineJoin = AreaStrokeLineJoin;
			var miterLimit = AreaStrokeMiterLimit;
			var pen = new Pen(brush, thickness, dashStyle, lineCap, lineJoin, miterLimit);
			var deflate = thickness * 0.5;
			var geometry = CreateStrokeGeometry(state.Points);
			var transform = context.PushPreTransform(
				Matrix.CreateTranslation(
					state.AreaMargin.Left + deflate,
					state.AreaMargin.Top + deflate));
			context.DrawGeometry(null, pen, geometry);
			transform.Dispose();
		}

		private void DrawCursor(DrawingContext context, LineChartState state)
		{
			var brush = CursorStroke;
			if (brush is null
				|| double.IsNaN(state.XAxisCursorPosition)
				|| state.AreaWidth <= 0
				|| state.AreaHeight <= 0
				|| state.AreaWidth < AreaMinViableWidth
				|| state.AreaHeight < AreaMinViableHeight)
			{
				return;
			}

			var thickness = CursorStrokeThickness;
			var dashStyle = CursorStrokeDashStyle;
			var lineCap = CursorStrokeLineCap;
			var lineJoin = CursorStrokeLineJoin;
			var miterLimit = CursorStrokeMiterLimit;
			var pen = new Pen(brush, thickness, dashStyle, lineCap, lineJoin, miterLimit);
			var deflate = thickness * 0.5;
			var p1 = new Point(state.XAxisCursorPosition + deflate, 0);
			var p2 = new Point(state.XAxisCursorPosition + deflate, state.AreaHeight);
			var transform = context.PushPreTransform(
				Matrix.CreateTranslation(
					state.AreaMargin.Left,
					state.AreaMargin.Top));
			context.DrawLine(pen, p1, p2);
			transform.Dispose();
		}

		private void DrawXAxis(DrawingContext context, LineChartState state)
		{
			var brush = XAxisStroke;
			if (brush is null
				|| state.AreaWidth <= 0
				|| state.AreaHeight <= 0
				|| state.AreaWidth < XAxisMinViableWidth
				|| state.AreaHeight < XAxisMinViableHeight)
			{
				return;
			}

			var size = XAxisArrowSize;
			var opacity = XAxisOpacity;
			var thickness = XAxisStrokeThickness;
			var pen = new Pen(brush, thickness, null, PenLineCap.Round);
			var deflate = thickness * 0.5;
			var offset = XAxisOffset;
			var p1 = new Point(
				state.AreaMargin.Left + offset.X,
				state.AreaMargin.Top + state.AreaHeight + offset.Y + deflate);
			var p2 = new Point(
				state.AreaMargin.Left + state.AreaWidth,
				state.AreaMargin.Top + state.AreaHeight + offset.Y + deflate);
			var opacityState = context.PushOpacity(opacity);
			context.DrawLine(pen, p1, p2);
			var p3 = new Point(p2.X, p2.Y);
			var p4 = new Point(p2.X - size, p2.Y - size);
			context.DrawLine(pen, p3, p4);
			var p5 = new Point(p2.X, p2.Y);
			var p6 = new Point(p2.X - size, p2.Y + size);
			context.DrawLine(pen, p5, p6);
			opacityState.Dispose();
		}

		private void DrawXAxisLabels(DrawingContext context, LineChartState state)
		{
			var foreground = XAxisLabelForeground;
			if (foreground is null
				|| state.XAxisLabels is null
				|| double.IsNaN(state.XAxisLabelStep)
				|| state.ChartWidth <= 0
				|| state.ChartHeight <= 0
				|| state.ChartHeight - state.AreaMargin.Top < state.AreaMargin.Bottom)
			{
				return;
			}

			var opacity = XAxisLabelOpacity;
			var fontFamily = XAxisLabelFontFamily;
			var fontStyle = XAxisLabelFontStyle;
			var fontWeight = XAxisLabelFontWeight;
			var typeface = new Typeface(fontFamily, fontStyle, fontWeight);
			var fontSize = XAxisLabelFontSize;
			var offset = XAxisLabelOffset;
			var angleRadians = Math.PI / 180.0 * XAxisLabelAngle;
			var alignment = XAxisLabelAlignment;
			var originTop = state.AreaMargin.Top + state.AreaHeight;
			var formattedTextLabels = new List<FormattedText>();
			var constrainWidthMax = 0.0;
			var constrainHeightMax = 0.0;

			foreach (var label in state.XAxisLabels)
			{
				var formattedText = CreateFormattedText(label, typeface, alignment, fontSize, Size.Empty);
				formattedTextLabels.Add(formattedText);
				constrainWidthMax = Math.Max(constrainWidthMax, formattedText.Bounds.Width);
				constrainHeightMax = Math.Max(constrainHeightMax, formattedText.Bounds.Height);
			}

			var constraintMax = new Size(constrainWidthMax, constrainHeightMax);
			var offsetTransform = context.PushPreTransform(Matrix.CreateTranslation(offset.X, offset.Y));

			for (var i = 0; i < formattedTextLabels.Count; i++)
			{
				formattedTextLabels[i].Constraint = constraintMax;

				var origin = new Point(i * state.XAxisLabelStep - constraintMax.Width / 2 + state.AreaMargin.Left,
					originTop);
				var offsetCenter = new Point(constraintMax.Width / 2 - constraintMax.Width / 2, 0);
				var xPosition = origin.X + constraintMax.Width / 2;
				var yPosition = origin.Y + constraintMax.Height / 2;
				var matrix = Matrix.CreateTranslation(-xPosition, -yPosition)
							 * Matrix.CreateRotation(angleRadians)
							 * Matrix.CreateTranslation(xPosition, yPosition);
				var labelTransform = context.PushPreTransform(matrix);
				var opacityState = context.PushOpacity(opacity);
				context.DrawText(foreground, origin + offsetCenter, formattedTextLabels[i]);
				opacityState.Dispose();
				labelTransform.Dispose();
			}

			offsetTransform.Dispose();
		}

		private void DrawXAxisTitle(DrawingContext context, LineChartState state)
		{
			// TODO: Draw XAxis title.
		}

		private void DrawYAxis(DrawingContext context, LineChartState state)
		{
			var brush = YAxisStroke;
			if (brush is null
				|| state.AreaWidth <= 0
				|| state.AreaHeight <= 0
				|| state.AreaWidth < YAxisMinViableWidth
				|| state.AreaHeight < YAxisMinViableHeight)
			{
				return;
			}

			var size = YAxisArrowSize;
			var opacity = YAxisOpacity;
			var thickness = YAxisStrokeThickness;
			var pen = new Pen(brush, thickness, null, PenLineCap.Round);
			var deflate = thickness * 0.5;
			var offset = YAxisOffset;
			var p1 = new Point(
				state.AreaMargin.Left + offset.X + deflate,
				state.AreaMargin.Top);
			var p2 = new Point(
				state.AreaMargin.Left + offset.X + deflate,
				state.AreaMargin.Top + state.AreaHeight + offset.Y);
			var opacityState = context.PushOpacity(opacity);
			context.DrawLine(pen, p1, p2);
			var p3 = new Point(p1.X, p1.Y);
			var p4 = new Point(p1.X - size, p1.Y + size);
			context.DrawLine(pen, p3, p4);
			var p5 = new Point(p1.X, p1.Y);
			var p6 = new Point(p1.X + size, p1.Y + size);
			context.DrawLine(pen, p5, p6);
			opacityState.Dispose();
		}

		private void DrawYAxisLabels(DrawingContext context, LineChartState state)
		{
			var foreground = YAxisLabelForeground;
			if (foreground is null
				|| state.YAxisLabels is null
				|| double.IsNaN(state.YAxisLabelStep)
				|| state.ChartWidth <= 0
				|| state.ChartWidth - state.AreaMargin.Right < state.AreaMargin.Left
				|| state.ChartHeight <= 0)
			{
				return;
			}

			var opacity = YAxisLabelOpacity;
			var fontFamily = YAxisLabelFontFamily;
			var fontStyle = YAxisLabelFontStyle;
			var fontWeight = YAxisLabelFontWeight;
			var typeface = new Typeface(fontFamily, fontStyle, fontWeight);
			var fontSize = YAxisLabelFontSize;
			var offset = YAxisLabelOffset;
			var angleRadians = Math.PI / 180.0 * YAxisLabelAngle;
			var alignment = YAxisLabelAlignment;
			var originLeft = state.AreaMargin.Left;
			var formattedTextLabels = new List<FormattedText>();
			var constrainWidthMax = 0.0;
			var constrainHeightMax = 0.0;

			for (var index = state.YAxisLabels.Count - 1; index >= 0; index--)
			{
				var label = state.YAxisLabels[index];
				var formattedText = CreateFormattedText(label, typeface, alignment, fontSize, Size.Empty);
				formattedTextLabels.Add(formattedText);
				constrainWidthMax = Math.Max(constrainWidthMax, formattedText.Bounds.Width);
				constrainHeightMax = Math.Max(constrainHeightMax, formattedText.Bounds.Height);
			}

			var constraintMax = new Size(constrainWidthMax, constrainHeightMax);
			var offsetTransform = context.PushPreTransform(Matrix.CreateTranslation(offset.X, offset.Y));

			for (var i = 0; i < formattedTextLabels.Count; i++)
			{
				formattedTextLabels[i].Constraint = constraintMax;

				var origin = new Point(originLeft - constraintMax.Width,
					i * state.YAxisLabelStep - constraintMax.Height / 2 + state.AreaMargin.Top);
				var offsetCenter = new Point(constraintMax.Width / 2 - constraintMax.Width / 2, 0);
				var xPosition = origin.X + constraintMax.Width / 2;
				var yPosition = origin.Y + constraintMax.Height / 2;
				var matrix = Matrix.CreateTranslation(-xPosition, -yPosition)
							 * Matrix.CreateRotation(angleRadians)
							 * Matrix.CreateTranslation(xPosition, yPosition);
				var labelTransform = context.PushPreTransform(matrix);
				var opacityState = context.PushOpacity(opacity);
				context.DrawText(foreground, origin + offsetCenter, formattedTextLabels[i]);
				opacityState.Dispose();
				labelTransform.Dispose();
			}

			offsetTransform.Dispose();
		}

		private void DrawYAxisTitle(DrawingContext context, LineChartState state)
		{
			var foreground = YAxisTitleForeground;
			if (foreground is null)
			{
				return;
			}

			if (state.AreaWidth <= 0
				|| state.AreaHeight <= 0
				|| state.AreaWidth < YAxisMinViableWidth
				|| state.AreaHeight < YAxisMinViableHeight)
			{
				return;
			}

			var opacity = YAxisTitleOpacity;
			var fontFamily = YAxisTitleFontFamily;
			var fontStyle = YAxisTitleFontStyle;
			var fontWeight = YAxisTitleFontWeight;
			var typeface = new Typeface(fontFamily, fontStyle, fontWeight);
			var fontSize = YAxisTitleFontSize;
			var offset = YAxisTitleOffset;
			var size = YAxisTitleSize;
			var angleRadians = Math.PI / 180.0 * YAxisTitleAngle;
			var alignment = YAxisTitleAlignment;
			var offsetTransform = context.PushPreTransform(Matrix.CreateTranslation(offset.X, offset.Y));
			var origin = new Point(state.AreaMargin.Left, state.AreaHeight + state.AreaMargin.Top);
			var constraint = new Size(size.Width, size.Height);
			var formattedText = CreateFormattedText(YAxisTitle, typeface, alignment, fontSize, constraint);
			var xPosition = origin.X + size.Width / 2;
			var yPosition = origin.Y + size.Height / 2;
			var matrix = Matrix.CreateTranslation(-xPosition, -yPosition)
						 * Matrix.CreateRotation(angleRadians)
						 * Matrix.CreateTranslation(xPosition, yPosition);
			var labelTransform = context.PushPreTransform(matrix);
			var offsetCenter = new Point(0, size.Height / 2 - formattedText.Bounds.Height / 2);
			var opacityState = context.PushOpacity(opacity);
			context.DrawText(foreground, origin + offsetCenter, formattedText);
			opacityState.Dispose();
			labelTransform.Dispose();
			offsetTransform.Dispose();
		}

		private void DrawBorder(DrawingContext context, LineChartState state)
		{
			var brush = BorderBrush;
			if (brush is null || state.AreaWidth <= 0 || state.AreaHeight <= 0)
			{
				return;
			}

			var thickness = BorderThickness;
			var radiusX = BorderRadiusX;
			var radiusY = BorderRadiusY;
			var pen = new Pen(brush, thickness, null, PenLineCap.Round);
			var rect = new Rect(0, 0, state.ChartWidth, state.ChartHeight);
			var rectDeflate = rect.Deflate(thickness * 0.5);
			context.DrawRectangle(Brushes.Transparent, pen, rectDeflate, radiusX, radiusY);
		}

		private void UpdateSubscription(INotifyCollectionChanged? oldValue, INotifyCollectionChanged? newValue)
		{
			if (oldValue is { } && _collectionChangedSubscriptions.ContainsKey(oldValue))
			{
				_collectionChangedSubscriptions[oldValue].Dispose();
				_collectionChangedSubscriptions.Remove(oldValue);
			}

			if (newValue is { })
			{
				newValue.CollectionChanged += ItemsPropertyCollectionChanged;

				_collectionChangedSubscriptions[newValue] = Disposable.Create(() =>
				{
					newValue.CollectionChanged -= ItemsPropertyCollectionChanged;
				});
			}
		}

		private void ItemsPropertyCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			InvalidateVisual();
		}

		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);

			if (change.Property == XAxisValuesProperty || change.Property == YAxisValuesProperty ||
				change.Property == XAxisLabelsProperty || change.Property == YAxisLabelsProperty)
			{
				UpdateSubscription(
					change.OldValue.GetValueOrDefault<INotifyCollectionChanged>(),
					change.NewValue.GetValueOrDefault<INotifyCollectionChanged>());
			}
		}

		public override void Render(DrawingContext context)
		{
			base.Render(context);

			var state = CreateChartState(Bounds.Width, Bounds.Height);

			DrawAreaFill(context, state);
			DrawAreaStroke(context, state);
			DrawCursor(context, state);

			DrawXAxis(context, state);
			DrawXAxisTitle(context, state);
			DrawXAxisLabels(context, state);

			DrawYAxis(context, state);
			DrawYAxisTitle(context, state);
			DrawYAxisLabels(context, state);

			DrawBorder(context, state);
		}

		private class LineChartState
		{
			public double ChartWidth { get; set; }
			public double ChartHeight { get; set; }
			public double AreaWidth { get; set; }
			public double AreaHeight { get; set; }
			public Thickness AreaMargin { get; set; }
			public Point[]? Points { get; set; }
			public List<string>? XAxisLabels { get; set; }
			public double XAxisLabelStep { get; set; }
			public List<string>? YAxisLabels { get; set; }
			public double YAxisLabelStep { get; set; }
			public double XAxisCursorPosition { get; set; }
		}
	}
}
