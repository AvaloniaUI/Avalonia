using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls.Selection;

#nullable enable

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// Synchronizes an <see cref="ISelectionModel"/> with a list of SelectedItems.
    /// </summary>
    internal class SelectedItemsSync
    {
        private IList? _selectedItems;
        private bool _updatingItems;
        private bool _updatingModel;
        private bool _initializeOnSourceAssignment;

        public SelectedItemsSync(ISelectionModel model)
        {
            model = model ?? throw new ArgumentNullException(nameof(model));
            Model = model;
        }

        public ISelectionModel Model { get; private set; }

        public IList GetOrCreateSelectedItems()
        {
            if (_selectedItems == null)
            {
                var items = new AvaloniaList<object?>(Model.SelectedItems);
                items.CollectionChanged += ItemsCollectionChanged;
                Model.SelectionChanged += SelectionModelSelectionChanged;
                _selectedItems = items;
            }

            return _selectedItems;
        }

        public void SetSelectedItems(IList? items)
        {
            items ??= new AvaloniaList<object>();

            if (items.IsFixedSize)
            {
                throw new NotSupportedException(
                    "Cannot assign fixed size selection to SelectedItems.");
            }

            if (_selectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= ItemsCollectionChanged;
            }

            if (_selectedItems == null)
            {
                Model.SelectionChanged += SelectionModelSelectionChanged;
            }

            try
            {
                _updatingModel = true;
                _selectedItems = items;

                if (Model.Source is object)
                {
                    using (Model.BatchUpdate())
                    {
                        Model.Clear();
                        Add(items);
                    }
                }
                else if (!_initializeOnSourceAssignment)
                {
                    Model.PropertyChanged += SelectionModelPropertyChanged;
                    _initializeOnSourceAssignment = true;
                }

                if (_selectedItems is INotifyCollectionChanged incc2)
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

            if (_selectedItems != null)
            {
                Model.PropertyChanged -= SelectionModelPropertyChanged;
                Model.SelectionChanged -= SelectionModelSelectionChanged;
                Model = model;
                Model.SelectionChanged += SelectionModelSelectionChanged;
                _initializeOnSourceAssignment = false;

                try
                {
                    _updatingItems = true;
                    _selectedItems.Clear();

                    foreach (var i in model.SelectedItems)
                    {
                        _selectedItems.Add(i);
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

            if (_selectedItems == null)
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
                using var operation = Model.BatchUpdate();

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
                        Model.Clear();
                        Add(_selectedItems);
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

        private void SelectionModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_initializeOnSourceAssignment &&
                _selectedItems != null &&
                e.PropertyName == nameof(ISelectionModel.Source))
            {
                try
                {
                    _updatingModel = true;
                    Add(_selectedItems);
                    _initializeOnSourceAssignment = false;
                }
                finally
                {
                    _updatingModel = false;
                }
            }
        }

        private void SelectionModelSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            if (_updatingModel)
            {
                return;
            }

            if (_selectedItems == null)
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
                    _selectedItems.Remove(i);
                }

                foreach (var i in selected)
                {
                    _selectedItems.Add(i);
                }
            }
            finally
            {
                _updatingItems = false;
            }
        }

        private static int IndexOf(object? source, object? item)
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
