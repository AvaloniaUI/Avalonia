// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ReactiveUI.Legacy;
using ReactiveUI;
using Avalonia.Layout;

namespace VirtualizationDemo.ViewModels
{
    internal class MainWindowViewModel : ReactiveObject
    {
        private int _itemCount = 200;
        private string _newItemString = "New Item";
        private int _newItemIndex;
        private IReactiveList<ItemViewModel> _items;
        private string _prefix = "Item";
        private ScrollBarVisibility _horizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        private ScrollBarVisibility _verticalScrollBarVisibility = ScrollBarVisibility.Auto;
        private Orientation _orientation = Orientation.Vertical;
        private ItemVirtualizationMode _virtualizationMode = ItemVirtualizationMode.Simple;

        public MainWindowViewModel()
        {
            this.WhenAnyValue(x => x.ItemCount).Subscribe(ResizeItems);
            RecreateCommand = ReactiveCommand.Create(() => Recreate());

            AddItemCommand = ReactiveCommand.Create(() => AddItem());

            RemoveItemCommand = ReactiveCommand.Create(() => Remove());

            SelectFirstCommand = ReactiveCommand.Create(() => SelectItem(0));

            SelectLastCommand = ReactiveCommand.Create(() => SelectItem(Items.Count - 1));
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

        public Orientation Orientation
        {
            get { return _orientation; }
            set { this.RaiseAndSetIfChanged(ref _orientation, value); }
        }

        public IEnumerable<Orientation> Orientations =>
            Enum.GetValues(typeof(Orientation)).Cast<Orientation>();

        public ScrollBarVisibility HorizontalScrollBarVisibility
        {
            get { return _horizontalScrollBarVisibility; }
            set { this.RaiseAndSetIfChanged(ref _horizontalScrollBarVisibility, value); }
        }

        public ScrollBarVisibility VerticalScrollBarVisibility
        {
            get { return _verticalScrollBarVisibility; }
            set { this.RaiseAndSetIfChanged(ref _verticalScrollBarVisibility, value); }
        }

        public IEnumerable<ScrollBarVisibility> ScrollBarVisibilities =>
            Enum.GetValues(typeof(ScrollBarVisibility)).Cast<ScrollBarVisibility>();

        public ItemVirtualizationMode VirtualizationMode
        {
            get { return _virtualizationMode; }
            set { this.RaiseAndSetIfChanged(ref _virtualizationMode, value); }
        }

        public IEnumerable<ItemVirtualizationMode> VirtualizationModes => 
            Enum.GetValues(typeof(ItemVirtualizationMode)).Cast<ItemVirtualizationMode>();

        public ReactiveCommand AddItemCommand { get; private set; }
        public ReactiveCommand RecreateCommand { get; private set; }
        public ReactiveCommand RemoveItemCommand { get; private set; }
        public ReactiveCommand SelectFirstCommand { get; private set; }
        public ReactiveCommand SelectLastCommand { get; private set; }

        public void RandomizeSize()
        {
            var random = new Random();

            foreach (var i in Items)
            {
                i.Height = random.Next(240) + 10;
            }
        }

        public void ResetSize()
        {
            foreach (var i in Items)
            {
                i.Height = double.NaN;
            }
        }

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
                index = Items.IndexOf(SelectedItems[0]);
            }

            Items.Insert(index, new ItemViewModel(_newItemIndex++, NewItemString));
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

        private void SelectItem(int index)
        {
            SelectedItems.Clear();
            SelectedItems.Add(Items[index]);
        }
    }
}
