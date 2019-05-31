using System;
using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace Avalonia.Controls.Repeaters
{
    public class ItemsRepeater : Panel
    {
        public static readonly AvaloniaProperty<double> HorizontalCacheLengthProperty =
            AvaloniaProperty.Register<ItemsRepeater, double>(nameof(HorizontalCacheLength), 2.0);
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            ItemsControl.ItemTemplateProperty.AddOwner<ItemsRepeater>();
        public static readonly DirectProperty<ItemsRepeater, IEnumerable> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<ItemsRepeater>(o => o.Items, (o, v) => o.Items = v);
        public static readonly AvaloniaProperty<Layout> LayoutProperty =
            AvaloniaProperty.Register<ItemsRepeater, Layout>(nameof(Layout), new StackLayout());
        public static readonly AvaloniaProperty<double> VerticalCacheLengthProperty =
            AvaloniaProperty.Register<ItemsRepeater, double>(nameof(VerticalCacheLength), 2.0);
        private static readonly AttachedProperty<VirtualizationInfo> VirtualizationInfoProperty =
            AvaloniaProperty.RegisterAttached<ItemsRepeater, IControl, VirtualizationInfo>("VirtualizationInfo");

        internal static readonly Rect InvalidRect = new Rect(-1, -1, -1, -1);
        internal static readonly Point ClearedElementsArrangePosition = new Point(-10000.0, -10000.0);

        private readonly ViewManager _viewManager;
        private readonly ViewportManager _viewportManager;
        private IEnumerable _items;
        private VirtualizingLayoutContext _layoutContext;
        private NotifyCollectionChangedEventArgs _processingItemsSourceChange;
        private Size _lastAvailableSize;
        private bool _isLayoutInProgress;
        private ItemsRepeaterElementPreparedEventArgs _elementPreparedArgs;
        private ItemsRepeaterElementClearingEventArgs _elementClearingArgs;
        private ItemsRepeaterElementIndexChangedEventArgs _elementIndexChangedArgs;

        public ItemsRepeater()
        {
            _viewManager = new ViewManager(this);
            _viewportManager = new ViewportManager(this);
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Once);
            OnLayoutChanged(null, Layout);
        }

        static ItemsRepeater()
        {
            ClipToBoundsProperty.OverrideDefaultValue<ItemsRepeater>(true);
        }

        public Layout Layout
        {
            get => GetValue(LayoutProperty);
            set => SetValue(LayoutProperty, value);
        }

        public IEnumerable Items
        {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public double HorizontalCacheLength
        {
            get => GetValue(HorizontalCacheLengthProperty);
            set => SetValue(HorizontalCacheLengthProperty, value);
        }

        public double VerticalCacheLength
        {
            get => GetValue(VerticalCacheLengthProperty);
            set => SetValue(VerticalCacheLengthProperty, value);
        }

        public ItemsSourceView ItemsSourceView { get; private set; }

        internal ItemTemplateWrapper ItemTemplateShim { get; set; }
        internal Point LayoutOrigin { get; set; }
        internal object LayoutState { get; set; }
        internal IControl MadeAnchor => _viewportManager.MadeAnchor;
        internal Rect RealizationWindow => _viewportManager.GetLayoutRealizationWindow();
        internal IControl SuggestedAnchor => _viewportManager.SuggestedAnchor;

        private bool IsProcessingCollectionChange => _processingItemsSourceChange != null;

        private LayoutContext LayoutContext
        {
            get
            {
                if (_layoutContext == null)
                {
                    _layoutContext = new RepeaterLayoutContext(this);
                }

                return _layoutContext;
            }
        }

        public event EventHandler<ItemsRepeaterElementClearingEventArgs> ElementClearing;
        public event EventHandler<ItemsRepeaterElementIndexChangedEventArgs> ElementIndexChanged;
        public event EventHandler<ItemsRepeaterElementPreparedEventArgs> ElementPrepared;

        public int GetElementIndex(IControl element) => GetElementIndexImpl(element);

        public IControl TryGetElement(int index) => GetElementFromIndexImpl(index);

        public void PinElement(IControl element) => _viewManager.UpdatePin(element, true);

        public void UnpinElement(IControl element) => _viewManager.UpdatePin(element, false);

        public IControl GetOrCreateElement(int index) => GetOrCreateElementImpl(index);

        internal static VirtualizationInfo TryGetVirtualizationInfo(IControl element)
        {
            var value = element.GetValue(VirtualizationInfoProperty);
            return value;
        }

        internal static VirtualizationInfo CreateAndInitializeVirtualizationInfo(IControl element)
        {
            if (TryGetVirtualizationInfo(element) != null)
            {
                throw new InvalidOperationException("VirtualizationInfo already created.");
            }

            var result = new VirtualizationInfo();
            element.SetValue(VirtualizationInfoProperty, result);
            return result;
        }

        internal static VirtualizationInfo GetVirtualizationInfo(IControl element)
        {
            var result = element.GetValue(VirtualizationInfoProperty);

            if (result == null)
            {
                result = new VirtualizationInfo();
                element.SetValue(VirtualizationInfoProperty, result);
            }

            return result;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (_isLayoutInProgress)
            {
                throw new AvaloniaInternalException("Reentrancy detected during layout.");
            }

            if (IsProcessingCollectionChange)
            {
                throw new NotSupportedException("Cannot run layout in the middle of a collection change.");
            }

            _viewportManager.OnOwnerMeasuring();

            _isLayoutInProgress = true;

            try
            {
                _viewManager.PrunePinnedElements();
                var extent = new Rect();
                var desiredSize = new Size();
                var layout = Layout;

                if (layout != null)
                {
                    var layoutContext = GetLayoutContext();

                    desiredSize = layout.Measure(layoutContext, availableSize);
                    extent = new Rect(LayoutOrigin.X, LayoutOrigin.Y, desiredSize.Width, desiredSize.Height);

                    // Clear auto recycle candidate elements that have not been kept alive by layout - i.e layout did not
                    // call GetElementAt(index).
                    foreach (var element in Children)
                    {
                        var virtInfo = GetVirtualizationInfo(element);

                        if (virtInfo.Owner == ElementOwner.Layout &&
                            virtInfo.AutoRecycleCandidate &&
                            !virtInfo.KeepAlive)
                        {
                            ClearElementImpl(element);
                        }
                    }
                }

                _viewportManager.SetLayoutExtent(extent);
                _lastAvailableSize = availableSize;
                return desiredSize;
            }
            finally
            {
                _isLayoutInProgress = false;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_isLayoutInProgress)
            {
                throw new AvaloniaInternalException("Reentrancy detected during layout.");
            }

            if (IsProcessingCollectionChange)
            {
                throw new NotSupportedException("Cannot run layout in the middle of a collection change.");
            }

            _isLayoutInProgress = true;

            try
            {
                var arrangeSize = Layout?.Arrange(GetLayoutContext(), finalSize) ?? default;

                // The view manager might clear elements during this call.
                // That's why we call it before arranging cleared elements
                // off screen.
                _viewManager.OnOwnerArranged();

                foreach (var element in Children)
                {
                    var virtInfo = GetVirtualizationInfo(element);
                    virtInfo.KeepAlive = false;

                    if (virtInfo.Owner == ElementOwner.ElementFactory ||
                        virtInfo.Owner == ElementOwner.PinnedPool)
                    {
                        // Toss it away. And arrange it with size 0 so that XYFocus won't use it.
                        element.Arrange(new Rect(
                            ClearedElementsArrangePosition.X - element.DesiredSize.Width,
                            ClearedElementsArrangePosition.Y - element.DesiredSize.Height,
                            0,
                            0));
                    }
                    else
                    {
                        var newBounds = element.Bounds;

                        //if (virtInfo.ArrangeBounds != ItemsRepeater.InvalidRect &&
                        //    newBounds != virtInfo.ArrangeBounds)
                        //{
                        //    _animationManager.OnElementBoundsChanged(element, virtInfo.ArrangeBounds, newBounds);
                        //}

                        virtInfo.ArrangeBounds = newBounds;
                    }
                }

                _viewportManager.OnOwnerArranged();
                //_animationManager.OnOwnerArranged();

                return arrangeSize;
            }
            finally
            {
                _isLayoutInProgress = false;
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            InvalidateMeasure();
            _viewportManager.ResetScrollers();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _viewportManager.ResetScrollers();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs args)
        {
            var property = args.Property;

            if (property == ItemsProperty)
            {
                var newValue = (IEnumerable)args.NewValue;
                var newDataSource = newValue as ItemsSourceView;
                if (newValue != null && newDataSource == null)
                {
                    newDataSource = new ItemsSourceView(newValue);
                }

                OnDataSourcePropertyChanged(ItemsSourceView, newDataSource);
            }
            else if (property == ItemTemplateProperty)
            {
                OnItemTemplateChanged((IDataTemplate)args.OldValue, (IDataTemplate)args.NewValue);
            }
            else if (property == LayoutProperty)
            {
                OnLayoutChanged((Layout)args.OldValue, (Layout)args.NewValue);
            }
            //else if (property == AnimatorProperty)
            //{
            //    OnAnimatorChanged((ElementAnimator)args.OldValue, (ElementAnimator)args.NewValue);
            //}
            else if (property == HorizontalCacheLengthProperty)
            {
                _viewportManager.HorizontalCacheLength = (double)args.NewValue;
            }
            else if (property == VerticalCacheLengthProperty)
            {
                _viewportManager.VerticalCacheLength = (double)args.NewValue;
            }
            else
            {
                base.OnPropertyChanged(args);
            }
        }

        internal IControl GetElementImpl(int index, bool forceCreate, bool supressAutoRecycle)
        {
            var element = _viewManager.GetElement(index, forceCreate, supressAutoRecycle);
            return element;
        }

        internal void ClearElementImpl(IControl element)
        {
            // Clearing an element due to a collection change
            // is more strict in that pinned elements will be forcibly
            // unpinned and sent back to the view generator.
            var isClearedDueToCollectionChange =
                _processingItemsSourceChange != null &&
                (_processingItemsSourceChange.Action == NotifyCollectionChangedAction.Remove ||
                    _processingItemsSourceChange.Action == NotifyCollectionChangedAction.Replace ||
                    _processingItemsSourceChange.Action == NotifyCollectionChangedAction.Reset);

            _viewManager.ClearElement(element, isClearedDueToCollectionChange);
            _viewportManager.OnElementCleared(element);
        }

        private int GetElementIndexImpl(IControl element)
        {
            var virtInfo = TryGetVirtualizationInfo(element);
            return _viewManager.GetElementIndex(virtInfo);
        }

        private IControl GetElementFromIndexImpl(int index)
        {
            IControl result = null;

            var children = Children;
            for (var i = 0; i < children.Count && result == null; ++i)
            {
                var element = children[i];
                var virtInfo = TryGetVirtualizationInfo(element);
                if (virtInfo?.IsRealized == true && virtInfo.Index == index)
                {
                    result = element;
                }
            }

            return result;
        }

        private IControl GetOrCreateElementImpl(int index)
        {
            if (index >= 0 && index >= ItemsSourceView.Count)
            {
                throw new ArgumentException("Argument index is invalid.", "index");
            }

            if (_isLayoutInProgress)
            {
                throw new NotSupportedException("GetOrCreateElement invocation is not allowed during layout.");
            }

            var element = GetElementFromIndexImpl(index);
            bool isAnchorOutsideRealizedRange = element == null;

            if (isAnchorOutsideRealizedRange)
            {
                if (Layout == null)
                {
                    throw new InvalidOperationException("Cannot make an Anchor when there is no attached layout.");
                }

                element = GetLayoutContext().GetOrCreateElementAt(index);
                element.Measure(Size.Infinity);
            }

            _viewportManager.OnMakeAnchor(element, isAnchorOutsideRealizedRange);
            InvalidateMeasure();

            return element;
        }

        internal void OnElementPrepared(IControl element, int index)
        {
            _viewportManager.OnElementPrepared(element);
            if (ElementPrepared != null)
            {
                if (_elementPreparedArgs == null)
                {
                    _elementPreparedArgs = new ItemsRepeaterElementPreparedEventArgs(element, index);
                }
                else
                {
                    _elementPreparedArgs.Update(element, index);
                }

                ElementPrepared(this, _elementPreparedArgs);
            }
        }

        internal void OnElementClearing(IControl element)
        {
            if (ElementClearing != null)
            {
                if (_elementClearingArgs == null)
                {
                    _elementClearingArgs = new ItemsRepeaterElementClearingEventArgs(element);
                }
                else
                {
                    _elementClearingArgs.Update(element);
                }

                ElementClearing(this, _elementClearingArgs);
            }
        }

        internal void OnElementIndexChanged(IControl element, int oldIndex, int newIndex)
        {
            if (ElementIndexChanged != null)
            {
                if (_elementIndexChangedArgs == null)
                {
                    _elementIndexChangedArgs = new ItemsRepeaterElementIndexChangedEventArgs(element, oldIndex, newIndex);
                }
                else
                {
                    _elementIndexChangedArgs.Update(element, oldIndex, newIndex);
                }

                ElementIndexChanged(this, _elementIndexChangedArgs);
            }
        }

        private void OnDataSourcePropertyChanged(ItemsSourceView oldValue, ItemsSourceView newValue)
        {
            if (_isLayoutInProgress)
            {
                throw new AvaloniaInternalException("Cannot set ItemsSourceView during layout.");
            }

            ItemsSourceView?.Dispose();
            ItemsSourceView = newValue;

            if (oldValue != null)
            {
                oldValue.CollectionChanged -= OnItemsSourceViewChanged;
            }

            if (newValue != null)
            {
                newValue.CollectionChanged += OnItemsSourceViewChanged;
            }

            if (Layout != null)
            {
                if (Layout is VirtualizingLayout virtualLayout)
                {
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    virtualLayout.OnItemsChangedCore(GetLayoutContext(), newValue, args);
                }
                else if (Layout is NonVirtualizingLayout nonVirtualLayout)
                {
                    // Walk through all the elements and make sure they are cleared for
                    // non-virtualizing layouts.
                    foreach (var element in Children)
                    {
                        if (GetVirtualizationInfo(element).IsRealized)
                        {
                            ClearElementImpl(element);
                        }
                    }
                }

                InvalidateMeasure();
            }
        }

        private void OnItemTemplateChanged(IDataTemplate oldValue, IDataTemplate newValue)
        {
            if (_isLayoutInProgress && oldValue != null)
            {
                throw new AvaloniaInternalException("ItemTemplate cannot be changed during layout.");
            }

            // Since the ItemTemplate has changed, we need to re-evaluate all the items that
            // have already been created and are now in the tree. The easiest way to do that
            // would be to do a reset.. Note that this has to be done before we change the template
            // so that the cleared elements go back into the old template.
            if (Layout != null)
            {
                if (Layout is VirtualizingLayout virtualLayout)
                {
                    var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                    _processingItemsSourceChange = args;

                    try
                    {
                        virtualLayout.OnItemsChangedCore(GetLayoutContext(), newValue, args);
                    }
                    finally
                    {
                        _processingItemsSourceChange = null;
                    }
                }
                else if (Layout is NonVirtualizingLayout nonVirtualLayout)
                {
                    // Walk through all the elements and make sure they are cleared for
                    // non-virtualizing layouts.
                    foreach (var element in Children)
                    {
                        if (GetVirtualizationInfo(element).IsRealized)
                        {
                            ClearElementImpl(element);
                        }
                    }
                }
            }

            ItemTemplateShim = new ItemTemplateWrapper(newValue);

            InvalidateMeasure();
        }

        private void OnLayoutChanged(Layout oldValue, Layout newValue)
        {
            if (_isLayoutInProgress)
            {
                throw new InvalidOperationException("Layout cannot be changed during layout.");
            }

            _viewManager.OnLayoutChanging();

            if (oldValue != null)
            {
                oldValue.UninitializeForContext(LayoutContext);
                oldValue.MeasureInvalidated -= InvalidateMeasureForLayout;
                oldValue.ArrangeInvalidated -= InvalidateArrangeForLayout;

                // Walk through all the elements and make sure they are cleared
                foreach (var element in Children)
                {
                    if (GetVirtualizationInfo(element).IsRealized)
                    {
                        ClearElementImpl(element);
                    }
                }

                LayoutState = null;
            }

            if (newValue != null)
            {
                newValue.InitializeForContext(LayoutContext);
                newValue.MeasureInvalidated += InvalidateMeasureForLayout;
                newValue.ArrangeInvalidated += InvalidateArrangeForLayout;
            }

            bool isVirtualizingLayout = newValue != null && newValue is VirtualizingLayout;
            _viewportManager.OnLayoutChanged(isVirtualizingLayout);
            InvalidateMeasure();
        }

        private void OnItemsSourceViewChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (_isLayoutInProgress)
            {
                // Bad things will follow if the data changes while we are in the middle of a layout pass.
                throw new InvalidOperationException("Changes in data source are not allowed during layout.");
            }

            if (IsProcessingCollectionChange)
            {
                throw new InvalidOperationException("Changes in the data source are not allowed during another change in the data source.");
            }

            _processingItemsSourceChange = args;

            try
            {
                //_animationManager.OnItemsSourceChanged(sender, args);
                _viewManager.OnItemsSourceChanged(sender, args);

                if (Layout != null)
                {
                    if (Layout is VirtualizingLayout virtualLayout)
                    {
                        virtualLayout.OnItemsChangedCore(GetLayoutContext(), sender, args);
                    }
                    else
                    {
                        // NonVirtualizingLayout
                        InvalidateMeasure();
                    }
                }
            }
            finally
            {
                _processingItemsSourceChange = null;
            }
        }

        private void InvalidateArrangeForLayout(object sender, EventArgs e) => InvalidateMeasure();

        private void InvalidateMeasureForLayout(object sender, EventArgs e) => InvalidateArrange();

        private VirtualizingLayoutContext GetLayoutContext()
        {
            if (_layoutContext == null)
            {
                _layoutContext = new RepeaterLayoutContext(this);
            }

            return _layoutContext;
        }
    }
}
