// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Collections;
using ReactiveUI;

namespace VirtualizationTest.ViewModels
{
    internal class MainWindowViewModel : ReactiveObject
    {
        private int _itemCount = 200;
        private string _newItemString;
        private IReactiveList<ItemViewModel> _items;
        private string _prefix = "Item";

        public MainWindowViewModel()
        {
            this.WhenAnyValue(x => x.ItemCount).Subscribe(ResizeItems);
            RecreateCommand = ReactiveCommand.Create();
            RecreateCommand.Subscribe(_ => Recreate());

            AddItemCommand = ReactiveCommand.Create();
            AddItemCommand.Subscribe(_ => AddItem());

            RemoveItemCommand = ReactiveCommand.Create();
            RemoveItemCommand.Subscribe(_ => Remove());
        }

        public string NewItemString
        {
            get { return _newItemString; }
            set { this.RaiseAndSetIfChanged(ref _newItemString, value); }
        }

        public int ItemCount
        {
            get { return _itemCount; }
            set { this.RaiseAndSetIfChanged(ref _itemCount, value); }
        }

        public AvaloniaList<ItemViewModel> SelectedItems { get; } 
            = new AvaloniaList<ItemViewModel>();

        public IReactiveList<ItemViewModel> Items
        {
            get { return _items; }
            private set { this.RaiseAndSetIfChanged(ref _items, value); }
        }

        public ReactiveCommand<object> AddItemCommand { get; private set; }

        public ReactiveCommand<object> RecreateCommand { get; private set; }

        public ReactiveCommand<object> RemoveItemCommand { get; private set; }

        private void ResizeItems(int count)
        {
            if (Items == null)
            {
                var items = Enumerable.Range(0, count)
                    .Select(x => new ItemViewModel(x));
                Items = new ReactiveList<ItemViewModel>(items);
            }
            else if (count > Items.Count)
            {
                var items = Enumerable.Range(Items.Count, count - Items.Count)
                    .Select(x => new ItemViewModel(x));
                Items.AddRange(items);
            }
            else if (count < Items.Count)
            {
                Items.RemoveRange(count, Items.Count - count);
            }
        }

        private void AddItem()
        {
            var index = Items.Count;

            if (SelectedItems.Count > 0)
            {
                index = Items.IndexOf(SelectedItems[0]) + 1;
            }

            Items.Insert(index, new ItemViewModel(index, NewItemString));
        }

        private void Remove()
        {
            if (SelectedItems.Count > 0)
            {
                Items.RemoveAll(SelectedItems);
            }
        }

        private void Recreate()
        {
            _prefix = _prefix == "Item" ? "Recreated" : "Item";
            var items = Enumerable.Range(0, _itemCount)
                .Select(x => new ItemViewModel(x, _prefix));
            Items = new ReactiveList<ItemViewModel>(items);
        }
    }
}
