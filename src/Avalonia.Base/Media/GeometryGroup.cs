using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents a composite geometry, composed of other <see cref="Geometry"/> objects.
    /// </summary>
    public class GeometryGroup : Geometry
    {
        public static readonly DirectProperty<GeometryGroup, GeometryCollection> ChildrenProperty =
            AvaloniaProperty.RegisterDirect<GeometryGroup, GeometryCollection> (
                nameof(Children),
                o => o.Children,
                (o, v)=> o.Children = v);

        public static readonly StyledProperty<FillRule> FillRuleProperty =
            AvaloniaProperty.Register<GeometryGroup, FillRule>(nameof(FillRule));

        private GeometryCollection _children;

        public GeometryGroup()
        {
            _children = new GeometryCollection
            {
                Parent = this
            };
        }

        /// <summary>
        /// Gets or sets the collection that contains the child geometries.
        /// </summary>
        [Content]
        public GeometryCollection Children
        {
            get => _children;
            set
            {
                OnChildrenChanged(_children, value);
                SetAndRaise(ChildrenProperty, ref _children, value);             
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

            if (_children.Count > 0)
            {
                result.Children = new GeometryCollection(_children);
            }
              
            return result;
        }

        protected void OnChildrenChanged(GeometryCollection oldChildren, GeometryCollection newChildren)
        {
            oldChildren.Parent = null;

            newChildren.Parent = this;
        }

        private protected sealed override IGeometryImpl? CreateDefiningGeometry()
        {
            if (_children.Count > 0)
            {
                var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();

                var children = new IGeometryImpl?[_children.Count];
                for (var c = 0; c < _children.Count; c++)
                    children[c] = _children[c].PlatformImpl;
                return factory.CreateGeometryGroup(FillRule, children!);
            }

            return null;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            switch (change.Property.Name)
            {
                case nameof(FillRule):                   
                case nameof(Children):
                    InvalidateGeometry();
                    break;
            }
        }

        internal void Invalidate()
        {
            InvalidateGeometry();
        }
    }
}
