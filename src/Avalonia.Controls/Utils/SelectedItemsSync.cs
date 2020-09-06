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
    internal class SelectedItemsSync : IDisposable
    {
        private ISelectionModel _selectionModel;
        private IList _selectedItems;
        private bool _updatingItems;
        private bool _updatingModel;

        public SelectedItemsSync(ISelectionModel model)
        {
            _selectionModel = model ?? throw new ArgumentNullException(nameof(model));
            _selectedItems = new AvaloniaList<object?>();
            SyncSelectedItemsWithSelectionModel();
            SubscribeToSelectedItems(_selectedItems);
            SubscribeToSelectionModel(model);
        }

        public ISelectionModel SelectionModel 
        {
            get => _selectionModel;
            set
            {
                if (_selectionModel != value)
                {
                    value = value ?? throw new ArgumentNullException(nameof(value));
                    UnsubscribeFromSelectionModel(_selectionModel);
                    _selectionModel = value;
                    SubscribeToSelectionModel(_selectionModel);
                    SyncSelectedItemsWithSelectionModel();
                }
            }
        }
        
        public IList SelectedItems 
        {
            get => _selectedItems;
            set
            {
                value ??= new AvaloniaList<object?>();

                if (_selectedItems != value)
                {
                    if (value.IsFixedSize)
                    {
                        throw new NotSupportedException(
                            "Cannot assign fixed size selection to SelectedItems.");
                    }

                    UnsubscribeFromSelectedItems(_selectedItems);
                    _selectedItems = value;
                    SubscribeToSelectedItems(_selectedItems);
                    SyncSelectionModelWithSelectedItems();
                }
            }
        }

        public void Dispose()
        {
            UnsubscribeFromSelectedItems(_selectedItems);
            UnsubscribeFromSelectionModel(_selectionModel);
        }

        private void SyncSelectedItemsWithSelectionModel()
        {
            _updatingItems = true;

            try
            {
                _selectedItems.Clear();

                if (_selectionModel.Source is object)
                {
                    foreach (var i in _selectionModel.SelectedItems)
                    {
                        _selectedItems.Add(i);
                    }
                }
            }
            finally
            {
                _updatingItems = false;
            }
        }

        private void SyncSelectionModelWithSelectedItems()
        {
            _updatingModel = true;

            try
            {
                if (_selectionModel.Source is object)
                {
                    using (_selectionModel.BatchUpdate())
                    {
                        SelectionModel.Clear();
                        Add(_selectedItems);
                    }
                }
            }
            finally
            {
                _updatingModel = false;
            }
        }

        private void SelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
                    var index = IndexOf(SelectionModel.Source, i);

                    if (index != -1)
                    {
                        SelectionModel.Deselect(index);
                    }
                }
            }

            try
            {
                using var operation = SelectionModel.BatchUpdate();

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
                        SelectionModel.Clear();
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
                var index = IndexOf(SelectionModel.Source, i);

                if (index != -1)
                {
                    SelectionModel.Select(index);
                }
            }
        }

        private void SelectionModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ISelectionModel.Source))
            {
                if (_selectedItems.Count > 0)
                {
                    SyncSelectionModelWithSelectedItems();
                }
                else
                {
                    SyncSelectedItemsWithSelectionModel();
                }
            }
        }

        private void SelectionModelSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            if (_updatingModel || _selectionModel.Source is null)
            {
                return;
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

        private void SelectionModelSourceReset(object sender, EventArgs e)
        {
            SyncSelectionModelWithSelectedItems();
        }


        private void SubscribeToSelectedItems(IList selectedItems)
        {
            if (selectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += SelectedItemsCollectionChanged;
            }
        }

        private void SubscribeToSelectionModel(ISelectionModel model)
        {
            model.PropertyChanged += SelectionModelPropertyChanged;
            model.SelectionChanged += SelectionModelSelectionChanged;
            model.SourceReset += SelectionModelSourceReset;
        }

        private void UnsubscribeFromSelectedItems(IList selectedItems)
        {
            if (selectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged -= SelectedItemsCollectionChanged;
            }
        }

        private void UnsubscribeFromSelectionModel(ISelectionModel model)
        {
            model.PropertyChanged -= SelectionModelPropertyChanged;
            model.SelectionChanged -= SelectionModelSelectionChanged;
            model.SourceReset -= SelectionModelSourceReset;
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
