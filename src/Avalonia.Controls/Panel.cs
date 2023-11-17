using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for controls that can contain multiple children.
    /// </summary>
    /// <remarks>
    /// Controls can be added to a <see cref="Panel"/> by adding them to its <see cref="Children"/>
    /// collection. All children are layed out to fill the panel.
    /// </remarks>
    public class Panel : Control, IChildIndexProvider
    {
        /// <summary>
        /// Defines the <see cref="Background"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> BackgroundProperty =
            Border.BackgroundProperty.AddOwner<Panel>();

        /// <summary>
        /// Initializes static members of the <see cref="Panel"/> class.
        /// </summary>
        static Panel()
        {
            AffectsRender<Panel>(BackgroundProperty);
        }

        private EventHandler<ChildIndexChangedEventArgs>? _childIndexChanged;

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
        public IBrush? Background
        {
            get => GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Gets whether the <see cref="Panel"/> hosts the items created by an <see cref="ItemsPresenter"/>.
        /// </summary>
        public bool IsItemsHost { get; internal set; }

        event EventHandler<ChildIndexChangedEventArgs>? IChildIndexProvider.ChildIndexChanged
        {
            add
            {
                if (_childIndexChanged is null)
                    Children.PropertyChanged += ChildrenPropertyChanged;
                _childIndexChanged += value;
            }

            remove
            {
                _childIndexChanged -= value;
                if (_childIndexChanged is null)
                    Children.PropertyChanged -= ChildrenPropertyChanged;
            }
        }

        /// <summary>
        /// Renders the visual to a <see cref="DrawingContext"/>.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        public sealed override void Render(DrawingContext context)
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
            where TPanel : Panel
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => AffectsParentArrangeInvalidate<TPanel>(e));
            foreach (var property in properties)
            {
                property.Changed.Subscribe(invalidateObserver);
            }
        }

        /// <summary>
        /// Marks a property on a child as affecting the parent panel's measurement.
        /// </summary>
        /// <param name="properties">The properties.</param>
        protected static void AffectsParentMeasure<TPanel>(params AvaloniaProperty[] properties)
            where TPanel : Panel
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(
                static e => AffectsParentMeasureInvalidate<TPanel>(e));
            foreach (var property in properties)
            {
                property.Changed.Subscribe(invalidateObserver);
            }
        }

        /// <summary>
        /// Called when the <see cref="Children"/> collection changes.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (!IsItemsHost)
                    {
                        LogicalChildren.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<Control>().ToList());
                    }
                    VisualChildren.InsertRange(e.NewStartingIndex, e.NewItems!.OfType<Visual>());
                    break;

                case NotifyCollectionChangedAction.Move:
                    if (!IsItemsHost)
                    {
                        LogicalChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                    }
                    VisualChildren.MoveRange(e.OldStartingIndex, e.OldItems!.Count, e.NewStartingIndex);
                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (!IsItemsHost)
                    {
                        LogicalChildren.RemoveAll(e.OldItems!.OfType<Control>().ToList());
                    }
                    VisualChildren.RemoveAll(e.OldItems!.OfType<Visual>());
                    break;

                case NotifyCollectionChangedAction.Replace:
                    for (var i = 0; i < e.OldItems!.Count; ++i)
                    {
                        var index = i + e.OldStartingIndex;
                        var child = (Control)e.NewItems![i]!;
                        if (!IsItemsHost)
                        {
                            LogicalChildren[index] = child;
                        }
                        VisualChildren[index] = child;
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    throw new NotSupportedException();
            }

            _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.ChildIndexesReset);
            InvalidateMeasureOnChildrenChanged();
        }

        private protected virtual void InvalidateMeasureOnChildrenChanged()
        {
            InvalidateMeasure();
        }

        private void ChildrenPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Children.Count) || e.PropertyName is null)
                _childIndexChanged?.Invoke(this, ChildIndexChangedEventArgs.TotalCountChanged);
        }

        private static void AffectsParentArrangeInvalidate<TPanel>(AvaloniaPropertyChangedEventArgs e)
            where TPanel : Panel
        {
            var control = e.Sender as Control;
            var panel = control?.VisualParent as TPanel;
            panel?.InvalidateArrange();
        }

        private static void AffectsParentMeasureInvalidate<TPanel>(AvaloniaPropertyChangedEventArgs e)
            where TPanel : Panel
        {
            var control = e.Sender as Control;
            var panel = control?.VisualParent as TPanel;
            panel?.InvalidateMeasure();
        }

        int IChildIndexProvider.GetChildIndex(ILogical child)
        {
            return child is Control control ? Children.IndexOf(control) : -1;
        }

        /// <inheritdoc />
        bool IChildIndexProvider.TryGetTotalCount(out int count)
        {
            count = Children.Count;
            return true;
        }
    }
}
