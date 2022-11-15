using System;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    [TemplatePart("PART_Indicator", typeof(Border))]
    [PseudoClasses(":vertical", ":horizontal", ":indeterminate")]
    public class ProgressBar : RangeBase
    {
        public class ProgressBarTemplateProperties : AvaloniaObject
        {
            private double _container2Width;
            private double _containerWidth;
            private double _containerAnimationStartPosition;
            private double _containerAnimationEndPosition;
            private double _container2AnimationStartPosition;
            private double _container2AnimationEndPosition;

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> ContainerAnimationStartPositionProperty =
           AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
               nameof(ContainerAnimationStartPosition),
               p => p.ContainerAnimationStartPosition,
               (p, o) => p.ContainerAnimationStartPosition = o, 0d);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> ContainerAnimationEndPositionProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(ContainerAnimationEndPosition),
                    p => p.ContainerAnimationEndPosition,
                    (p, o) => p.ContainerAnimationEndPosition = o, 0d);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> Container2AnimationStartPositionProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(Container2AnimationStartPosition),
                    p => p.Container2AnimationStartPosition,
                    (p, o) => p.Container2AnimationStartPosition = o, 0d);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> Container2AnimationEndPositionProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(Container2AnimationEndPosition),
                    p => p.Container2AnimationEndPosition,
                    (p, o) => p.Container2AnimationEndPosition = o);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> Container2WidthProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(Container2Width),
                    p => p.Container2Width,
                    (p, o) => p.Container2Width = o);

            public static readonly DirectProperty<ProgressBarTemplateProperties, double> ContainerWidthProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateProperties, double>(
                    nameof(ContainerWidth),
                    p => p.ContainerWidth,
                    (p, o) => p.ContainerWidth = o);

            public double ContainerAnimationStartPosition
            {
                get => _containerAnimationStartPosition;
                set => SetAndRaise(ContainerAnimationStartPositionProperty, ref _containerAnimationStartPosition, value);
            }

            public double ContainerAnimationEndPosition
            {
                get => _containerAnimationEndPosition;
                set => SetAndRaise(ContainerAnimationEndPositionProperty, ref _containerAnimationEndPosition, value);
            }

            public double Container2AnimationStartPosition
            {
                get => _container2AnimationStartPosition;
                set => SetAndRaise(Container2AnimationStartPositionProperty, ref _container2AnimationStartPosition, value);
            }

            public double Container2Width
            {
                get => _container2Width;
                set => SetAndRaise(Container2WidthProperty, ref _container2Width, value);
            }

            public double ContainerWidth
            {
                get => _containerWidth;
                set => SetAndRaise(ContainerWidthProperty, ref _containerWidth, value);
            }

            public double Container2AnimationEndPosition
            {
                get => _container2AnimationEndPosition;
                set => SetAndRaise(Container2AnimationEndPositionProperty, ref _container2AnimationEndPosition, value);
            }
        }

        private double _percentage;
        private double _indeterminateStartingOffset;
        private double _indeterminateEndingOffset;
        private Border? _indicator;
        private IDisposable? _trackSizeChangedListener;

        public static readonly StyledProperty<bool> IsIndeterminateProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(IsIndeterminate));

        public static readonly StyledProperty<bool> ShowProgressTextProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(ShowProgressText));

        public static readonly StyledProperty<string> ProgressTextFormatProperty =
            AvaloniaProperty.Register<ProgressBar, string>(nameof(ProgressTextFormat), "{1:0}%");

        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<ProgressBar, Orientation>(nameof(Orientation), Orientation.Horizontal);

        public static readonly DirectProperty<ProgressBar, double> PercentageProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(Percentage),
                o => o.Percentage);

        public static readonly DirectProperty<ProgressBar, double> IndeterminateStartingOffsetProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(IndeterminateStartingOffset),
                p => p.IndeterminateStartingOffset,
                (p, o) => p.IndeterminateStartingOffset = o);

        public static readonly DirectProperty<ProgressBar, double> IndeterminateEndingOffsetProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(IndeterminateEndingOffset),
                p => p.IndeterminateEndingOffset,
                (p, o) => p.IndeterminateEndingOffset = o);

        public double Percentage
        {
            get { return _percentage; }
            private set { SetAndRaise(PercentageProperty, ref _percentage, value); }
        }

        public double IndeterminateStartingOffset
        {
            get => _indeterminateStartingOffset;
            set => SetAndRaise(IndeterminateStartingOffsetProperty, ref _indeterminateStartingOffset, value);
        }

        public double IndeterminateEndingOffset
        {
            get => _indeterminateEndingOffset;
            set => SetAndRaise(IndeterminateEndingOffsetProperty, ref _indeterminateEndingOffset, value);
        }

        static ProgressBar()
        {
            ValueProperty.OverrideMetadata<ProgressBar>(new DirectPropertyMetadata<double>(defaultBindingMode: BindingMode.OneWay));
            ValueProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
            MinimumProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
            MaximumProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
            IsIndeterminateProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
            OrientationProperty.Changed.AddClassHandler<ProgressBar>((x, e) => x.UpdateIndicatorWhenPropChanged(e));
        }

        public ProgressBar()
        {
            UpdatePseudoClasses(IsIndeterminate, Orientation);
        }

        public ProgressBarTemplateProperties TemplateProperties { get; } = new ProgressBarTemplateProperties();

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

        public string ProgressTextFormat
        {
            get => GetValue(ProgressTextFormatProperty);
            set => SetValue(ProgressTextFormatProperty, value);
        }

        public Orientation Orientation
        {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);
            UpdateIndicator();
            return result;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == IsIndeterminateProperty)
            {
                UpdatePseudoClasses(change.GetNewValue<bool>(), null);
            }
            else if (change.Property == OrientationProperty)
            {
                UpdatePseudoClasses(null, change.GetNewValue<Orientation>());
            }
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            // dispose any previous track size listener
            _trackSizeChangedListener?.Dispose();

            _indicator = e.NameScope.Get<Border>("PART_Indicator");

            // listen to size changes of the indicators track (parent) and update the indicator there. 
            _trackSizeChangedListener = _indicator.Parent?.GetPropertyChangedObservable(BoundsProperty)
                .Subscribe(_ => UpdateIndicator());

            UpdateIndicator();
        }

        private void UpdateIndicator()
        {
            // Gets the size of the parent indicator container
            var barSize = _indicator?.Parent?.Bounds.Size ?? Bounds.Size;

            if (_indicator != null)
            {
                if (IsIndeterminate)
                {
                    // Pulled from ModernWPF.

                    var dim = Orientation == Orientation.Horizontal ? barSize.Width : barSize.Height;
                    var barIndicatorWidth = dim * 0.4; // Indicator width at 40% of ProgressBar
                    var barIndicatorWidth2 = dim * 0.6; // Indicator width at 60% of ProgressBar

                    TemplateProperties.ContainerWidth = barIndicatorWidth;
                    TemplateProperties.Container2Width = barIndicatorWidth2;

                    TemplateProperties.ContainerAnimationStartPosition = barIndicatorWidth * -1.8; // Position at -180%
                    TemplateProperties.ContainerAnimationEndPosition = barIndicatorWidth * 3.0; // Position at 300%

                    TemplateProperties.Container2AnimationStartPosition = barIndicatorWidth2 * -1.5; // Position at -150%
                    TemplateProperties.Container2AnimationEndPosition = barIndicatorWidth2 * 1.66; // Position at 166%


                    // Remove these properties when we switch to fluent as default and removed the old one.
                    IndeterminateStartingOffset = -dim;
                    IndeterminateEndingOffset = dim;

                    var padding = Padding;
                    var rectangle = new RectangleGeometry(
                        new Rect(
                            padding.Left,
                            padding.Top,
                            barSize.Width - (padding.Right + padding.Left),
                            barSize.Height - (padding.Bottom + padding.Top)
                            ));
                }
                else
                {
                    double percent = Maximum == Minimum ? 1.0 : (Value - Minimum) / (Maximum - Minimum);

                    // When the Orientation changed, the indicator's Width or Height should set to double.NaN.
                    if (Orientation == Orientation.Horizontal)
                    {
                        _indicator.Width = barSize.Width * percent;
                        _indicator.Height = double.NaN;
                    }
                    else
                    {
                        _indicator.Width = double.NaN;
                        _indicator.Height = barSize.Height * percent;
                    }


                    Percentage = percent * 100;
                }
            }
        }

        private void UpdateIndicatorWhenPropChanged(AvaloniaPropertyChangedEventArgs e)
        {
            UpdateIndicator();
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
