
using System;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    public class ProgressBar : RangeBase
    {
        public static readonly StyledProperty<bool> IsIndeterminateProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(IsIndeterminate));

        public static readonly StyledProperty<bool> ShowProgressTextProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(ShowProgressText));

        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<ProgressBar, Orientation>(nameof(Orientation), Orientation.Horizontal);

        public static readonly DirectProperty<ProgressBar, double> ContainerAnimationStartPositionProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(ContainerAnimationStartPosition),
                p => p.ContainerAnimationStartPosition,
                (p, o) => p.ContainerAnimationStartPosition = o);

        public static readonly DirectProperty<ProgressBar, double> ContainerAnimationEndPositionProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(ContainerAnimationEndPosition),
                p => p.ContainerAnimationEndPosition,
                (p, o) => p.ContainerAnimationEndPosition = o);


        public static readonly DirectProperty<ProgressBar, double> Container2AnimationStartPositionProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(Container2AnimationStartPosition),
                p => p.Container2AnimationStartPosition,
                (p, o) => p.Container2AnimationStartPosition = o);

        public static readonly DirectProperty<ProgressBar, double> Container2AnimationEndPositionProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(Container2AnimationEndPosition),
                p => p.Container2AnimationEndPosition,
                (p, o) => p.Container2AnimationEndPosition = o);


        public static readonly DirectProperty<ProgressBar, double> ContainerAnimationMidPositionProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(ContainerAnimationMidPosition),
                p => p.ContainerAnimationMidPosition,
                (p, o) => p.ContainerAnimationMidPosition = o);
                
        private Border _indicator;

        static ProgressBar()
        {
            ValueProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
            IsIndeterminateProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
        }

        public ProgressBar()
        {
            UpdatePseudoClasses(IsIndeterminate, Orientation);
        }

        public bool IsIndeterminate
        {
            get => GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        public bool ShowProgressText
        {
            get => GetValue(ShowProgressTextProperty);
            set => SetValue(ShowProgressTextProperty, value);
        }

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        private double _containerAnimationStartPosition;
        public double ContainerAnimationStartPosition
        {
            get => _containerAnimationStartPosition;
            set => SetAndRaise(ContainerAnimationStartPositionProperty, ref _containerAnimationStartPosition, value);
        }

        private double _containerAnimationEndPosition;
        public double ContainerAnimationEndPosition
        {
            get => _containerAnimationEndPosition;
            set => SetAndRaise(ContainerAnimationEndPositionProperty, ref _containerAnimationEndPosition, value);
        }

        private double _container2AnimationStartPosition;
        public double Container2AnimationStartPosition
        {
            get => _container2AnimationStartPosition;
            set => SetAndRaise(Container2AnimationStartPositionProperty, ref _container2AnimationStartPosition, value);
        }

        private double _container2AnimationEndPosition;
        public double Container2AnimationEndPosition
        {
            get => _container2AnimationEndPosition;
            set => SetAndRaise(Container2AnimationEndPositionProperty, ref _container2AnimationEndPosition, value);
        }


        private double _containerAnimationMidPosition;
        public double ContainerAnimationMidPosition
        {
            get => _containerAnimationMidPosition;
            set => SetAndRaise(ContainerAnimationMidPositionProperty, ref _containerAnimationMidPosition, value);
        }
 
        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            UpdateIndicator(finalSize);
            return base.ArrangeOverride(finalSize);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsIndeterminateProperty)
            {
                UpdatePseudoClasses(change.NewValue.GetValueOrDefault<bool>(), null);
            }
            else if (change.Property == OrientationProperty)
            {
                UpdatePseudoClasses(null, change.NewValue.GetValueOrDefault<Orientation>());
            }
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _indicator = e.NameScope.Get<Border>("PART_Indicator");

            UpdateIndicator(Bounds.Size);
        }

        private void UpdateIndicator(Size bounds)
        {
            if (_indicator != null)
            {
                if (IsIndeterminate)
                {
                    // Pulled from ModernWPF.

                    var dim = Orientation == Orientation.Horizontal ? bounds.Width : bounds.Height;
                    var barIndicatorWidth = dim * 0.4; // Indicator width at 40% of ProgressBar
                    var barIndicatorWidth2 = dim * 0.6; // Indicator width at 60% of ProgressBar

                    ContainerAnimationStartPosition = barIndicatorWidth * -1.0; // Position at -100%
                    ContainerAnimationEndPosition = barIndicatorWidth * 3.0; // Position at 300%
                    Container2AnimationStartPosition = barIndicatorWidth2 * -1.5; // Position at -150%
                    Container2AnimationEndPosition = barIndicatorWidth2 * 1.66; // Position at 166%
                    ContainerAnimationMidPosition = dim * 0.2; 
                }
                else
                {
                    double percent = Maximum == Minimum ? 1.0 : (Value - Minimum) / (Maximum - Minimum);

                    if (Orientation == Orientation.Horizontal)
                        _indicator.Width = bounds.Width * percent;
                    else
                        _indicator.Height = bounds.Height * percent;
                }
            }
        }

        private void UpdateIndicatorWhenPropChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateIndicator(Bounds.Size);
        }

        private void UpdatePseudoClasses(
            bool? isIndeterminate,
            Orientation? o)
        {
            if (isIndeterminate.HasValue)
            {
                PseudoClasses.Set(":indeterminate", isIndeterminate.Value);
            }

            if (o.HasValue)
            {
                PseudoClasses.Set(":vertical", o == Orientation.Vertical);
                PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
            }
        }
    }
}
