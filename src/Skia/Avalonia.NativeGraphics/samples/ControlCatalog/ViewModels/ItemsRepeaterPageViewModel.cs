using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Media;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class ItemsRepeaterPageViewModel : ViewModelBase
    {
        private int _newItemIndex = 1;
        private int _newGenerationIndex = 0;
        private ObservableCollection<Item> _items;

        public ItemsRepeaterPageViewModel()
        {
            _items = CreateItems();
        }

        public ObservableCollection<Item> Items
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }

        public Item? SelectedItem { get; set; }

        public void AddItem()
        {
            var index = SelectedItem != null ? Items.IndexOf(SelectedItem) : -1;
            Items.Insert(index + 1, new Item(index + 1, $"New Item {_newItemIndex++}"));
        }

        public void RemoveItem()
        {
            if (SelectedItem is not null)
            {
                Items.Remove(SelectedItem);
                SelectedItem = null;
            }
            else if (Items.Count > 0)
            {
                Items.RemoveAt(Items.Count - 1);
            }
        }

        public void RandomizeHeights()
        {
            var random = new Random();

            foreach (var i in Items)
            {
                i.Height = random.Next(240) + 10;
            }
        }

        public void ResetItems()
        {
            Items = CreateItems();
        }

        private ObservableCollection<Item> CreateItems()
        {
            var suffix = _newGenerationIndex == 0 ? string.Empty : $"[{_newGenerationIndex.ToString()}]";

            _newGenerationIndex++;

            return new ObservableCollection<Item>(
                Enumerable.Range(1, 100000).Select(i => new Item(i, $"Item {i.ToString()} {suffix}")));
        }

        public class Item : ViewModelBase
        {
            private double _height = double.NaN;

            public Item(int index, string text)
            {
                Index = index;
                Text = text;
            }
            public int Index { get; }
            public string Text { get; }
            
            public double Height 
            {
                get => _height;
                set => this.RaiseAndSetIfChanged(ref _height, value);
            }
        }
    }
}
