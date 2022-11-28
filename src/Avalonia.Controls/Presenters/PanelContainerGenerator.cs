using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using Avalonia.Controls.Utils;

namespace Avalonia.Controls.Presenters
{
    /// <summary>
    /// Generates containers for <see cref="ItemsPresenter"/>s that have non-virtualizing panels.
    /// </summary>
    internal class PanelContainerGenerator : IDisposable
    {
        private readonly ItemsPresenter _presenter;

        public PanelContainerGenerator(ItemsPresenter presenter)
        {
            Debug.Assert(presenter.ItemsControl is not null);
            Debug.Assert(presenter.Panel is not null or VirtualizingPanel);
            
            _presenter = presenter;
            _presenter.ItemsControl.PropertyChanged += OnItemsControlPropertyChanged;

            if (_presenter.ItemsControl.Items is INotifyCollectionChanged incc)
                incc.CollectionChanged += OnItemsChanged;

            OnItemsChanged(null, CollectionUtils.ResetEventArgs);
        }

        public void Dispose()
        {
            if (_presenter.ItemsControl is { } itemsControl)
            {
                itemsControl.PropertyChanged -= OnItemsControlPropertyChanged;

                if (itemsControl.Items is INotifyCollectionChanged incc)
                    incc.CollectionChanged -= OnItemsChanged;

                itemsControl.ClearLogicalChildren();
            }

            _presenter.Panel?.Children.Clear();
        }

        private void OnItemsControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ItemsControl.ItemsProperty)
            {
                if (e.OldValue is INotifyCollectionChanged inccOld)
                    inccOld.CollectionChanged -= OnItemsChanged;
                OnItemsChanged(null, CollectionUtils.ResetEventArgs);
                if (e.NewValue is INotifyCollectionChanged inccNew)
                    inccNew.CollectionChanged += OnItemsChanged;
            }
        }

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_presenter.Panel is null || _presenter.ItemsControl is null)
                return;

            var itemsControl = _presenter.ItemsControl;
            var panel = _presenter.Panel;

            void Add(int index, IEnumerable items)
            {
                var i = index;
                foreach (var item in items)
                    panel.Children.Insert(i++, CreateContainer(itemsControl, item, i));
            }
            
            void Remove(int index, int count)
            {
                for (var i = 0; i < count; ++i)
                {
                    itemsControl.RemoveLogicalChild(panel.Children[i + index]);
                    panel.Children.RemoveAt(i + index);
                }
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Add(e.NewStartingIndex, e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Move:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    itemsControl.ClearLogicalChildren();
                    panel.Children.Clear();
                    if (_presenter.ItemsControl?.Items is { } items)
                        Add(0, items);
                    break;
            }
        }

        private static Control CreateContainer(ItemsControl itemsControl, object? item, int index)
        {
            var generator = itemsControl.ItemContainerGenerator;

            if (item is Control c && generator.IsItemItsOwnContainer(c))
            {
                return c;
            }
            else
            {
                c = generator.CreateContainer();
                itemsControl.AddLogicalChild(c);
                generator.PrepareItemContainer(c, item, index);
                return c;
            }
        }
    }
}
