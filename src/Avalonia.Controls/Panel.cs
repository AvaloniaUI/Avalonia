using System;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for controls that can contain multiple children.
    /// </summary>
    /// <remarks>
    /// Controls can be added to a <see cref="Panel"/> by adding them to its <see cref="Children"/>
    /// collection. All children are layed out to fill the panel.
    /// </remarks>
    public class Panel : Control, IPanel, IChildIndexProvider
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<Panel>();

        private PanelChildren _children;

        /// <summary>
        /// Initializes static members of the <see cref="Panel"/> class.
        /// </summary>
        static Panel()
        {
            AffectsRender<Panel>(BackgroundProperty);
        }

        private EventHandler<ChildIndexChangedEventArgs> _childIndexChanged;

        /// <summary>
        /// Gets the children of the <see cref="Panel"/>.
        /// </summary>
        [Content]
        public Controls Children => _children ??= new(this);

        /// <summary>
        /// Gets or Sets Panel background brush.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        protected override int LogicalChildrenCount => Children.Count;
        protected override int VisualChildrenCount => Children.Count;

        protected override event EventHandler LogicalChildrenChanged;
        
        protected override event EventHandler VisualChildrenChanged
        {
            add => LogicalChildrenChanged += value;
            remove => LogicalChildrenChanged -= value;
        }

        event EventHandler<ChildIndexChangedEventArgs> IChildIndexProvider.ChildIndexChanged
        {
            add => _childIndexChanged += value;
            remove => _childIndexChanged -= value;
        }

        /// <summary>
        /// Renders the visual to a <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public override void Render(DrawingContext context)
        {
            var background = Background;
            if (background != null)
            {
                var renderSize = Bounds.Size;
                context.FillRectangle(background, new Rect(renderSize));
            }

            base.Render(context);
        }

        /// <summary>
        /// Marks a property on a child as affecting the parent panel's arrangement.
        /// </summary>
        /// <param name="properties">The properties.</param>
        protected static void AffectsParentArrange<TPanel>(params AvaloniaProperty[] properties)
            where TPanel : class, IPanel
        {
            foreach (var property in properties)
            {
                property.Changed.Subscribe(AffectsParentArrangeInvalidate<TPanel>);
            }
        }

        /// <summary>
        /// Marks a property on a child as affecting the parent panel's measurement.
        /// </summary>
        /// <param name="properties">The properties.</param>
        protected static void AffectsParentMeasure<TPanel>(params AvaloniaProperty[] properties)
            where TPanel : class, IPanel
        {
            foreach (var property in properties)
            {
                property.Changed.Subscribe(AffectsParentMeasureInvalidate<TPanel>);
            }
        }

        /// <summary>
        /// Called in response to the <see cref="Children"/> collection changing in order to allow
        /// the panel to carry out any needed invalidation.
        /// </summary>
        protected internal virtual void InvalidateDueToChildrenChange()
        {
            OnChildIndexChanged();
            InvalidateMeasure();
            VisualRoot?.Renderer?.RecalculateChildren(this);
        }

        protected void OnChildIndexChanged(ILogical changed = null)
        {
            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs(changed));
        }

        protected override ILogical GetLogicalChild(int index) => Children[index];
        protected override IVisual GetVisualChild(int index) => Children[index];

        internal new void AddVisualChild(IVisual child) => base.AddVisualChild(child);
        internal new void RemoveVisualChild(IVisual child) => base.RemoveVisualChild(child);

        private static void AffectsParentArrangeInvalidate<TPanel>(AvaloniaPropertyChangedEventArgs e)
            where TPanel : class, IPanel
        {
            var control = e.Sender as IControl;
            var panel = control?.VisualParent as TPanel;
            panel?.InvalidateArrange();
        }

        private static void AffectsParentMeasureInvalidate<TPanel>(AvaloniaPropertyChangedEventArgs e)
            where TPanel : class, IPanel
        {
            var control = e.Sender as IControl;
            var panel = control?.VisualParent as TPanel;
            panel?.InvalidateMeasure();
        }

        int IChildIndexProvider.GetChildIndex(ILogical child)
        {
            return child is IControl control ? Children.IndexOf(control) : -1;
        }

        public bool TryGetTotalCount(out int count)
        {
            count = Children.Count;
            return true;
        }
    }
}
