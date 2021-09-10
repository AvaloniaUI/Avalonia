using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;

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

        /// <summary>
        /// Initializes static members of the <see cref="Panel"/> class.
        /// </summary>
        static Panel()
        {
            AffectsRender<Panel>(BackgroundProperty);
        }

        private EventHandler<ChildIndexChangedEventArgs> _childIndexChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="Panel"/> class.
        /// </summary>
        public Panel()
        {
            Children.CollectionChanged += ChildrenChanged;
        }

        /// <summary>
        /// Gets the children of the <see cref="Panel"/>.
        /// </summary>
        [Content]
        public Controls Children { get; } = new Controls();

        /// <summary>
        /// Gets or Sets Panel background brush.
        /// </summary>
        public IBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        int? IChildIndexProvider.TotalCount => Children.Count;

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
        /// Called when the <see cref="Children"/> collection changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<Control> controls;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    controls = e.NewItems.OfType<Control>().ToList();
                    LogicalChildren.InsertRange(e.NewStartingIndex, controls);
                    VisualChildren.InsertRange(e.NewStartingIndex, e.NewItems.OfType<Visual>());
                    break;

                case NotifyCollectionChangedAction.Move:
                    LogicalChildren.MoveRange(e.OldStartingIndex, e.OldItems.Count, e.NewStartingIndex);
                    VisualChildren.MoveRange(e.OldStartingIndex, e.OldItems.Count, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    controls = e.OldItems.OfType<Control>().ToList();
                    LogicalChildren.RemoveAll(controls);
                    VisualChildren.RemoveAll(e.OldItems.OfType<Visual>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    for (var i = 0; i < e.OldItems.Count; ++i)
                    {
                        var index = i + e.OldStartingIndex;
                        var child = (IControl)e.NewItems[i];
                        LogicalChildren[index] = child;
                        VisualChildren[index] = child;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
            }

            _childIndexChanged?.Invoke(this, new ChildIndexChangedEventArgs());
            InvalidateMeasureOnChildrenChanged();
        }

        private protected virtual void InvalidateMeasureOnChildrenChanged()
        {
            InvalidateMeasure();
        }

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
    }
}
