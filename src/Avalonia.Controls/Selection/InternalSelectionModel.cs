using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Collections;

#nullable enable

namespace Avalonia.Controls.Selection
{
    internal class InternalSelectionModel : SelectionModel<object?>
    {
        private IList? _selectedItems;
        private bool _ignoreModelChanges;
        private bool _ignoreSelectedItemsChanges;

        public InternalSelectionModel()
        {
            SelectionChanged += OnSelectionChanged;
            SourceReset += OnSourceReset;
        }

        [AllowNull]
        public new IList SelectedItems
        {
            get
            {
                if (_selectedItems is null)
                {
                    _selectedItems = new AvaloniaList<object?>();
                    SubscribeToSelectedItems();
                }

                return _selectedItems;
            }
            set
            {
                value ??= new AvaloniaList<object?>();

                if (value.IsFixedSize)
                {
                    throw new NotSupportedException("Cannot assign fixed size selection to SelectedItems.");
                }

                if (_selectedItems != value)
                {
                    UnsubscribeFromSelectedItems();
                    _selectedItems = value;
                    SyncFromSelectedItems();
                    SubscribeToSelectedItems();
                    
                    if (ItemsView is null)
                    {
                        SetInitSelectedItems(value);
                    }
                }
            }
        }

        private protected override void SetSource(IEnumerable? value)
        {
            try
            {
                _ignoreSelectedItemsChanges = true;
                base.SetSource(value);
            }
            finally
            {
                _ignoreSelectedItemsChanges = false;
            }

            SyncToSelectedItems();
        }

        private void SyncToSelectedItems()
        {
            if (_selectedItems is object)
            {
                try
                {
                    _ignoreSelectedItemsChanges = true;
                    _selectedItems.Clear();

                    foreach (var i in base.SelectedItems)
                    {
                        _selectedItems.Add(i);
                    }
                }
                finally
                {
                    _ignoreSelectedItemsChanges = false;
                }
            }
        }

        private void SyncFromSelectedItems()
        {
            if (Source is null || _selectedItems is null)
            {
                return;
            }

            try
            {
                _ignoreModelChanges = true;

                using (BatchUpdate())
                {
                    Clear();
                    Add(_selectedItems);
                }
            }
            finally
            {
                _ignoreModelChanges = false;
            }
        }

        private void SubscribeToSelectedItems()
        {
            if (_selectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnSelectedItemsCollectionChanged;
            }
        }

        private void UnsubscribeFromSelectedItems()
        {
            if (_selectedItems is INotifyCollectionChanged incc)
            {
                incc.CollectionChanged += OnSelectedItemsCollectionChanged;
            }
        }

        private void OnSelectionChanged(object sender, SelectionModelSelectionChangedEventArgs e)
        {
            if (_ignoreModelChanges)
            {
                return;
            }

            try
            {
                var items = SelectedItems;
                var deselected = e.DeselectedItems.ToList();
                var selected = e.SelectedItems.ToList();

                _ignoreSelectedItemsChanges = true;

                foreach (var i in deselected)
                {
                    items.Remove(i);
                }

                foreach (var i in selected)
                {
                    items.Add(i);
                }
            }
            finally
            {
                _ignoreSelectedItemsChanges = false;
            }
        }

        private void OnSourceReset(object sender, EventArgs e) => SyncFromSelectedItems();

        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_ignoreSelectedItemsChanges)
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
                    var index = IndexOf(Source, i);

                    if (index != -1)
                    {
                        Deselect(index);
                    }
                }
            }

            try
            {
                using var operation = BatchUpdate();

                _ignoreModelChanges = true;

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
                        Clear();
                        Add(_selectedItems);
                        break;
                }
            }
            finally
            {
                _ignoreModelChanges = false;
            }
        }

        private void Add(IList newItems)
        {
            foreach (var i in newItems)
            {
                var index = IndexOf(Source, i);

                if (index != -1)
                {
                    Select(index);
                }
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
