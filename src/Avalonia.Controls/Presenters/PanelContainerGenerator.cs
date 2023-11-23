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
        private static readonly AttachedProperty<bool> ItemIsOwnContainerProperty =
            AvaloniaProperty.RegisterAttached<PanelContainerGenerator, Control, bool>("ItemIsOwnContainer");

        private readonly ItemsPresenter _presenter;

        public PanelContainerGenerator(ItemsPresenter presenter)
        {
            Debug.Assert(presenter.ItemsControl is not null);
            Debug.Assert(presenter.Panel is not null or VirtualizingPanel);
            
            _presenter = presenter;
            _presenter.ItemsControl.ItemsView.PostCollectionChanged += OnItemsChanged;

            OnItemsChanged(null, CollectionUtils.ResetEventArgs);
        }

        public void Dispose()
        {
            if (_presenter.ItemsControl is { } itemsControl)
            {
                itemsControl.ItemsView.PostCollectionChanged -= OnItemsChanged;
                ClearItemsControlLogicalChildren();
            }

            _presenter.Panel?.Children.Clear();
        }

        internal void Refresh() => OnItemsChanged(null, CollectionUtils.ResetEventArgs);

        private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_presenter.Panel is null || _presenter.ItemsControl is null)
                return;

            var itemsControl = _presenter.ItemsControl;
            var generator = itemsControl.ItemContainerGenerator;
            var children = _presenter.Panel.Children;

            void Add(int index, IEnumerable items)
            {
                var i = index;
                foreach (var item in items)
                    InsertContainer(itemsControl, children, item, i++);

                var childCount = children.Count;
                var delta = i - index;

                for (; i < childCount; ++i)
                    generator.ItemContainerIndexChanged(children[i], i - delta, i);
            }

            void Remove(int index, int count)
            {
                for (var i = 0; i < count; ++i)
                {
                    var c = children[index + i];

                    itemsControl.RemoveLogicalChild(children[i + index]);

                    if (!c.IsSet(ItemIsOwnContainerProperty))
                    {
                        generator.ClearItemContainer(c);
                    }
                }

                children.RemoveRange(index, count);

                var childCount = children.Count;

                for (var i = index; i < childCount; ++i)
                    generator.ItemContainerIndexChanged(children[i], i + count, i);
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
                case NotifyCollectionChangedAction.Move:
                    Remove(e.OldStartingIndex, e.OldItems!.Count);
                    Add(e.NewStartingIndex, e.NewItems!);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    ClearItemsControlLogicalChildren();
                    children.Clear();
                    Add(0, _presenter.ItemsControl.ItemsView);
                    break;
            }
        }

        private static void InsertContainer(
            ItemsControl itemsControl,
            Controls children,
            object? item, 
            int index)
        {
            var generator = itemsControl.ItemContainerGenerator;
            Control container;
            
            if (generator.NeedsContainer(item, index, out var recycleKey))
            {
                container = generator.CreateContainer(item, index, recycleKey);
            }
            else
            {
                container = (Control)item!;
                container.SetValue(ItemIsOwnContainerProperty, true);
            }

            generator.PrepareItemContainer(container, item, index);
            itemsControl.AddLogicalChild(container);
            children.Insert(index, container);
            generator.ItemContainerPrepared(container, item, index);
        }

        private void ClearItemsControlLogicalChildren()
        {
            if (_presenter.Panel is null || _presenter.ItemsControl is null)
                return;

            var itemsControl = _presenter.ItemsControl;
            var panel = _presenter.Panel;

            foreach (var c in panel.Children)
            {
                if (!c.IsSet(ItemIsOwnContainerProperty))
                    itemsControl.RemoveLogicalChild(c);
            }
        }
    }
}
