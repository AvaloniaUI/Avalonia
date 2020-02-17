using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// Synchronizes an <see cref="ISelectionModel"/> with a list of SelectedItems.
    /// </summary>
    internal class SelectedItemsSync
    {
        private IList? _items;
        private bool _updatingItems;
        private bool _updatingModel;

        public SelectedItemsSync(ISelectionModel model)
        {
            Model = model;
        }

        public ISelectionModel Model { get; private set; }

        public IList GetOrCreateItems()
        {
            if (_items == null)
            {
                var items = new AvaloniaList<object>(Model.SelectedItems);
                items.CollectionChanged += ItemsCollectionChanged;
                Model.SelectionChanged += SelectionModelSelectionChanged;
                _items = items;
            }

            return _items;
        }

        public void SetItems(IList items)
        {
            items = items ?? throw new ArgumentNullException(nameof(items));

            if (items.IsFixedSize)
            {
                throw new NotSupportedException(
                    "Cannot assign fixed size selection to SelectedItems.");
            }

            if (_items is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= ItemsCollectionChanged;
            }

            if (_items == null)
            {
                Model.SelectionChanged += SelectionModelSelectionChanged;
            }

            try
            {
                _updatingModel = true;
                _items = items;

                using (Model.Update())
                {
                    Model.ClearSelection();
                    Add(items);
                }

                if (_items is INotifyCollectionChanged incc2)
                {
                    incc2.CollectionChanged += ItemsCollectionChanged;
                }
            }
            finally
            {
                _updatingModel = false;
            }
        }

        public void SetModel(ISelectionModel model)
        {
            model = model ?? throw new ArgumentNullException(nameof(model));

            if (_items != null)
            {
                Model.SelectionChanged -= SelectionModelSelectionChanged;
                Model = model;
                Model.SelectionChanged += SelectionModelSelectionChanged;

                try
                {
                    _updatingItems = true;
                    _items.Clear();

                    foreach (var i in model.SelectedItems)
                    {
                        _items.Add(i);
                    }
                }
                finally
                {
                    _updatingItems = false;
                }
            }
        }

        private void ItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_updatingItems)
            {
                return;
            }

            if (_items == null)
            {
                throw new AvaloniaInternalException("CollectionChanged raised but we don't have items.");
            }

            void Remove()
            {
                foreach (var i in e.OldItems)
                {
                    var index = IndexOf(Model.Source, i);

                    if (index != -1)
                    {
                        Model.Deselect(index);
                    }
                }
            }

            try
            {
                using var operation = Model.Update();

                _updatingModel = true;

                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        Add(e.NewItems);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        Remove();
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        Remove();
                        Add(e.NewItems);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        Model.ClearSelection();
                        Add(_items);
                        break;
                }
            }
            finally
            {
                _updatingModel = false;
            }
        }

        private void Add(IList newItems)
        {
            foreach (var i in newItems)
            {
                var index = IndexOf(Model.Source, i);

                if (index != -1)
                {
                    Model.Select(index);
                }
            }
        }

        private void SelectionModelSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            if (_updatingModel)
            {
                return;
            }

            if (_items == null)
            {
                throw new AvaloniaInternalException("SelectionModelChanged raised but we don't have items.");
            }

            try
            {
                var deselected = e.DeselectedItems.ToList();
                var selected = e.SelectedItems.ToList();

                _updatingItems = true;

                foreach (var i in deselected)
                {
                    _items.Remove(i);
                }

                foreach (var i in selected)
                {
                    _items.Add(i);
                }
            }
            finally
            {
                _updatingItems = false;
            }
        }

        private static int IndexOf(object source, object item)
        {
            if (source is IList l)
            {
                return l.IndexOf(item);
            }
            else if (source is ItemsSourceView v)
            {
                return v.IndexOf(item);
            }

            return -1;
        }
    }
}
