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
    internal class ItemsPresenterContainerGenerator : IDisposable
    {
        private readonly ItemsPresenter _presenter;

        public ItemsPresenterContainerGenerator(ItemsPresenter presenter)
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
            _presenter.ItemsControl!.PropertyChanged -= OnItemsControlPropertyChanged;

            if (_presenter.ItemsControl.Items is INotifyCollectionChanged incc)
                incc.CollectionChanged -= OnItemsChanged;

            _presenter.Panel!.Children.Clear();
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
            if (_presenter.ItemsControl?.Items is null || _presenter.Panel is null)
                return;

            var generator = _presenter.ItemsControl.ItemContainerGenerator;
            var panel = _presenter.Panel;

            void Add(int index, IEnumerable items)
            {
                var i = index;

                foreach (var item in items)
                {
                    var c = generator.Materialize(i, item);
                    panel.Children.Insert(i++, c.ContainerControl);
                }
            }
            
            void Remove(int index, int count)
            {
                for (var i = 0; i < count; ++i)
                    panel.Children.RemoveAt(i + index);
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    generator.InsertSpace(e.NewStartingIndex, e.NewItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    generator.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    generator.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    generator.InsertSpace(e.NewStartingIndex, e.NewItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Move:
                    generator.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    generator.InsertSpace(e.NewStartingIndex, e.NewItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    generator.Clear();
                    panel.Children.Clear();
                    if (_presenter.ItemsControl.Items is { } items)
                        Add(0, items);
                    break;
            }
        }
    }
}
