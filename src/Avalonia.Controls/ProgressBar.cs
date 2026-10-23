using System;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control used to indicate the progress of an operation.
    /// </summary>
    [TemplatePart("PART_Indicator", typeof(Border), IsRequired = true)]
    [PseudoClasses(":vertical", ":horizontal", ":indeterminate")]
    public class ProgressBar : RangeBase
    {
        /// <summary>
        /// Provides calculated values for use with the <see cref="ProgressBar"/>'s control theme or template.
        /// </summary>
        /// <remarks>
        /// This class is NOT intended for general use outside of control templates.
        /// </remarks>
        public class ProgressBarTemplateSettings : AvaloniaObject
        {
            private double _container2Width;
            private double _containerWidth;
            private double _containerAnimationStartPosition;
            private double _containerAnimationEndPosition;
            private double _container2AnimationStartPosition;
            private double _container2AnimationEndPosition;
            private Vector3D _containerAnimationStartOffset;
            private Vector3D _containerAnimationEndOffset;
            private Vector3D _container2AnimationStartOffset;
            private Vector3D _container2AnimationEndOffset;
            private double _indeterminateStartingOffset;
            private double _indeterminateEndingOffset;
            private Vector3D _indeterminateStartingOffset3D;
            private Vector3D _indeterminateEndingOffset3D;

            /// <summary>
            /// Defines the <see cref="ContainerAnimationStartPosition"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double>
                ContainerAnimationStartPositionProperty =
                    AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                        nameof(ContainerAnimationStartPosition),
                        p => p.ContainerAnimationStartPosition,
                        (p, o) => p.ContainerAnimationStartPosition = o);

            /// <summary>
            /// Defines the <see cref="ContainerAnimationEndPosition"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double>
                ContainerAnimationEndPositionProperty =
                    AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                        nameof(ContainerAnimationEndPosition),
                        p => p.ContainerAnimationEndPosition,
                        (p, o) => p.ContainerAnimationEndPosition = o);

            /// <summary>
            /// Defines the <see cref="Container2AnimationStartPosition"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double>
                Container2AnimationStartPositionProperty =
                    AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                        nameof(Container2AnimationStartPosition),
                        p => p.Container2AnimationStartPosition,
                        (p, o) => p.Container2AnimationStartPosition = o);

            /// <summary>
            /// Defines the <see cref="Container2AnimationEndPosition"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double>
                Container2AnimationEndPositionProperty =
                    AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                        nameof(Container2AnimationEndPosition),
                        p => p.Container2AnimationEndPosition,
                        (p, o) => p.Container2AnimationEndPosition = o);

            /// <summary>
            /// Defines the <see cref="ContainerAnimationStartOffset"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, Vector3D>
                ContainerAnimationStartOffsetProperty =
                    AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, Vector3D>(
                        nameof(ContainerAnimationStartOffset),
                        p => p.ContainerAnimationStartOffset,
                        (p, o) => p.ContainerAnimationStartOffset = o);

            /// <summary>
            /// Defines the <see cref="ContainerAnimationEndOffset"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, Vector3D>
                ContainerAnimationEndOffsetProperty =
                    AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, Vector3D>(
                        nameof(ContainerAnimationEndOffset),
                        p => p.ContainerAnimationEndOffset,
                        (p, o) => p.ContainerAnimationEndOffset = o);

            /// <summary>
            /// Defines the <see cref="Container2AnimationStartOffset"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, Vector3D>
                Container2AnimationStartOffsetProperty =
                    AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, Vector3D>(
                        nameof(Container2AnimationStartOffset),
                        p => p.Container2AnimationStartOffset,
                        (p, o) => p.Container2AnimationStartOffset = o);

            /// <summary>
            /// Defines the <see cref="Container2AnimationEndOffset"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, Vector3D>
                Container2AnimationEndOffsetProperty =
                    AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, Vector3D>(
                        nameof(Container2AnimationEndOffset),
                        p => p.Container2AnimationEndOffset,
                        (p, o) => p.Container2AnimationEndOffset = o);

            /// <summary>
            /// Defines the <see cref="Container2Width"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double> Container2WidthProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                    nameof(Container2Width),
                    p => p.Container2Width,
                    (p, o) => p.Container2Width = o);

            /// <summary>
            /// Defines the <see cref="ContainerWidth"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double> ContainerWidthProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                    nameof(ContainerWidth),
                    p => p.ContainerWidth,
                    (p, o) => p.ContainerWidth = o);

            /// <summary>
            /// Defines the <see cref="IndeterminateStartingOffset"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double> IndeterminateStartingOffsetProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                    nameof(IndeterminateStartingOffset),
                    p => p.IndeterminateStartingOffset,
                    (p, o) => p.IndeterminateStartingOffset = o);
            
            /// <summary>
            /// Defines the <see cref="IndeterminateEndingOffset"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, double> IndeterminateEndingOffsetProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, double>(
                    nameof(IndeterminateEndingOffset),
                    p => p.IndeterminateEndingOffset,
                    (p, o) => p.IndeterminateEndingOffset = o);

            /// <summary>
            /// Defines the <see cref="IndeterminateStartingOffset3D"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, Vector3D> IndeterminateStartingOffset3DProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, Vector3D>(
                    nameof(IndeterminateStartingOffset3D),
                    p => p.IndeterminateStartingOffset3D,
                    (p, o) => p.IndeterminateStartingOffset3D = o);

            /// <summary>
            /// Defines the <see cref="IndeterminateEndingOffset3D"/> property.
            /// </summary>
            public static readonly DirectProperty<ProgressBarTemplateSettings, Vector3D> IndeterminateEndingOffset3DProperty =
                AvaloniaProperty.RegisterDirect<ProgressBarTemplateSettings, Vector3D>(
                    nameof(IndeterminateEndingOffset3D),
                    p => p.IndeterminateEndingOffset3D,
                    (p, o) => p.IndeterminateEndingOffset3D = o);

            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the first indeterminate indicator's width.
            /// </summary>
            public double ContainerWidth
            {
                get => _containerWidth;
                set => SetAndRaise(ContainerWidthProperty, ref _containerWidth, value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the first indeterminate indicator's start offset when animated.
            /// </summary>
            public Vector3D ContainerAnimationStartOffset
            {
                get => _containerAnimationStartOffset;
                set => SetAndRaise(ContainerAnimationStartOffsetProperty, ref _containerAnimationStartOffset, value);
            }
            
            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the second indeterminate indicator's width.
            /// </summary>
            public double Container2Width
            {
                get => _container2Width;
                set => SetAndRaise(Container2WidthProperty, ref _container2Width, value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the first indeterminate indicator's end offset when animated.
            /// </summary>
            public Vector3D ContainerAnimationEndOffset
            {
                get => _containerAnimationEndOffset;
                set => SetAndRaise(ContainerAnimationEndOffsetProperty, ref _containerAnimationEndOffset, value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the first indeterminate indicator's start position when animated.
            /// </summary>
            public double ContainerAnimationStartPosition
            {
                get => _containerAnimationStartPosition;
                set => SetAndRaise(ContainerAnimationStartPositionProperty, ref _containerAnimationStartPosition,
                    value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the second indeterminate indicator's start offset when animated.
            /// </summary>
            public Vector3D Container2AnimationStartOffset
            {
                get => _container2AnimationStartOffset;
                set => SetAndRaise(Container2AnimationStartOffsetProperty, ref _container2AnimationStartOffset, value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the first indeterminate indicator's end position when animated.
            /// </summary>
            public double ContainerAnimationEndPosition
            {
                get => _containerAnimationEndPosition;
                set => SetAndRaise(ContainerAnimationEndPositionProperty, ref _containerAnimationEndPosition, value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the second indeterminate indicator's end offset when animated.
            /// </summary>
            public Vector3D Container2AnimationEndOffset
            {
                get => _container2AnimationEndOffset;
                set => SetAndRaise(Container2AnimationEndOffsetProperty, ref _container2AnimationEndOffset, value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the second indeterminate indicator's start position when animated.
            /// </summary>
            public double Container2AnimationStartPosition
            {
                get => _container2AnimationStartPosition;
                set => SetAndRaise(Container2AnimationStartPositionProperty, ref _container2AnimationStartPosition,
                    value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Simple to define the starting offset of its indeterminate animation.
            /// </summary>
            public Vector3D IndeterminateStartingOffset3D
            {
                get => _indeterminateStartingOffset3D;
                set => SetAndRaise(IndeterminateStartingOffset3DProperty, ref _indeterminateStartingOffset3D, value);
            }
            
            /// <summary>
            /// Used by Avalonia.Themes.Fluent to define the second indeterminate indicator's end position when animated.
            /// </summary>
            public double Container2AnimationEndPosition
            {
                get => _container2AnimationEndPosition;
                set => SetAndRaise(Container2AnimationEndPositionProperty, ref _container2AnimationEndPosition, value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Simple to define the ending offset of its indeterminate animation.
            /// </summary>
            public Vector3D IndeterminateEndingOffset3D
            {
                get => _indeterminateEndingOffset3D;
                set => SetAndRaise(IndeterminateEndingOffset3DProperty, ref _indeterminateEndingOffset3D, value);
            }

            /// <summary>
            /// Used by Avalonia.Themes.Simple  to define the starting point of its indeterminate animation.
            /// </summary>
            public double IndeterminateStartingOffset
            {
                get => _indeterminateStartingOffset;
                set => SetAndRaise(IndeterminateStartingOffsetProperty, ref _indeterminateStartingOffset, value);
            }
            
            /// <summary>
            /// Used by Avalonia.Themes.Simple to define the ending point of its indeterminate animation.
            /// </summary>
            public double IndeterminateEndingOffset
            {
                get => _indeterminateEndingOffset;
                set => SetAndRaise(IndeterminateEndingOffsetProperty, ref _indeterminateEndingOffset, value);
            }
        }

        private double _percentage;
        private Border? _indicator;
        private IDisposable? _trackSizeChangedListener;

        /// <summary>
        /// Defines the <see cref="IsIndeterminate"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsIndeterminateProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(IsIndeterminate));

        /// <summary>
        /// Defines the <see cref="ShowProgressText"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowProgressTextProperty =
            AvaloniaProperty.Register<ProgressBar, bool>(nameof(ShowProgressText));

        /// <summary>
        /// Defines the <see cref="ProgressTextFormat"/> property.
        /// </summary>
        public static readonly StyledProperty<string> ProgressTextFormatProperty =
            AvaloniaProperty.Register<ProgressBar, string>(nameof(ProgressTextFormat), "{1:0}%");

        /// <summary>
        /// Defines the <see cref="Orientation"/> property.
        /// </summary>
        public static readonly StyledProperty<Orientation> OrientationProperty =
            AvaloniaProperty.Register<ProgressBar, Orientation>(nameof(Orientation));

        /// <summary>
        /// Defines the <see cref="Percentage"/> property.
        /// </summary>
        public static readonly DirectProperty<ProgressBar, double> PercentageProperty =
            AvaloniaProperty.RegisterDirect<ProgressBar, double>(
                nameof(Percentage),
                o => o.Percentage);

        /// <summary>
        /// Gets the overall percentage complete of the progress 
        /// </summary>
        /// <remarks>
        /// This read-only property is automatically calculated using the current <see cref="RangeBase.Value"/> and
        /// the effective range (<see cref="RangeBase.Maximum"/> - <see cref="RangeBase.Minimum"/>).
        /// </remarks>
        public double Percentage
        {
            get => _percentage;
            private set => SetAndRaise(PercentageProperty, ref _percentage, value);
        }

        static ProgressBar()
        {
            ValueProperty.OverrideMetadata<ProgressBar>(new(defaultBindingMode: BindingMode.OneWay));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBar"/> class.
        /// </summary>
        public ProgressBar()
        {
            UpdatePseudoClasses(IsIndeterminate, Orientation);
        }

        /// <summary>
        /// Gets or sets the TemplateSettings for the <see cref="ProgressBar"/>.
        /// </summary>
        public ProgressBarTemplateSettings TemplateSettings { get; } = new ProgressBarTemplateSettings();

        /// <summary>
        /// Gets or sets a value indicating whether the progress bar shows the actual value or a generic,
        /// continues progress indicator (indeterminate state).
        /// </summary>
        public bool IsIndeterminate
        {
            get => GetValue(IsIndeterminateProperty);
            set => SetValue(IsIndeterminateProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether progress text will be shown.
        /// </summary>
        public bool ShowProgressText
        {
            get => GetValue(ShowProgressTextProperty);
            set => SetValue(ShowProgressTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string applied to the internally calculated progress text before it is shown.
        /// </summary>
        public string ProgressTextFormat
        {
            get => GetValue(ProgressTextFormatProperty);
            set => SetValue(ProgressTextFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets the orientation of the <see cref="ProgressBar"/>.
        /// </summary>
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

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty ||
                change.Property == MinimumProperty ||
                change.Property == MaximumProperty ||
                change.Property == IsIndeterminateProperty ||
                change.Property == OrientationProperty)
            {
                UpdateIndicator();
            }

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

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ProgressBarAutomationPeer(this);
        }

        private void UpdateIndicator()
        {
            // Gets the size of the parent indicator container
            var barSize = _indicator?.VisualParent?.Bounds.Size ?? Bounds.Size;

            if (_indicator == null)
                return;

            // Pulled from ModernWPF.

            var dim = Orientation == Orientation.Horizontal ? barSize.Width : barSize.Height;
            var barIndicatorWidth = dim * 0.4; // Indicator width at 40% of ProgressBar
            var barIndicatorWidth2 = dim * 0.6; // Indicator width at 60% of ProgressBar

            TemplateSettings.ContainerWidth = barIndicatorWidth;
            TemplateSettings.Container2Width = barIndicatorWidth2;

            TemplateSettings.ContainerAnimationStartPosition = barIndicatorWidth * -1.8; // Position at -180%
            TemplateSettings.ContainerAnimationEndPosition = barIndicatorWidth * 3.0; // Position at 300%

            TemplateSettings.Container2AnimationStartPosition = barIndicatorWidth2 * -1.5; // Position at -150%
            TemplateSettings.Container2AnimationEndPosition = barIndicatorWidth2 * 1.66; // Position at 166%

            var containerAnimationStartOffset = TemplateSettings.ContainerAnimationStartPosition;
            var containerAnimationEndOffset = TemplateSettings.ContainerAnimationEndPosition;
            var container2AnimationStartOffset = TemplateSettings.Container2AnimationStartPosition;
            var container2AnimationEndOffset = TemplateSettings.Container2AnimationEndPosition;

            TemplateSettings.ContainerAnimationStartOffset = Orientation == Orientation.Horizontal
                ? new Vector3D(containerAnimationStartOffset, 0, 0)
                : new Vector3D(0, containerAnimationStartOffset, 0);
            TemplateSettings.ContainerAnimationEndOffset = Orientation == Orientation.Horizontal
                ? new Vector3D(containerAnimationEndOffset, 0, 0)
                : new Vector3D(0, containerAnimationEndOffset, 0);
            TemplateSettings.Container2AnimationStartOffset = Orientation == Orientation.Horizontal
                ? new Vector3D(container2AnimationStartOffset, 0, 0)
                : new Vector3D(0, container2AnimationStartOffset, 0);
            TemplateSettings.Container2AnimationEndOffset = Orientation == Orientation.Horizontal
                ? new Vector3D(container2AnimationEndOffset, 0, 0)
                : new Vector3D(0, container2AnimationEndOffset, 0);

            TemplateSettings.IndeterminateStartingOffset = -dim;
            TemplateSettings.IndeterminateEndingOffset = dim;
            TemplateSettings.IndeterminateStartingOffset3D = Orientation == Orientation.Horizontal
                ? new Vector3D(-dim, 0, 0)
                : new Vector3D(0, -dim, 0);
            TemplateSettings.IndeterminateEndingOffset3D = Orientation == Orientation.Horizontal
                ? new Vector3D(dim, 0, 0)
                : new Vector3D(0, dim, 0);

            var percent = Math.Abs(Maximum - Minimum) < double.Epsilon ?
                1.0 :
                (Value - Minimum) / (Maximum - Minimum);

            // When the Orientation changed, the indicator's Width or Height should set to double.NaN.
            // Indicator size calculation should consider the ProgressBar's Padding property setting
            if (Orientation == Orientation.Horizontal)
            {
                var width = (barSize.Width - _indicator.Margin.Left - _indicator.Margin.Right) * percent;
                _indicator.Width = width > 0 ? width : 0;
                _indicator.Height = double.NaN;
            }
            else
            {
                _indicator.Width = double.NaN;
                var height = (barSize.Height - _indicator.Margin.Top - _indicator.Margin.Bottom) * percent;
                _indicator.Height = height > 0 ? height : 0;
            }

            Percentage = percent * 100;
        }

        private void UpdatePseudoClasses(
            bool? isIndeterminate,
            Orientation? o)
        {
            if (isIndeterminate.HasValue)
            {
                PseudoClasses.Set(":indeterminate", isIndeterminate.Value);
            }

            if (!o.HasValue) return;
            PseudoClasses.Set(":vertical", o == Orientation.Vertical);
            PseudoClasses.Set(":horizontal", o == Orientation.Horizontal);
        }
    }
}
