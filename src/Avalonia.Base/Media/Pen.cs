using System;
using Avalonia.Media.Immutable;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how a stroke is drawn.
    /// </summary>
    public sealed class Pen : AvaloniaObject, IPen
    {
        /// <summary>
        /// Defines the <see cref="Brush"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BrushProperty =
            AvaloniaProperty.Register<Pen, IBrush?>(nameof(Brush));

        /// <summary>
        /// Defines the <see cref="Thickness"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ThicknessProperty =
            AvaloniaProperty.Register<Pen, double>(nameof(Thickness), 1.0);

        /// <summary>
        /// Defines the <see cref="DashStyle"/> property.
        /// </summary>
        public static readonly StyledProperty<IDashStyle?> DashStyleProperty =
            AvaloniaProperty.Register<Pen, IDashStyle?>(nameof(DashStyle));

        /// <summary>
        /// Defines the <see cref="LineCap"/> property.
        /// </summary>
        public static readonly StyledProperty<PenLineCap> LineCapProperty =
            AvaloniaProperty.Register<Pen, PenLineCap>(nameof(LineCap));

        /// <summary>
        /// Defines the <see cref="LineJoin"/> property.
        /// </summary>
        public static readonly StyledProperty<PenLineJoin> LineJoinProperty =
            AvaloniaProperty.Register<Pen, PenLineJoin>(nameof(LineJoin));

        /// <summary>
        /// Defines the <see cref="MiterLimit"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MiterLimitProperty =
            AvaloniaProperty.Register<Pen, double>(nameof(MiterLimit), 10.0);

        private EventHandler? _invalidated;
        private IAffectsRender? _subscribedToBrush;
        private IAffectsRender? _subscribedToDashes;
        private TargetWeakEventSubscriber<Pen, EventArgs>? _weakSubscriber;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pen"/> class.
        /// </summary>
        public Pen()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pen"/> class.
        /// </summary>
        /// <param name="color">The stroke color.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashStyle">The dash style.</param>
        /// <param name="lineCap">Specifies the type of graphic shape to use on both ends of a line.</param>
        /// <param name="lineJoin">The line join.</param>
        /// <param name="miterLimit">The miter limit.</param>
        public Pen(
            uint color,
            double thickness = 1.0,
            IDashStyle? dashStyle = null,
            PenLineCap lineCap = PenLineCap.Flat,
            PenLineJoin lineJoin = PenLineJoin.Miter,
            double miterLimit = 10.0) : this(new SolidColorBrush(color), thickness, dashStyle, lineCap, lineJoin, miterLimit)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pen"/> class.
        /// </summary>
        /// <param name="brush">The brush used to draw.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="dashStyle">The dash style.</param>
        /// <param name="lineCap">The line cap.</param>
        /// <param name="lineJoin">The line join.</param>
        /// <param name="miterLimit">The miter limit.</param>
        public Pen(
            IBrush? brush,
            double thickness = 1.0,
            IDashStyle? dashStyle = null,
            PenLineCap lineCap = PenLineCap.Flat,
            PenLineJoin lineJoin = PenLineJoin.Miter,
            double miterLimit = 10.0)
        {
            Brush = brush;
            Thickness = thickness;
            LineCap = lineCap;
            LineJoin = lineJoin;
            MiterLimit = miterLimit;
            DashStyle = dashStyle;
        }

        /// <summary>
        /// Gets or sets the brush used to draw the stroke.
        /// </summary>
        public IBrush? Brush
        {
            get => GetValue(BrushProperty);
            set => SetValue(BrushProperty, value);
        }

        private static readonly WeakEvent<IAffectsRender, EventArgs> InvalidatedWeakEvent =
            WeakEvent.Register<IAffectsRender>(
                (s, h) => s.Invalidated += h,
                (s, h) => s.Invalidated -= h);

        /// <summary>
        /// Gets or sets the stroke thickness.
        /// </summary>
        public double Thickness
        {
            get => GetValue(ThicknessProperty);
            set => SetValue(ThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the style of dashed lines drawn with a <see cref="Pen"/> object.
        /// </summary>
        public IDashStyle? DashStyle
        {
            get => GetValue(DashStyleProperty);
            set => SetValue(DashStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the type of shape to use on both ends of a line.
        /// </summary>
        public PenLineCap LineCap
        {
            get => GetValue(LineCapProperty);
            set => SetValue(LineCapProperty, value);
        }

        /// <summary>
        /// Gets or sets the join style for the ends of two consecutive lines drawn with this
        /// <see cref="Pen"/>.
        /// </summary>
        public PenLineJoin LineJoin
        {
            get => GetValue(LineJoinProperty);
            set => SetValue(LineJoinProperty, value);
        }

        /// <summary>
        /// Gets or sets the limit of the thickness of the join on a mitered corner.
        /// </summary>
        public double MiterLimit
        {
            get => GetValue(MiterLimitProperty);
            set => SetValue(MiterLimitProperty, value);
        }

        /// <summary>
        /// Raised when the pen changes.
        /// </summary>
        public event EventHandler? Invalidated
        {
            add
            {
                _invalidated += value;
                UpdateSubscriptions();
            }
            remove
            {
                _invalidated -= value; 
                UpdateSubscriptions();
            }
        }

        /// <summary>
        /// Creates an immutable clone of the brush.
        /// </summary>
        /// <returns>The immutable clone.</returns>
        public ImmutablePen ToImmutable()
        {
            return new ImmutablePen(
                Brush?.ToImmutable(),
                Thickness,
                DashStyle?.ToImmutable(),
                LineCap,
                LineJoin,
                MiterLimit);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            _invalidated?.Invoke(this, EventArgs.Empty);
            if(change.Property == BrushProperty)
                UpdateSubscription(ref _subscribedToBrush, Brush);
            if(change.Property == DashStyleProperty)
                UpdateSubscription(ref _subscribedToDashes, DashStyle);
            base.OnPropertyChanged(change);
        }

        
        void UpdateSubscription(ref IAffectsRender? field, object? value)
        {
            if ((_invalidated == null || field != value) && field != null)
            {
                if (_weakSubscriber != null)
                    InvalidatedWeakEvent.Unsubscribe(field, _weakSubscriber);
                field = null;
            }

            if (_invalidated != null && field != value && value is IAffectsRender affectsRender)
            {
                if (_weakSubscriber == null)
                {
                    _weakSubscriber = new TargetWeakEventSubscriber<Pen, EventArgs>(
                        this, static (target, _, ev, _) =>
                        {
                            if (ev == InvalidatedWeakEvent)
                                target._invalidated?.Invoke(target, EventArgs.Empty);
                        });
                }

                InvalidatedWeakEvent.Subscribe(affectsRender, _weakSubscriber);
                field = affectsRender;
            }
        }

        void UpdateSubscriptions()
        {
            UpdateSubscription(ref _subscribedToBrush, Brush);
            UpdateSubscription(ref _subscribedToDashes, DashStyle);
        }
    }
}
