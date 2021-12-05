using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia.Controls.Utils;

#nullable enable

namespace Avalonia.Controls.Presenters
{
    internal class ItemContainerSync : IDisposable
    {
        private readonly ItemsControl _itemsControl;
        private readonly IPanel _panel;
        private readonly List<IControl?> _elements;

        public ItemContainerSync(ItemsControl itemsControl, IPanel panel)
        {
            _itemsControl = itemsControl;
            _panel = panel;
            _elements = new();
            _elements.InsertMany(0, null, _itemsControl.ItemsView.Count);
            _itemsControl.ItemsChanged += OnItemsChanged;
        }

        public void Dispose()
        {
            _elements.Clear();
            _panel.Children.Clear();
            _itemsControl.ItemsChanged -= OnItemsChanged;
        }

        public void GenerateContainers()
        {
            Debug.Assert(_elements.Count == _itemsControl.ItemsView.Count);

            var count = _elements.Count;
            var items = _itemsControl.ItemsView;
            var generator = _itemsControl.ItemContainerGenerator!;

            for (var i = 0; i < count; i++)
            {
                if (_elements[i] is null)
                {
                    var item = items[i];
                    var element = generator.Realize(_panel, i, item);
                    _panel.Children.Insert(i, element);
                    _elements[i] = element;
                    _itemsControl.RaiseContainerRealized(element, i, item);
                }
            }
        }

        private void OnItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            void Add()
            {
                _elements.InsertMany(e.NewStartingIndex, null, e.NewItems.Count);
            }

            void Remove()
            {
                for (var i = e.OldStartingIndex + e.OldItems.Count - 1; i >= e.OldStartingIndex; --i)
                {
                    if (_elements[i] is IControl element)
                    {
                        _panel.Children.Remove(element);
                        _elements.RemoveAt(i);
                        _itemsControl.RaiseContainerUnrealized(element, i);
                    }
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    for (var i = e.OldStartingIndex + e.OldItems.Count - 1; i >= e.OldStartingIndex; --i)
                    {
                        if (_elements[i] is IControl element)
                        {
                            _panel.Children.Remove(element);
                            _elements[i] = null;
                            _itemsControl.RaiseContainerUnrealized(element, i);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Move:
                    Remove();
                    Add();
                    break;
                case NotifyCollectionChangedAction.Reset:
                    _panel.Children.Clear();

                    for (var i = 0; i < _elements.Count; ++i)
                    {
                        if (_elements[i] is IControl element)
                            _itemsControl.RaiseContainerUnrealized(element, i);
                    }

                    _elements.Clear();
                    _elements.InsertMany(0, null, _itemsControl.ItemsView.Count);
                    break;
                default:
                    throw new NotImplementedException();
            }

            _itemsControl.Presenter!.InvalidateMeasure();
        }
    }
}
