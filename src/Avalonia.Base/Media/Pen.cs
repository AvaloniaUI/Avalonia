using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Media.Immutable;
using Avalonia.Rendering.Composition;
using Avalonia.Rendering.Composition.Drawing;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    /// <summary>
    /// Describes how a stroke is drawn.
    /// </summary>
    public sealed class Pen : AvaloniaObject, IPen, ICompositionRenderResource<IPen>, ICompositorSerializable
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

        private DashStyle? _subscribedToDashes;
        private TargetWeakEventSubscriber<Pen, EventArgs>? _weakSubscriber;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pen"/> class.
        /// </summary>
        public Pen()
        { }

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
        { }

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

        private static readonly WeakEvent<DashStyle, EventArgs> InvalidatedWeakEvent =
            WeakEvent.Register<DashStyle>(
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
        
        /// <summary>
        /// Smart reuse and update pen properties.
        /// </summary>
        /// <param name="pen">Old pen to modify.</param>
        /// <param name="brush">The brush used to draw.</param>
        /// <param name="thickness">The stroke thickness.</param>
        /// <param name="strokeDashArray">The stroke dask array.</param>
        /// <param name="strokeDaskOffset">The stroke dask offset.</param>
        /// <param name="lineCap">The line cap.</param>
        /// <param name="lineJoin">The line join.</param>
        /// <param name="miterLimit">The miter limit.</param>
        /// <returns>If a new instance was created and visual invalidation required.</returns>
        internal static bool TryModifyOrCreate(ref IPen? pen,
                                             IBrush? brush,
                                             double thickness,
                                             IList<double>? strokeDashArray = null,
                                             double strokeDaskOffset = default,
                                             PenLineCap lineCap = PenLineCap.Flat,
                                             PenLineJoin lineJoin = PenLineJoin.Miter,
                                             double miterLimit = 10.0)
        {
            var previousPen = pen;
            if (brush is null)
            {
                pen = null;
                return previousPen is not null;
            }
            
            IDashStyle? dashStyle = null;
            if (strokeDashArray is { Count: > 0 })
            {
                // strokeDashArray can be IList (instead of AvaloniaList) in future
                // So, if it supports notification - create a mutable DashStyle
                dashStyle = strokeDashArray is INotifyCollectionChanged 
                    ? new DashStyle(strokeDashArray, strokeDaskOffset) 
                    : new ImmutableDashStyle(strokeDashArray, strokeDaskOffset);
            }
            
            if (brush is IImmutableBrush immutableBrush && dashStyle is null or ImmutableDashStyle)
            {
                pen = new ImmutablePen(
                    immutableBrush,
                    thickness,
                    (ImmutableDashStyle?)dashStyle,
                    lineCap,
                    lineJoin,
                    miterLimit);

                return true;
            }

            var mutablePen = previousPen as Pen ?? new Pen();
            mutablePen.Brush = brush;
            mutablePen.Thickness = thickness;
            mutablePen.LineCap = lineCap;
            mutablePen.LineJoin = lineJoin;
            mutablePen.DashStyle = dashStyle;
            mutablePen.MiterLimit = miterLimit;

            pen = mutablePen;
            return !Equals(previousPen, pen);
        }

        void RegisterForSerialization()
        {
            _resource.RegisterForInvalidationOnAllCompositors(this);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            RegisterForSerialization();

            if (change.Property == BrushProperty)
                _resource.ProcessPropertyChangeNotification(change);

            if (change.Property == DashStyleProperty)
                UpdateDashStyleSubscription();
            base.OnPropertyChanged(change);
        }


        void UpdateDashStyleSubscription()
        {
            var newValue = _resource.IsAttached ? DashStyle as DashStyle : null;

            if (ReferenceEquals(_subscribedToDashes, newValue))
                return;

            if (_subscribedToDashes != null && _weakSubscriber != null)
            {
                InvalidatedWeakEvent.Unsubscribe(_subscribedToDashes, _weakSubscriber);
                _subscribedToDashes = null;
            }

            if (newValue != null)
            {
                _weakSubscriber ??= new TargetWeakEventSubscriber<Pen, EventArgs>(
                    this, static (target, _, ev, _) =>
                    {
                        if (ev == InvalidatedWeakEvent)
                            target.RegisterForSerialization();
                    });
                InvalidatedWeakEvent.Subscribe(newValue, _weakSubscriber);
                _subscribedToDashes = newValue;
            }
        }

        private CompositorResourceHolder<ServerCompositionSimplePen> _resource;

        IPen ICompositionRenderResource<IPen>.GetForCompositor(Compositor c) => _resource.GetForCompositor(c);

        void ICompositionRenderResource.AddRefOnCompositor(Compositor c)
        {
            if (_resource.CreateOrAddRef(c, this, out _, static c => new ServerCompositionSimplePen(c.Server)))
            {
                (Brush as ICompositionRenderResource)?.AddRefOnCompositor(c);
                UpdateDashStyleSubscription();
            }
        }

        void ICompositionRenderResource.ReleaseOnCompositor(Compositor c)
        {
            if (_resource.Release(c))
            {
                (Brush as ICompositionRenderResource)?.ReleaseOnCompositor(c);
                UpdateDashStyleSubscription();
            }
        }

        SimpleServerObject? ICompositorSerializable.TryGetServer(Compositor c) => _resource.TryGetForCompositor(c);

        void ICompositorSerializable.SerializeChanges(Compositor c, BatchStreamWriter writer)
        {
            ServerCompositionSimplePen.SerializeAllChanges(writer,
                Brush.GetServer(c), DashStyle?.ToImmutable(), LineCap, LineJoin, MiterLimit, Thickness);
        }
    }
}
