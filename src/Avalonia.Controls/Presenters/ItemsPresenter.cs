using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Presents items inside an <see cref="Avalonia.Controls.ItemsControl"/>.
    /// </summary>
    public class ItemsPresenter : Control, ILogicalScrollable, IScrollSnapPointsInfo
    {
        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<Panel?>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<ItemsPresenter>();

        private PanelContainerGenerator? _generator;
        private ILogicalScrollable? _logicalScrollable;
        private IScrollSnapPointsInfo? _scrollSnapPointsInfo;
        private EventHandler? _scrollInvalidated;

        /// <summary>
        /// Defines the <see cref="AreHorizontalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreHorizontalSnapPointsRegularProperty =
            AvaloniaProperty.Register<ItemsPresenter, bool>(nameof(AreHorizontalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="AreVerticalSnapPointsRegular"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AreVerticalSnapPointsRegularProperty =
            AvaloniaProperty.Register<ItemsPresenter, bool>(nameof(AreVerticalSnapPointsRegular));

        /// <summary>
        /// Defines the <see cref="HorizontalSnapPointsChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> HorizontalSnapPointsChangedEvent =
            RoutedEvent.Register<ItemsPresenter, RoutedEventArgs>(
                nameof(HorizontalSnapPointsChanged),
                RoutingStrategies.Bubble);

        /// <summary>
        /// Defines the <see cref="VerticalSnapPointsChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> VerticalSnapPointsChangedEvent =
            RoutedEvent.Register<ItemsPresenter, RoutedEventArgs>(
                nameof(VerticalSnapPointsChanged),
                RoutingStrategies.Bubble);

        event EventHandler? ILogicalScrollable.ScrollInvalidated
        {
            add => _scrollInvalidated += value;
            remove => _scrollInvalidated -= value;
        }

        /// <summary>
        /// Gets or sets a template which creates the <see cref="Panel"/> used to display the items.
        /// </summary>
        public ITemplate<Panel?> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        /// <summary>
        /// Gets the panel used to display the items.
        /// </summary>
        public Panel? Panel { get; private set; }

        /// <summary>
        /// Gets the owner <see cref="ItemsControl"/>.
        /// </summary>
        internal ItemsControl? ItemsControl { get; private set; }
        
        bool ILogicalScrollable.CanHorizontallyScroll 
        {
            get => _logicalScrollable?.CanHorizontallyScroll ?? false;
            set
            {
                if (_logicalScrollable is not null)
                    _logicalScrollable.CanHorizontallyScroll = value;
            }
        }

        bool ILogicalScrollable.CanVerticallyScroll 
        {
            get => _logicalScrollable?.CanVerticallyScroll ?? false;
            set
            {
                if (_logicalScrollable is not null)
                    _logicalScrollable.CanVerticallyScroll = value;
            }
        }

        Vector IScrollable.Offset 
        {
            get => _logicalScrollable?.Offset ?? default;
            set
            {
                if (_logicalScrollable is not null)
                    _logicalScrollable.Offset = value;
            }
        }

        /// <summary>
        /// Occurs when the measurements for horizontal snap points change.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged
        {
            add => AddHandler(HorizontalSnapPointsChangedEvent, value);
            remove => RemoveHandler(HorizontalSnapPointsChangedEvent, value);
        }

        /// <summary>
        /// Occurs when the measurements for vertical snap points change.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged
        {
            add => AddHandler(VerticalSnapPointsChangedEvent, value);
            remove => RemoveHandler(VerticalSnapPointsChangedEvent, value);
        }

        bool ILogicalScrollable.IsLogicalScrollEnabled => _logicalScrollable?.IsLogicalScrollEnabled ?? false;
        Size ILogicalScrollable.ScrollSize => _logicalScrollable?.ScrollSize ?? default;
        Size ILogicalScrollable.PageScrollSize => _logicalScrollable?.PageScrollSize ?? default;
        Size IScrollable.Extent => _logicalScrollable?.Extent ?? default;
        Size IScrollable.Viewport => _logicalScrollable?.Viewport ?? default;

        /// <summary>
        /// Gets or sets whether the horizontal snap points for the <see cref="ItemsPresenter"/> are equidistant from each other.
        /// </summary>
        public bool AreHorizontalSnapPointsRegular
        {
            get { return GetValue(AreHorizontalSnapPointsRegularProperty); }
            set { SetValue(AreHorizontalSnapPointsRegularProperty, value); }
        }

        /// <summary>
        /// Gets or sets whether the vertical snap points for the <see cref="ItemsPresenter"/> are equidistant from each other.
        /// </summary>
        public bool AreVerticalSnapPointsRegular
        {
            get { return GetValue(AreVerticalSnapPointsRegularProperty); }
            set { SetValue(AreVerticalSnapPointsRegularProperty, value); }
        }

        public override sealed void ApplyTemplate()
        {
            if (Panel is null && ItemsControl is not null)
            {
                if (_logicalScrollable is not null)
                {
                    _logicalScrollable.ScrollInvalidated -= OnLogicalScrollInvalidated;
                }

                Panel = ItemsPanel.Build();

                if (Panel is null)
                {
                    return;
                }

                Panel.TemplatedParent = TemplatedParent;
                Panel.IsItemsHost = true;
                _scrollSnapPointsInfo = Panel as IScrollSnapPointsInfo;
                LogicalChildren.Add(Panel);
                VisualChildren.Add(Panel);

                if (_scrollSnapPointsInfo != null)
                {
                    _scrollSnapPointsInfo.AreVerticalSnapPointsRegular = AreVerticalSnapPointsRegular;
                    _scrollSnapPointsInfo.AreHorizontalSnapPointsRegular = AreHorizontalSnapPointsRegular;
                }

                if (Panel is VirtualizingPanel v)
                    v.Attach(ItemsControl);
                else
                    CreateSimplePanelGenerator();

                if (Panel is IScrollSnapPointsInfo scrollSnapPointsInfo)
                {
                    scrollSnapPointsInfo.VerticalSnapPointsChanged += (s, e) =>
                    {
                        e.RoutedEvent = VerticalSnapPointsChangedEvent;
                        RaiseEvent(e);
                    };

                    scrollSnapPointsInfo.HorizontalSnapPointsChanged += (s, e) =>
                    {
                        e.RoutedEvent = HorizontalSnapPointsChangedEvent;
                        RaiseEvent(e);
                    };
                }

                _logicalScrollable = Panel as ILogicalScrollable;

                if (_logicalScrollable is not null)
                {
                    _logicalScrollable.ScrollInvalidated += OnLogicalScrollInvalidated;
                }
            }
        }

        bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect) =>
            _logicalScrollable?.BringIntoView(target, targetRect) ?? false;
        Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from) =>
            _logicalScrollable?.GetControlInDirection(direction, from);
        void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e) => _scrollInvalidated?.Invoke(this, e);

        internal void ScrollIntoView(int index)
        {
            if (Panel is VirtualizingPanel v)
                v.ScrollIntoView(index);
            else if (index >= 0 && index < Panel?.Children.Count)
                Panel.Children[index].BringIntoView();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TemplatedParentProperty)
            {
                ResetState();
                ItemsControl = null;

                if (change.NewValue is ItemsControl itemsControl)
                {
                    ItemsControl = itemsControl;
                    ItemsControl.RegisterItemsPresenter(this);
                }
            }
            else if (change.Property == ItemsPanelProperty)
            {
                ResetState();
                InvalidateMeasure();
            }
            else if(change.Property == AreHorizontalSnapPointsRegularProperty)
            {
                if (_scrollSnapPointsInfo != null)
                    _scrollSnapPointsInfo.AreHorizontalSnapPointsRegular = AreHorizontalSnapPointsRegular;
            }
            else if (change.Property == AreVerticalSnapPointsRegularProperty)
            {
                if (_scrollSnapPointsInfo != null)
                    _scrollSnapPointsInfo.AreVerticalSnapPointsRegular = AreVerticalSnapPointsRegular;
            }
        }

        internal void Refresh()
        {
            if (Panel is VirtualizingPanel v)
                v.Refresh();
            else
                _generator?.Refresh();
        }

        private void ResetState()
        {
            _generator?.Dispose();
            _generator = null;
            LogicalChildren.Clear();
            VisualChildren.Clear();
            (Panel as VirtualizingPanel)?.Detach();
            Panel = null;
        }

        private void CreateSimplePanelGenerator()
        {
            Debug.Assert(Panel is not VirtualizingPanel);

            if (ItemsControl is null || Panel is null)
                return;

            _generator?.Dispose();
            _generator = new(this);
        }

        internal Control? ContainerFromIndex(int index)
        {
            if (Panel is VirtualizingPanel v)
                return v.ContainerFromIndex(index);
            return index >= 0 && index < Panel?.Children.Count ? Panel.Children[index] : null;
        }

        internal IEnumerable<Control>? GetRealizedContainers()
        {
            if (Panel is VirtualizingPanel v)
                return v.GetRealizedContainers();
            return Panel?.Children;
        }

        internal int IndexFromContainer(Control container)
        {
            if (Panel is VirtualizingPanel v)
                return v.IndexFromContainer(container);
            return Panel?.Children.IndexOf(container) ?? -1;
        }

        private void OnLogicalScrollInvalidated(object? sender, EventArgs e) => _scrollInvalidated?.Invoke(this, e);

        public IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
        {
            if(Panel is IScrollSnapPointsInfo scrollSnapPointsInfo)
            {
                return scrollSnapPointsInfo.GetIrregularSnapPoints(orientation, snapPointsAlignment);
            }

            return new List<double>();
        }

        public double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
        {
            if (Panel is IScrollSnapPointsInfo scrollSnapPointsInfo)
            {
                return scrollSnapPointsInfo.GetRegularSnapPoints(orientation, snapPointsAlignment, out offset);
            }

            offset = 0;

            return 0;
        }
    }
}
