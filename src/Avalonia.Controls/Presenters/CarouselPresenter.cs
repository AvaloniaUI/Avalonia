using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    public class CarouselPresenter : Panel, IItemsPresenter, ICollectionChangedListener
    {
        /// <summary>
        /// Defines the <see cref="IsVirtualized"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsVirtualizedProperty =
            Carousel.IsVirtualizedProperty.AddOwner<CarouselPresenter>();

        /// <summary>
        /// Defines the <see cref="Items"/> property.
        /// </summary>
        public static readonly DirectProperty<CarouselPresenter, IEnumerable?> ItemsProperty =
            ItemsControl.ItemsProperty.AddOwner<CarouselPresenter>(o => o.Items, (o, v) => o.Items = v);
        
        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly DirectProperty<CarouselPresenter, int> SelectedIndexProperty =
            SelectingItemsControl.SelectedIndexProperty.AddOwner<CarouselPresenter>(
                o => o.SelectedIndex,
                (o, v) => o.SelectedIndex = v);

        private IItemsPresenterHost? _host;
        private IElementFactory? _elementFactory;
        private ItemsSourceView? _items;
        private int _realizedIndex = -1;
        private List<IControl?>? _nonVirtualizedContainers;
        private int _selectedIndex;

        /// <summary>
        /// Gets or sets the element factory used to create items for the control.
        /// </summary>
        public IElementFactory? ElementFactory
        {
            get => _elementFactory;
            private set
            {
                if (_elementFactory != value)
                {
                    _elementFactory = value;
                    // TODO: Reset controls
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the items in the carousel are virtualized.
        /// </summary>
        /// <remarks>
        /// When the carousel is virtualized, only the active page is held in memory.
        /// </remarks>
        public bool IsVirtualized
        {
            get => GetValue(IsVirtualizedProperty);
            set => SetValue(IsVirtualizedProperty, value);
        }

        /// <summary>
        /// Gets or sets an object source used to generate the content of the CarouselPresenter.
        /// </summary>
        public IEnumerable? Items
        {
            get => _items;
            set
            {
                if (value is null || value is ItemsSourceView)
                {
                    if (value != _items)
                    {
                        var oldValue = _items;
                        _items?.RemoveListener(this);
                        _items = value as ItemsSourceView;
                        _items?.AddListener(this);
                        RaisePropertyChanged(ItemsProperty, oldValue, _items);
                        InvalidateMeasure();
                    }
                }
                else
                {
                    throw new ArgumentException("Carousel.Items must be an ItemsSourceView.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the index of the selected page.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => SetAndRaise(SelectedIndexProperty, ref _selectedIndex, value);
        }

        /// <summary>
        /// Gets the currently realized elements.
        /// </summary>
        public IEnumerable<IControl> RealizedElements => VisualChildren.OfType<IControl>();

        public event EventHandler<ElementPreparedEventArgs>? ElementPrepared;
        public event EventHandler<ElementClearingEventArgs>? ElementClearing;
        public event EventHandler<ElementIndexChangedEventArgs>? ElementIndexChanged;

        public int GetElementIndex(IControl element)
        {
            if (IsVirtualized)
                return element == Children[0] ? _realizedIndex : -1;
            else
                return _nonVirtualizedContainers?.IndexOf(element) ?? -1;
        }

        public bool ScrollIntoView(int index)
        {
            // Scrolling into view in a carousel is not supported: the selected item must be
            // set in order for an element to be made visible.
            return false;
        }

        public IControl? TryGetElement(int index)
        {
            if (IsVirtualized)
            {
                return index == _realizedIndex ? Children[0] : null;
            }
            else if (index < _nonVirtualizedContainers?.Count)
            {
                return _nonVirtualizedContainers?[index];
            }

            return null;
        }


        void ICollectionChangedListener.PreChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle collection changed events early so that we can update _realizedIndex before we
            // receive the SelectedIndex property change. This avoids having to carry out an unnecessary
            // layout pass because although the selected index has changed, the item being displayed
            // hasn't.
            OnItemsCollectionChanged(e);
        }

        void ICollectionChangedListener.Changed(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e) { }
        void ICollectionChangedListener.PostChanged(INotifyCollectionChanged sender, NotifyCollectionChangedEventArgs e) { }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (IsVirtualized)
                return MeasureVirtualized(availableSize);
            else
                return MeasureNonVirtualized(availableSize);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            if (change.Property == SelectedIndexProperty &&
                change.NewValue.GetValueOrDefault<int>() != _realizedIndex)
            {
                InvalidateMeasure();
            }
            else if (change.Property == TemplatedParentProperty)
            {
                _host = change.NewValue.GetValueOrDefault<IItemsPresenterHost>();

                if (_host is object)
                {
                    _host.RegisterItemsPresenter(this);
                    ElementFactory = _host.ElementFactory;
                }
            }

            base.OnPropertyChanged(change);
        }

        private Size MeasureVirtualized(Size availableSize)
        {
            if (_items is null || _elementFactory is null || _selectedIndex < 0 || _selectedIndex > _items.Count)
            {
                Children.Clear();
                _realizedIndex = -1;
                return default;
            }

            IControl? element = null;

            if (_realizedIndex != _selectedIndex)
            {
                Children.Clear();
                element = GetElement(_selectedIndex);
                _realizedIndex = _selectedIndex;
            }
            else if (Children.Count > 0)
            {
                element = Children[0];
            }

            element?.Measure(availableSize);
            return element?.DesiredSize ?? default;
        }

        private Size MeasureNonVirtualized(Size availableSize)
        {
            if (_items is null || _elementFactory is null)
            {
                Children.Clear();
                _realizedIndex = -1;
                return default;
            }

            if (_nonVirtualizedContainers is null)
            {
                _nonVirtualizedContainers = new List<IControl?>(_items.Count);
                AddItems(0, _items.Count);
            }

            var result = Size.Empty;

            for (var i = 0; i < _nonVirtualizedContainers.Count; ++i)
            {
                var element = _nonVirtualizedContainers[i] ??= GetElement(i);

                if (i == _selectedIndex)
                {
                    element.IsVisible = true;
                    element.Measure(availableSize);
                    result = element.DesiredSize;
                }
                else
                {
                    element.IsVisible = false;
                }
            }

            _realizedIndex = _selectedIndex;
            return result;
        }

        private IControl GetElement(int index)
        {
            var data = _items![index];
            var element = _elementFactory!.GetElement(new ElementFactoryGetArgs
            {
                Data = data,
                Index = index,
                Parent = this,
            });

            element.DataContext = data;

            if (element.VisualParent != this)
                Children.Add(element);

            return element;
        }

        private void OnItemsCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            void RemoveItems(int startIndex, int count)
            {
                if (_nonVirtualizedContainers is null)
                    return;

                for (var i = 0; i < count; ++i)
                {
                    var element = _nonVirtualizedContainers[startIndex];
                    Children.Remove(element);
                    _nonVirtualizedContainers.RemoveAt(startIndex);
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewStartingIndex <= _realizedIndex)
                        _realizedIndex += e.NewItems.Count;

                    AddItems(e.NewStartingIndex, e.NewItems.Count);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex + e.OldItems.Count < _realizedIndex)
                    {
                        _realizedIndex -= e.OldItems.Count;
                    }
                    else if (e.OldStartingIndex <= _realizedIndex &&
                        e.OldStartingIndex + e.OldItems.Count > _realizedIndex)
                    {
                        _realizedIndex = -1;

                        if (IsVirtualized)
                            Children.Clear();

                        InvalidateMeasure();
                    }

                    RemoveItems(e.OldStartingIndex, e.OldItems.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.NewStartingIndex <= _realizedIndex &&
                        e.NewStartingIndex + e.NewItems.Count > _realizedIndex)
                    {
                        InvalidateMeasure();
                    }

                    if (_nonVirtualizedContainers is object)
                    {
                        for (var i = e.OldStartingIndex; i < e.OldStartingIndex + e.OldItems.Count; ++i)
                        {
                            var element = _nonVirtualizedContainers[i];
                            Children.Remove(element);
                            _nonVirtualizedContainers[i] = null;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex <= _realizedIndex &&
                        e.OldStartingIndex + e.OldItems.Count > _realizedIndex)
                    {
                        _realizedIndex = (_realizedIndex - e.OldStartingIndex) + e.NewStartingIndex;
                        InvalidateMeasure();
                    }

                    RemoveItems(e.OldStartingIndex, e.OldItems.Count);
                    AddItems(e.NewStartingIndex, e.NewItems.Count);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void AddItems(int index, int count)
        {
            if (_nonVirtualizedContainers is null)
                return;

            for (var i = 0; i < count; ++i)
                _nonVirtualizedContainers.Insert(index, null);

            InvalidateMeasure();
        }
    }
}
