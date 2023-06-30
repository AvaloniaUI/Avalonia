using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Viewbox is used to scale single child to fit in the available space.
    /// </summary>
    public class Viewbox : Control
    {
        private readonly ViewboxContainer _containerVisual;

        /// <summary>
        /// Defines the <see cref="Stretch"/> property.
        /// </summary>
        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<Viewbox, Stretch>(nameof(Stretch), Stretch.Uniform);

        /// <summary>
        /// Defines the <see cref="StretchDirection"/> property.
        /// </summary>
        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            AvaloniaProperty.Register<Viewbox, StretchDirection>(nameof(StretchDirection), StretchDirection.Both);

        /// <summary>
        /// Defines the <see cref="Child"/> property
        /// </summary>
        public static readonly StyledProperty<Control?> ChildProperty =
            Decorator.ChildProperty.AddOwner<Viewbox>();

        static Viewbox()
        {
            ClipToBoundsProperty.OverrideDefaultValue<Viewbox>(true);
            UseLayoutRoundingProperty.OverrideDefaultValue<Viewbox>(true);
            AffectsMeasure<Viewbox>(StretchProperty, StretchDirectionProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Viewbox"/> class.
        /// </summary>
        public Viewbox()
        {
            // The Child control is hosted inside a ViewboxContainer control so that the transform
            // can be applied independently of the Viewbox and Child transforms.
            _containerVisual = new ViewboxContainer();
            _containerVisual.RenderTransformOrigin = RelativePoint.TopLeft;
            ((ISetLogicalParent)_containerVisual).SetParent(this);
            VisualChildren.Add(_containerVisual);
        }

        /// <summary>
        /// Gets or sets the stretch mode, 
        /// which determines how child fits into the available space.
        /// </summary>
        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        /// <summary>
        /// Gets or sets a value controlling in what direction contents will be stretched.
        /// </summary>
        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        /// <summary>
        /// Gets or sets the child of the Viewbox
        /// </summary>
        [Content]
        public Control? Child
        {
            get => GetValue(ChildProperty);
            set => SetValue(ChildProperty, value);
        }

        /// <summary>
        /// Gets or sets the transform applied to the container visual that
        /// hosts the child of the Viewbox
        /// </summary>
        internal ITransform? InternalTransform
        {
            get => _containerVisual.RenderTransform;
            set => _containerVisual.RenderTransform = value;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ChildProperty)
            {
                var (oldChild, newChild) = change.GetOldAndNewValue<Control?>();

                if (oldChild is not null)
                {
                    ((ISetLogicalParent)oldChild).SetParent(null);
                    LogicalChildren.Remove(oldChild);
                }

                _containerVisual.Child = newChild;

                if (newChild is not null)
                {
                    ((ISetLogicalParent)newChild).SetParent(this);
                    LogicalChildren.Add(newChild);
                }

                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            var child = _containerVisual;

            child.Measure(Size.Infinity);

            var childSize = child.DesiredSize;

            var size = Stretch.CalculateSize(availableSize, childSize, StretchDirection);

            return size;
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            var child = _containerVisual;

            var childSize = child.DesiredSize;
            var scale = Stretch.CalculateScaling(finalSize, childSize, StretchDirection);

            InternalTransform = new ImmutableTransform(Matrix.CreateScale(scale.X, scale.Y));

            child.Arrange(new Rect(childSize));

            return childSize * scale;
        }

        /// <summary>
        /// A simple container control which hosts its child as a visual but not logical child.
        /// </summary>
        private class ViewboxContainer : Control
        {
            private Control? _child;

            public Control? Child
            {
                get => _child;
                set
                {
                    if (_child != value)
                    {
                        if (_child is not null)
                            VisualChildren.Remove(_child);

                        _child = value;

                        if (_child is not null)
                            VisualChildren.Add(_child);

                        InvalidateMeasure();
                    }
                }
            }
        }
    }
}
