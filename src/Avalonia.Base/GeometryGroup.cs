using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Metadata;
using Avalonia.Platform;

#nullable enable

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a composite geometry, composed of other <see cref="Geometry"/> objects.
    /// </summary>
    public class GeometryGroup : Geometry
    {
        public static readonly DirectProperty<GeometryGroup, GeometryCollection?> ChildrenProperty =
            AvaloniaProperty.RegisterDirect<GeometryGroup, GeometryCollection?> (
                nameof(Children),
                o => o.Children,
                (o, v) => o.Children = v);

        public static readonly StyledProperty<FillRule> FillRuleProperty =
            AvaloniaProperty.Register<GeometryGroup, FillRule>(nameof(FillRule));

        private GeometryCollection? _children;
        private bool _childrenSet;

        /// <summary>
        /// Gets or sets the collection that contains the child geometries.
        /// </summary>
        [Content]
        public GeometryCollection? Children
        {
            get => _children ??= (!_childrenSet ? new GeometryCollection() : null);
            set
            {
                SetAndRaise(ChildrenProperty, ref _children, value);
                _childrenSet = true;
            }
        }

        /// <summary>
        /// Gets or sets how the intersecting areas of the objects contained in this
        /// <see cref="GeometryGroup"/> are combined. The default is <see cref="FillRule.EvenOdd"/>.
        /// </summary>
        public FillRule FillRule
        {
            get => GetValue(FillRuleProperty);
            set => SetValue(FillRuleProperty, value);
        }

        public override Geometry Clone()
        {
            var result = new GeometryGroup { FillRule = FillRule, Transform = Transform };
            if (_children?.Count > 0)
                result.Children = new GeometryCollection(_children);
            return result;
        }

        protected override IGeometryImpl? CreateDefiningGeometry()
        {
            if (_children?.Count > 0)
            {
                var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
                return factory.CreateGeometryGroup(FillRule, _children);
            }

            return null;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ChildrenProperty || change.Property == FillRuleProperty)
            {
                InvalidateGeometry();
            }
        }
    }
}
