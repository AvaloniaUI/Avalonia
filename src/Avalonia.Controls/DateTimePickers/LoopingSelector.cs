using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Avalonia.Controls.Primitives
{

    public delegate void SelectionChangedEventHandler(object sender, SelectionChangedEventArgs e);

    /// <summary>
    /// An items control with the ability for infinite looping
    /// <para>
    /// Supports UI virtualization, though doesn't inherit from ItemsControl so we manage containers and
    /// realized items ourselves
    /// </para>
    /// <para>
    /// In WinUI, this control isn't usable outside of the Date/Time Pickers, but here it technically can be
    /// Note, it's default behavior is for the pickers though. 
    /// </para>
    /// </summary>
    public class LoopingSelector : TemplatedControl
    {
        public LoopingSelector()
        {
            _panel = new LoopingPanel(this);
            LogicalChildren.Add(_panel);
            AddHandler(LoopingSelectorItem.SelectedEvent, OnItemSelected);
            this.GetObservable(BoundsProperty).Subscribe(x => OnBoundsChanged(x));
        }

        static LoopingSelector()
        {
            FocusableProperty.OverrideDefaultValue<LoopingSelector>(true);
            ItemsProperty.Changed.AddClassHandler<LoopingSelector>((x, v) => x.OnItemsChanged(v));
        }

        /// <summary>
        /// Defines the <see cref="Items"/> Property
        /// </summary>
        public static readonly DirectProperty<LoopingSelector, IEnumerable> ItemsProperty =
            AvaloniaProperty.RegisterDirect<LoopingSelector, IEnumerable>("Items",
                x => x.Items, (x, v) => x.Items = v);

        /// <summary>
        /// Defines the <see cref="ItemCount"/> Property
        /// </summary>
        public static readonly DirectProperty<LoopingSelector, int> ItemCountProperty =
            AvaloniaProperty.RegisterDirect<LoopingSelector, int>("ItemCount",
                x => x.ItemCount);

        /// <summary>
        /// Defines the <see cref="SelectedItem"/> Property
        /// </summary>
        public static readonly DirectProperty<LoopingSelector, object> SelectedItemProperty =
            AvaloniaProperty.RegisterDirect<LoopingSelector, object>("SelectedItem",
                x => x.SelectedItem, (x, v) => x.SelectedItem = v);

        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> Property
        /// </summary>
        public static readonly DirectProperty<LoopingSelector, int> SelectedIndexProperty =
            AvaloniaProperty.RegisterDirect<LoopingSelector, int>("SelectedIndex",
                x => x.SelectedIndex, (x, v) => x.SelectedIndex = v);

        //UWP/WinUI has ItemWidth, ignoring, will have items just fill the width of the container

        /// <summary>
        /// Defines the <see cref="ItemHeight"/> Property
        /// </summary>
        public static readonly DirectProperty<LoopingSelector, double> ItemHeightProperty =
           AvaloniaProperty.RegisterDirect<LoopingSelector, double>("ItemHeight",
               x => x.ItemHeight, (x, v) => x.ItemHeight = v);

        /// <summary>
        /// Defines the <see cref="ItemTemplate"/> Property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
            AvaloniaProperty.Register<LoopingSelector, IDataTemplate>("ItemTemplate");

        /// <summary>
        /// Defines the <see cref="ShouldLoop"/> Property
        /// </summary>
        public static readonly DirectProperty<LoopingSelector, bool> ShouldLoopProperty =
            AvaloniaProperty.RegisterDirect<LoopingSelector, bool>("ShouldLoop",
                x => x.ShouldLoop, (x, v) => x.ShouldLoop = v);

        /// <summary>
        /// Gets or sets the Items
        /// </summary>
        public IEnumerable Items
        {
            get => _items;
            set => SetAndRaise(ItemsProperty, ref _items, value);
        }

        /// <summary>
        /// Gets the number of items
        /// </summary>
        public int ItemCount
        {
            get => _itemCount;
            private set => SetAndRaise(ItemCountProperty, ref _itemCount, value);
        }

        /// <summary>
        /// Gets or sets the SelectedIndex
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (Items == null || ItemCount == 0)
                    return;
                var old = _selectedIndex;
                SetAndRaise(SelectedIndexProperty, ref _selectedIndex, value);

                var oldItem = _selectedItem;
                if (value == -1)
                {
                    _selectedItem = null;
                }
                else
                {
                    if (Items is IList l)
                        _selectedItem = l[value];
                    else
                        _selectedItem = Items.ElementAt(value);
                }
                RaisePropertyChanged(SelectedItemProperty, oldItem, _selectedItem);
                if (!_preventMovingScrollWhenSelecting)
                    UpdateOffset();

                SelectionChangedEventArgs args = new SelectionChangedEventArgs(null, new object[] { oldItem }, new object[] { _selectedItem });
                OnSelectionChanged(this, args);
            }
        }

        /// <summary>
        /// Gets or sets the SelectedItem
        /// </summary>
        public object SelectedItem
        {
            get
            {
                if (SelectedIndex == -1)
                    return null;
                if (Items is IList l)
                    return l[SelectedIndex];
                else
                    return Items.ElementAt(SelectedIndex);
            }
            set
            {
                if (value == null)
                {
                    SelectedIndex = -1;
                }
                else
                {
                    if (Items is IList l)
                        SelectedIndex = l.IndexOf(value);
                    else
                        SelectedIndex = Items.IndexOf(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the items
        /// </summary>
        public double ItemHeight
        {
            get => _itemHeight;
            set
            {
                SetAndRaise(ItemHeightProperty, ref _itemHeight, value);
            }
        }

        /// <summary>
        /// Gets or sets the item template
        /// </summary>
        public IDataTemplate ItemTemplate
        {
            get => GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the items should loop
        /// </summary>
        public bool ShouldLoop
        {
            get => _shouldLoop;
            set
            {
                SetAndRaise(ShouldLoopProperty, ref _shouldLoop, value);
            }
        }

        /// <summary>
        /// Raised when the SelectedItem/Index changes
        /// </summary>
        public event SelectionChangedEventHandler SelectionChanged;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if(_scroller != null)
            {
                _scroller.Content = null;
            }
            base.OnApplyTemplate(e);
            _scroller = e.NameScope.Find<ScrollViewer>("Scroller");

            _scroller.Content = _panel;

            _upButton = e.NameScope.Find<RepeatButton>("UpButton");
            if (_upButton != null)
            {
                _upButton.Click += OnUpButtonClick;
            }

            _downButton = e.NameScope.Find<RepeatButton>("DownButton");
            if (_downButton != null)
            {
                _downButton.Click += OnDownButtonClick;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (ShouldLoop)
                    {
                        var selIndex = SelectedIndex;
                        selIndex--;
                        if (selIndex < 0)
                            selIndex += ItemCount;
                        SelectedIndex = selIndex;
                    }
                    else
                    {
                        SelectedIndex = Math.Max(0, SelectedIndex - 1);
                    }
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (ShouldLoop)
                    {
                        var selIndex = SelectedIndex;
                        selIndex++;
                        if (selIndex >= ItemCount)
                            selIndex -= ItemCount;
                        SelectedIndex = selIndex;
                    }
                    else
                    {
                        SelectedIndex = Math.Min(ItemCount, SelectedIndex + 1);
                    }
                    e.Handled = true;
                    break;
                case Key.PageUp:
                    if (ShouldLoop)
                    {
                        var selIndex = SelectedIndex;
                        selIndex -= 4;
                        if (selIndex < 0)
                            selIndex += ItemCount;
                        SelectedIndex = selIndex;
                    }
                    else
                    {
                        SelectedIndex = Math.Max(0, SelectedIndex - 4);
                    }
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            KeyboardDevice.Instance.SetFocusedElement(this, NavigationMethod.Pointer, KeyModifiers.None);
            FocusManager.Instance.Focus(this, NavigationMethod.Pointer, KeyModifiers.None);
        }

        private void OnDownButtonClick(object sender, RoutedEventArgs e)
        {
            var selIndex = SelectedIndex;
            if (selIndex == ItemCount - 1)
                SelectedIndex = 0;
            else
                SelectedIndex++;
            e.Handled = true;
        }

        private void OnUpButtonClick(object sender, RoutedEventArgs e)
        {
            var selIndex = SelectedIndex;
            if (selIndex == 0)
                SelectedIndex = ItemCount - 1;
            else
                SelectedIndex--;
            e.Handled = true;
        }

        private void OnItemsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyCollectionChanged oldC)
            {
                oldC.CollectionChanged -= OnItemsCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newC)
            {
                newC.CollectionChanged += OnItemsCollectionChanged;
            }

            //When the entire items list changes, reset selection quietly
            _selectedIndex = -1;
            _selectedItem = null;

            if (Items is IList l)
            {
                ItemCount = l.Count;
            }
            else
            {
                ItemCount = Items.Count();
            }

            EnsureContainers();
            UpdateOffset();
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    ItemCount += e.NewItems.Count;
                    var index = e.NewStartingIndex;
                    //if (IsContainerIndexLoaded(index))
                    //{
                    //    AddContainers(index, e.NewItems);
                    //}
                    EnsureContainers();
                    UpdateOffset();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    ItemCount -= e.OldItems.Count;
                    EnsureContainers();
                    UpdateOffset();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _selectedIndex = 0;
                    _selectedItem = 0;
                    ItemCount = 0;
                    _panel.Children.Clear();
                    UpdateOffset();
                    break;

                //TODO - items source should be static anyway
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    throw new NotSupportedException("Can't move or replace items in ItemsCollection");
            }
        }

        /// <summary>
        /// Ensures we have the correct number of containers in the LoopingSelectorPanel
        /// This will add, remove, or clear the panel as necessary
        /// </summary>
        private void EnsureContainers()
        {
            if (Bounds.Height == 0)
                return;

            int itemCount = ItemCount;
            //How many containers we ideally want
            int desiredItemsLoaded = (_totalItemsInViewport * 2) + 1;
            
            var realizedContainerCount = _panel.Children.Count;

            if (ShouldLoop)
            {
                //When looping we should ALWAYS have desiredItemsLoaded number of containers
                //available
                var delta = Math.Abs(realizedContainerCount - desiredItemsLoaded);
                if (realizedContainerCount < desiredItemsLoaded) //Add more containers
                {
                    List<LoopingSelectorItem> panelItems = new List<LoopingSelectorItem>();
                    for (int i = 0; i < delta; i++)
                    {
                        LoopingSelectorItem lsi = new LoopingSelectorItem();
                        lsi.Height = ItemHeight;
                        lsi.ContentTemplate = ItemTemplate;
                        panelItems.Add(lsi);
                    }
                    _panel.Children.AddRange(panelItems);
                }
                else if (realizedContainerCount > desiredItemsLoaded) //Remove extra containers
                {
                    //Technically not needed now as we don't move the containers when looping, just
                    //swap content, but is here in case of future improvements
                    _panel.Children.RemoveRange(realizedContainerCount - delta, delta);
                }
            }
            else
            {
                //When we're not looping, things are a little trickier, if we're near the bounds of scrolling,
                //We may not have desiredItemsLoaded containers, so we need to account for that
                //NumContainers here should be in range [_totalItemsInViewport, desiredItemsLoaded]

                //First index of realized items
                int selIndex = SelectedIndex;
                selIndex = selIndex == -1 ? 0 : selIndex;

                int numItemsAboveSelected = _totalItemsInViewport;
                if (selIndex - numItemsAboveSelected < 0)
                    numItemsAboveSelected = selIndex;

                int numItemsBelowSelected = _totalItemsInViewport;
                if (selIndex + _totalItemsInViewport >= ItemCount)
                    numItemsBelowSelected = ItemCount - selIndex - 1;

                int neededContainers = numItemsBelowSelected + numItemsAboveSelected + 1;
                int currentCount = _panel.Children.Count;

                //Do we need containers?
                var numContsToAddRemove = neededContainers - currentCount;
                
                if (numContsToAddRemove > 0) //Add Containers
                {
                    List<LoopingSelectorItem> panelItems = new List<LoopingSelectorItem>();

                    for (int i = 0; i < numContsToAddRemove; i++)
                    {
                        LoopingSelectorItem lsi = new LoopingSelectorItem();
                        lsi.Height = ItemHeight;
                        lsi.ContentTemplate = ItemTemplate;
                        panelItems.Add(lsi);
                    }
                    _panel.Children.AddRange(panelItems);
                }
                else if (numContsToAddRemove < 0) //Remove containers
                {
                    numContsToAddRemove = Math.Abs(numContsToAddRemove);
                    _panel.Children.RemoveRange(currentCount - numContsToAddRemove, numContsToAddRemove);
                }
            }

            if (ItemCount > 0)
                SetItemContent();
        }

        /// <summary>
        /// Sets the content of the loaded containers
        /// </summary>
        private void SetItemContent()
        {
            int itemCount = ItemCount;

            if (ShouldLoop)
            {
                var selIndex = SelectedIndex;
                var panelItems = _panel.Children;

                int c = 0;
                int index = selIndex == -1 ? -_totalItemsInViewport : selIndex - _totalItemsInViewport;

                while (c < panelItems.Count)
                {
                    if (index >= ItemCount)
                        index -= ItemCount;
                    if (index < 0)
                        index += ItemCount;

                    if (index == selIndex)
                        (panelItems[c] as LoopingSelectorItem).IsSelected = true;
                    else
                        (panelItems[c] as LoopingSelectorItem).IsSelected = false;

                    (panelItems[c] as LoopingSelectorItem).Content = GetElementAt(index);
                    c++;
                    index++;
                }
            }
            else
            {
                //We first need the first item in the realized items...
                var selIndex = SelectedIndex;
                var firstIndex = Math.Max(0, selIndex - _totalItemsInViewport);
                var panelItems = _panel.Children;

                for (int i = 0; i < panelItems.Count; i++)
                {
                    if (firstIndex == selIndex)
                        (panelItems[i] as LoopingSelectorItem).IsSelected = true;
                    else
                        (panelItems[i] as LoopingSelectorItem).IsSelected = false;

                    (panelItems[i] as LoopingSelectorItem).Content = GetElementAt(firstIndex);
                    firstIndex++;
                }

            }
        }

        private object GetElementAt(int index)
        {
            if (index < 0 || index >= ItemCount)
                return null;

            if (Items is IList l)
                return l[index];
            else
                return Items.Cast<object>().ToList()[index];
        }

        /// <summary>
        /// Updates the scrollviewer offset when the selectedindex changed
        /// </summary>
        private void UpdateOffset()
        {
            if (_panel == null || ItemCount == 0)
                return;

            _preventUpdateSelection = true;

            if (ShouldLoop)
            {
                //We measure for 10x as many items, so when we set the SelectedIndex
                //and need to change the offset, should set it towards the middle
                //so we preserve scrolling in both directions
                int selIndex = SelectedIndex;
                selIndex = selIndex == -1 ? 0 : selIndex;
                var extent = ItemCount * ItemHeight;
                _panel.Offset = new Vector(0, (selIndex * ItemHeight) + (extent * 5));

            }
            else
            {
                //Not looping, just convert the SelectedIndex to an offset
                //if -1, set to 0;
                int selIndex = SelectedIndex;
                if (ItemCount == 0 || SelectedIndex == -1)
                    _panel.Offset = new Vector(0, 0);
                else
                    _panel.Offset = new Vector(0, selIndex * ItemHeight);
            }

            EnsureContainers();

            _preventUpdateSelection = false;
        }

        /// <summary>
        /// Updates the SelectedIndex when scrolling occurs
        /// </summary>
        /// <param name="offsetY"></param>
        internal void SetSelectedIndexFromOffset(double offsetY)
        {
            if (_preventUpdateSelection)
                return;

            _preventMovingScrollWhenSelecting = true;

            if (ShouldLoop)
            {
                var extent = ItemCount * ItemHeight;
                var numExtents = offsetY / extent;
                numExtents = numExtents < 0 ? 0 : Math.Truncate(numExtents);
                var pixelOffset = offsetY - extent * numExtents;

                SelectedIndex = (int)(pixelOffset / ItemHeight);
            }
            else
            {
                SelectedIndex = (int)(offsetY / ItemHeight);
            }

            EnsureContainers();

            _preventMovingScrollWhenSelecting = false;
        }

        private void OnItemSelected(object sender, RoutedEventArgs e)
        {
            var item = (e.Source as LoopingSelectorItem).Content;
            SelectedItem = item;
        }

        protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectionChanged?.Invoke(sender, e);
        }

        private void OnBoundsChanged(Rect x)
        {
            //Ideally we always want this to be odd, since the selected item is placed in the middle,
            //so we have the same number of items above and below at all times
            _totalItemsInViewport = (int)Math.Ceiling(x.Height / ItemHeight);
            if (_totalItemsInViewport % 2 == 0)
                _totalItemsInViewport += 1;

            EnsureContainers();
        }


        //TemplateItems
        private RepeatButton _downButton;
        private RepeatButton _upButton;
        private ScrollViewer _scroller;

        private LoopingPanel _panel;

        private int _totalItemsInViewport;
        private IEnumerable _items;
        private int _itemCount;
        private int _selectedIndex = -1;
        private object _selectedItem;
        private double _itemHeight = 32;
        private bool _shouldLoop = true;
        private bool _preventUpdateSelection;
        private bool _preventMovingScrollWhenSelecting;
    }
}
