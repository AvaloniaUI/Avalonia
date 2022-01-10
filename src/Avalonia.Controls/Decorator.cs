using System;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.VisualTree;

#nullable enable

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for controls which decorate a single child control.
    /// </summary>
    public class Decorator : Control
    {
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly StyledProperty<IControl?> ChildProperty =
            AvaloniaProperty.Register<Decorator, IControl?>(nameof(Child), validate: ValidateChild);

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty =
            AvaloniaProperty.Register<Decorator, Thickness>(nameof(Padding));

        private IControl? _child;

        /// <summary>
        /// Initializes static members of the <see cref="Decorator"/> class.
        /// </summary>
        static Decorator()
        {
            AffectsMeasure<Decorator>(ChildProperty, PaddingProperty);
        }

        /// <summary>
        /// Gets or sets the decorated control.
        /// </summary>
        [Content]
        public IControl? Child
        {
            get => _child;
            set => SetValue(ChildProperty, value);
        }

        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Child"/> control.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        protected override int VisualChildrenCount => Child is null ? 0 : 1;

        protected override event EventHandler? VisualChildrenChanged;

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            return LayoutHelper.MeasureChild(Child, availableSize, Padding);
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            return LayoutHelper.ArrangeChild(Child, finalSize, Padding);
        }

        protected override IVisual GetVisualChild(int index)
        {
            return (index == 0 && _child is not null) ?
                _child : throw new ArgumentOutOfRangeException(nameof(index));
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ChildProperty)
            {
                var oldChild = change.OldValue.GetValueOrDefault<IControl>();
                _child = change.NewValue.GetValueOrDefault<IControl>();

                if (oldChild is not null)
                {
                    ((ISetLogicalParent)oldChild).SetParent(null);
                    LogicalChildren.Clear();
                    RemoveVisualChild(oldChild);
                }

                if (_child is not null)
                {
                    ((ISetLogicalParent)_child).SetParent(this);
                    AddVisualChild(_child);
                    LogicalChildren.Add(_child);
                }

                VisualChildrenChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private static bool ValidateChild(IControl? arg)
        {
            return arg is null || (arg.VisualParent is null && arg.Parent is null);
        }
    }
}
