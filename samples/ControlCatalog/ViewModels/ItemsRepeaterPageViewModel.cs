using System;
using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class ItemsRepeaterPageViewModel : ReactiveObject
    {
        private int _newItemIndex = 1;
        private int _newGenerationIndex = 0;
        private ObservableCollection<Item> _items;

        public ItemsRepeaterPageViewModel()
        {
            Items = CreateItems();
        }

        public ObservableCollection<Item> Items
        {
            get => _items;
            set => this.RaiseAndSetIfChanged(ref _items, value);
        }

        public Item SelectedItem { get; set; }

        public void AddItem()
        {
            var index = SelectedItem != null ? Items.IndexOf(SelectedItem) : -1;
            Items.Insert(index + 1, new Item { Text = $"New Item {_newItemIndex++}" });
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
                Enumerable.Range(1, 100000).Select(i => new Item
                {
                    Text = $"Item {i.ToString()} {suffix}"
                }));
        }

        public class Item : ReactiveObject
        {
            private double _height = double.NaN;

            public string Text { get; set; }
            
            public double Height 
            {
                get => _height;
                set => this.RaiseAndSetIfChanged(ref _height, value);
            }
        }
    }
}
