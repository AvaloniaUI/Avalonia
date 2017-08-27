// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Metadata;
using Avalonia.Collections;
using System.Windows.Markup;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of an polyline or polygon.
    /// </summary>
    [ContentProperty(nameof(Points))]
    public class PolylineGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="Points"/> property.
        /// </summary>
        public static readonly DirectProperty<PolylineGeometry, Points> PointsProperty =
            AvaloniaProperty.RegisterDirect<PolylineGeometry, Points>(nameof(Points), g => g.Points, (g, f) => g.Points = f);

        /// <summary>
        /// Defines the <see cref="IsFilled"/> property.
        /// </summary>
        public static readonly AvaloniaProperty<bool> IsFilledProperty =
            AvaloniaProperty.Register<PolylineGeometry, bool>(nameof(IsFilled));

        private Points _points;
        private bool _isDirty;
        private IDisposable _pointsObserver;

        static PolylineGeometry()
        {
            PointsProperty.Changed.AddClassHandler<PolylineGeometry>((s, e) =>
                s.OnPointsChanged(e.OldValue as Points, e.NewValue as Points));
            IsFilledProperty.Changed.AddClassHandler<PolylineGeometry>((s, _) => s.NotifyChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolylineGeometry"/> class.
        /// </summary>
        public PolylineGeometry()
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = factory.CreateStreamGeometry();

            Points = new Points();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolylineGeometry"/> class.
        /// </summary>
        public PolylineGeometry(IEnumerable<Point> points, bool isFilled) : this()
        {
            Points.AddRange(points);
            IsFilled = isFilled;
        }

        public void PrepareIfNeeded()
        {
            if (_isDirty)
            {
                _isDirty = false;

                using (var context = ((IStreamGeometryImpl)PlatformImpl).Open())
                {
                    var points = Points;
                    var isFilled = IsFilled;
                    if (points.Count > 0)
                    {
                        context.BeginFigure(points[0], isFilled);
                        for (int i = 1; i < points.Count; i++)
                        {
                            context.LineTo(points[i]);
                        }
                        context.EndFigure(isFilled);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the figures.
        /// </summary>
        /// <value>
        /// The points.
        /// </value>
        [Content]
        public Points Points
        {
            get => _points;
            set => SetAndRaise(PointsProperty, ref _points, value);
        }

        public bool IsFilled
        {
            get => GetValue(IsFilledProperty);
            set => SetValue(IsFilledProperty, value);
        }

        public override IGeometryImpl PlatformImpl
        {
            get
            {
                PrepareIfNeeded();
                return base.PlatformImpl;
            }
            protected set => base.PlatformImpl = value;
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            PrepareIfNeeded();
            return new PolylineGeometry(Points, IsFilled);
        }

        private void OnPointsChanged(Points oldValue, Points newValue)
        {
            _pointsObserver?.Dispose();

            _pointsObserver = newValue?.ForEachItem(f => NotifyChanged(), f => NotifyChanged(), () => NotifyChanged());
        }

        internal void NotifyChanged()
        {
            _isDirty = true;
        }
    }
}
