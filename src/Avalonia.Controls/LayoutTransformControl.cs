// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.
//
// Idea got from and adapted to work in avalonia
// http://silverlight.codeplex.com/SourceControl/changeset/view/74775#Release/Silverlight4/Source/Controls.Layout.Toolkit/LayoutTransformer/LayoutTransformer.cs
//

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// Control that implements support for transformations as if applied by LayoutTransform.
    /// </summary>
    public class LayoutTransformControl : Decorator
    {
        public static readonly AvaloniaProperty<Transform> LayoutTransformProperty =
            AvaloniaProperty.Register<LayoutTransformControl, Transform>(nameof(LayoutTransform));

        public static readonly AvaloniaProperty<bool> UseRenderTransformProperty =
            AvaloniaProperty.Register<LayoutTransformControl, bool>(nameof(LayoutTransform));

        static LayoutTransformControl()
        {
            ClipToBoundsProperty.OverrideDefaultValue<LayoutTransformControl>(true);

            LayoutTransformProperty.Changed
                .AddClassHandler<LayoutTransformControl>(x => x.OnLayoutTransformChanged);

            ChildProperty.Changed
                .AddClassHandler<LayoutTransformControl>(x => x.OnChildChanged);
            UseRenderTransformProperty.Changed.AddClassHandler<LayoutTransformControl>(x => x.OnUseRenderTransformPropertyChanged);
        }

        /// <summary>
        /// Gets or sets a graphics transformation that should apply to this element when layout is performed.
        /// </summary>
        public Transform LayoutTransform
        {
            get { return GetValue(LayoutTransformProperty); }
            set { SetValue(LayoutTransformProperty, value); }
        }

        /// <summary>
        /// Utilize the <see cref="Visual.RenderTransformProperty"/> for layout transforms.
        /// </summary>
        public bool UseRenderTransform
        {
            get { return GetValue(UseRenderTransformProperty); }
            set { SetValue(UseRenderTransformProperty, value); }
        }

        public IControl TransformRoot => Child;

        /// <summary>
        /// Provides the behavior for the "Arrange" pass of layout.
        /// </summary>
        /// <param name="finalSize">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (TransformRoot == null || LayoutTransform == null)
            {
                LayoutTransform = RenderTransform;
                return base.ArrangeOverride(finalSize);
            }

            // Determine the largest available size after the transformation
            Size finalSizeTransformed = ComputeLargestTransformedSize(finalSize);
            if (IsSizeSmaller(finalSizeTransformed, TransformRoot.DesiredSize))
            {
                // Some elements do not like being given less space than they asked for (ex: TextBlock)
                // Bump the working size up to do the right thing by them
                finalSizeTransformed = TransformRoot.DesiredSize;
            }

            // Transform the working size to find its width/height
            Rect transformedRect = new Rect(0, 0, finalSizeTransformed.Width, finalSizeTransformed.Height).TransformToAABB(_transformation);
            // Create the Arrange rect to center the transformed content
            Rect finalRect = new Rect(
                -transformedRect.X + ((finalSize.Width - transformedRect.Width) / 2),
                -transformedRect.Y + ((finalSize.Height - transformedRect.Height) / 2),
                finalSizeTransformed.Width,
                finalSizeTransformed.Height);

            // Perform an Arrange on TransformRoot (containing Child)
            Size arrangedsize;
            TransformRoot.Arrange(finalRect);
            arrangedsize = TransformRoot.Bounds.Size;

            // This is the first opportunity under Silverlight to find out the Child's true DesiredSize
            if (IsSizeSmaller(finalSizeTransformed, arrangedsize) && (Size.Empty == _childActualSize))
            {
                //// Unfortunately, all the work so far is invalid because the wrong DesiredSize was used
                //// Make a note of the actual DesiredSize
                //_childActualSize = arrangedsize;
                //// Force a new measure/arrange pass
                //InvalidateMeasure();
            }
            else
            {
                // Clear the "need to measure/arrange again" flag
                _childActualSize = Size.Empty;
            }

            // Return result to perform the transformation
            return finalSize;
        }

        /// <summary>
        /// Provides the behavior for the "Measure" pass of layout.
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (TransformRoot == null || LayoutTransform == null)
            {
                return base.MeasureOverride(availableSize);
            }

            Size measureSize;
            if (_childActualSize == Size.Empty)
            {
                // Determine the largest size after the transformation
                measureSize = ComputeLargestTransformedSize(availableSize);
            }
            else
            {
                // Previous measure/arrange pass determined that Child.DesiredSize was larger than believed
                measureSize = _childActualSize;
            }

            // Perform a measure on the TransformRoot (containing Child)
            TransformRoot.Measure(measureSize);

            var desiredSize = TransformRoot.DesiredSize;

            // Transform DesiredSize to find its width/height
            Rect transformedDesiredRect = new Rect(0, 0, desiredSize.Width, desiredSize.Height).TransformToAABB(_transformation);
            Size transformedDesiredSize = new Size(transformedDesiredRect.Width, transformedDesiredRect.Height);

            // Return result to allocate enough space for the transformation
            return transformedDesiredSize;
        }

        IDisposable _renderTransformChangedEvent;

        private void OnUseRenderTransformPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            // HACK: In theory, this method and the UseRenderTransform shouldn't exist but
            //       it's hard to animate this particular control with style animations without
            //       PropertyPaths.
            //
            //       So until we get that implemented, we'll stick on this not-so-good
            //       workaround.

            var target = e.Sender as LayoutTransformControl;
            var shouldUseRenderTransform = (bool)e.NewValue;
            if (target != null)
            {
                if (shouldUseRenderTransform)
                {
                    _renderTransformChangedEvent = RenderTransformProperty.Changed
                            .Subscribe(
                                (x) =>
                                {
                                    var target2 = x.Sender as LayoutTransformControl;
                                    if (target2 != null)
                                    {
                                        target2.LayoutTransform = target2.RenderTransform;
                                    }
                                });
                }
                else
                {
                    _renderTransformChangedEvent?.Dispose();
                    LayoutTransform = null;
                }
            }
        }

        private void OnChildChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (null != TransformRoot)
            {
                TransformRoot.RenderTransform = _matrixTransform;
                TransformRoot.RenderTransformOrigin = new RelativePoint(0, 0, RelativeUnit.Absolute);
            }

            ApplyLayoutTransform();
        }

        /// <summary>
        /// Acceptable difference between two doubles.
        /// </summary>
        private const double AcceptableDelta = 0.0001;

        /// <summary>
        /// Number of decimals to round the Matrix to.
        /// </summary>
        private const int DecimalsAfterRound = 4;

        /// <summary>
        /// Actual DesiredSize of Child element (the value it returned from its MeasureOverride method).
        /// </summary>
        private Size _childActualSize = Size.Empty;

        /// <summary>
        /// RenderTransform/MatrixTransform applied to TransformRoot.
        /// </summary>
        private MatrixTransform _matrixTransform = new MatrixTransform();

        /// <summary>
        /// Transformation matrix corresponding to _matrixTransform.
        /// </summary>
        private Matrix _transformation;
        private IDisposable _transformChangedEvent = null;

        /// <summary>
        /// Returns true if Size a is smaller than Size b in either dimension.
        /// </summary>
        /// <param name="a">Second Size.</param>
        /// <param name="b">First Size.</param>
        /// <returns>True if Size a is smaller than Size b in either dimension.</returns>
        private static bool IsSizeSmaller(Size a, Size b)
        {
            return (a.Width + AcceptableDelta < b.Width) || (a.Height + AcceptableDelta < b.Height);
        }

        /// <summary>
        /// Rounds the non-offset elements of a Matrix to avoid issues due to floating point imprecision.
        /// </summary>
        /// <param name="matrix">Matrix to round.</param>
        /// <param name="decimals">Number of decimal places to round to.</param>
        /// <returns>Rounded Matrix.</returns>
        private static Matrix RoundMatrix(Matrix matrix, int decimals)
        {
            return new Matrix(
                Math.Round(matrix.M11, decimals),
                Math.Round(matrix.M12, decimals),
                Math.Round(matrix.M21, decimals),
                Math.Round(matrix.M22, decimals),
                matrix.M31,
                matrix.M32);
        }

        /// <summary>
        /// Applies the layout transform on the LayoutTransformerControl content.
        /// </summary>
        /// <remarks>
        /// Only used in advanced scenarios (like animating the LayoutTransform).
        /// Should be used to notify the LayoutTransformer control that some aspect
        /// of its Transform property has changed.
        /// </remarks>
        private void ApplyLayoutTransform()
        {
            if (LayoutTransform == null)
                return;

            // Get the transform matrix and apply it
            _transformation = RoundMatrix(LayoutTransform.Value, DecimalsAfterRound);

            if (null != _matrixTransform)
            {
                _matrixTransform.Matrix = _transformation;
            }

            // New transform means re-layout is necessary
            InvalidateMeasure();
        }

        /// <summary>
        /// Compute the largest usable size (greatest area) after applying the transformation to the specified bounds.
        /// </summary>
        /// <param name="arrangeBounds">Arrange bounds.</param>
        /// <returns>Largest Size possible.</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Closely corresponds to WPF's FrameworkElement.FindMaximalAreaLocalSpaceRect.")]
        private Size ComputeLargestTransformedSize(Size arrangeBounds)
        {
            // Computed largest transformed size
            Size computedSize = Size.Empty;

            // Detect infinite bounds and constrain the scenario
            bool infiniteWidth = double.IsInfinity(arrangeBounds.Width);
            if (infiniteWidth)
            {
                // arrangeBounds.Width = arrangeBounds.Height;
                arrangeBounds = arrangeBounds.WithWidth(arrangeBounds.Height);
            }
            bool infiniteHeight = double.IsInfinity(arrangeBounds.Height);
            if (infiniteHeight)
            {
                //arrangeBounds.Height = arrangeBounds.Width;
                arrangeBounds = arrangeBounds.WithHeight(arrangeBounds.Width);
            }

            // Capture the matrix parameters
            double a = _transformation.M11;
            double b = _transformation.M12;
            double c = _transformation.M21;
            double d = _transformation.M22;

            // Compute maximum possible transformed width/height based on starting width/height
            // These constraints define two lines in the positive x/y quadrant
            double maxWidthFromWidth = Math.Abs(arrangeBounds.Width / a);
            double maxHeightFromWidth = Math.Abs(arrangeBounds.Width / c);
            double maxWidthFromHeight = Math.Abs(arrangeBounds.Height / b);
            double maxHeightFromHeight = Math.Abs(arrangeBounds.Height / d);

            // The transformed width/height that maximize the area under each segment is its midpoint
            // At most one of the two midpoints will satisfy both constraints
            double idealWidthFromWidth = maxWidthFromWidth / 2;
            double idealHeightFromWidth = maxHeightFromWidth / 2;
            double idealWidthFromHeight = maxWidthFromHeight / 2;
            double idealHeightFromHeight = maxHeightFromHeight / 2;

            // Compute slope of both constraint lines
            double slopeFromWidth = -(maxHeightFromWidth / maxWidthFromWidth);
            double slopeFromHeight = -(maxHeightFromHeight / maxWidthFromHeight);

            if ((0 == arrangeBounds.Width) || (0 == arrangeBounds.Height))
            {
                // Check for empty bounds
                computedSize = new Size(arrangeBounds.Width, arrangeBounds.Height);
            }
            else if (infiniteWidth && infiniteHeight)
            {
                // Check for completely unbound scenario
                computedSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
            }
            else if (!_transformation.HasInverse)
            {
                // Check for singular matrix
                computedSize = new Size(0, 0);
            }
            else if ((0 == b) || (0 == c))
            {
                // Check for 0/180 degree special cases
                double maxHeight = (infiniteHeight ? double.PositiveInfinity : maxHeightFromHeight);
                double maxWidth = (infiniteWidth ? double.PositiveInfinity : maxWidthFromWidth);
                if ((0 == b) && (0 == c))
                {
                    // No constraints
                    computedSize = new Size(maxWidth, maxHeight);
                }
                else if (0 == b)
                {
                    // Constrained by width
                    double computedHeight = Math.Min(idealHeightFromWidth, maxHeight);
                    computedSize = new Size(
                        maxWidth - Math.Abs((c * computedHeight) / a),
                        computedHeight);
                }
                else if (0 == c)
                {
                    // Constrained by height
                    double computedWidth = Math.Min(idealWidthFromHeight, maxWidth);
                    computedSize = new Size(
                        computedWidth,
                        maxHeight - Math.Abs((b * computedWidth) / d));
                }
            }
            else if ((0 == a) || (0 == d))
            {
                // Check for 90/270 degree special cases
                double maxWidth = (infiniteHeight ? double.PositiveInfinity : maxWidthFromHeight);
                double maxHeight = (infiniteWidth ? double.PositiveInfinity : maxHeightFromWidth);
                if ((0 == a) && (0 == d))
                {
                    // No constraints
                    computedSize = new Size(maxWidth, maxHeight);
                }
                else if (0 == a)
                {
                    // Constrained by width
                    double computedHeight = Math.Min(idealHeightFromHeight, maxHeight);
                    computedSize = new Size(
                        maxWidth - Math.Abs((d * computedHeight) / b),
                        computedHeight);
                }
                else if (0 == d)
                {
                    // Constrained by height
                    double computedWidth = Math.Min(idealWidthFromWidth, maxWidth);
                    computedSize = new Size(
                        computedWidth,
                        maxHeight - Math.Abs((a * computedWidth) / c));
                }
            }
            else if (idealHeightFromWidth <= ((slopeFromHeight * idealWidthFromWidth) + maxHeightFromHeight))
            {
                // Check the width midpoint for viability (by being below the height constraint line)
                computedSize = new Size(idealWidthFromWidth, idealHeightFromWidth);
            }
            else if (idealHeightFromHeight <= ((slopeFromWidth * idealWidthFromHeight) + maxHeightFromWidth))
            {
                // Check the height midpoint for viability (by being below the width constraint line)
                computedSize = new Size(idealWidthFromHeight, idealHeightFromHeight);
            }
            else
            {
                // Neither midpoint is viable; use the intersection of the two constraint lines instead
                // Compute width by setting heights equal (m1*x+c1=m2*x+c2)
                double computedWidth = (maxHeightFromHeight - maxHeightFromWidth) / (slopeFromWidth - slopeFromHeight);
                // Compute height from width constraint line (y=m*x+c; using height would give same result)
                computedSize = new Size(
                    computedWidth,
                    (slopeFromWidth * computedWidth) + maxHeightFromWidth);
            }

            // Return result
            return computedSize;
        }

        private void OnLayoutTransformChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newTransform = e.NewValue as Transform;

            _transformChangedEvent?.Dispose();
            _transformChangedEvent = null;

            if (newTransform != null)
            {
                _transformChangedEvent = Observable.FromEventPattern<EventHandler, EventArgs>(
                                        v => newTransform.Changed += v, v => newTransform.Changed -= v)
                                        .Subscribe(onNext: v => ApplyLayoutTransform());
            }

            ApplyLayoutTransform();
        }
    }
}
