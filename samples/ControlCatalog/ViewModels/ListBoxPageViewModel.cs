using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using MiniMvvm;

#nullable enable

namespace ControlCatalog.ViewModels
{
    public class ListBoxPageViewModel : ViewModelBase
    {
        private bool _multiple;
        private bool _toggle;
        private bool _alwaysSelected;
        private bool _autoScrollToSelectedItem = true;
        private int _counter;
        private IObservable<SelectionMode> _selectionMode;
        private ItemVirtualizationMode _virtualizationMode = ItemVirtualizationMode.Simple; 

        public ListBoxPageViewModel()
        {
            Items = new ObservableCollection<Item>(GenerateItems(10000));

            Selection = new SelectionModel<Item>();
            Selection.Select(1);

            _selectionMode = this.WhenAnyValue(
                x => x.Multiple,
                x => x.Toggle,
                x => x.AlwaysSelected,
                (m, t, a) =>
                    (m ? Avalonia.Controls.SelectionMode.Multiple : 0) |
                    (t ? Avalonia.Controls.SelectionMode.Toggle : 0) |
                    (a ? Avalonia.Controls.SelectionMode.AlwaysSelected : 0));

            AddItemCommand = MiniCommand.Create(() => Items.Add(GenerateItem()));

            RemoveItemCommand = MiniCommand.Create(() =>
            {
                var items = Selection.SelectedItems.ToList();

                foreach (var item in items)
                {
                    Items.Remove(item);
                }
            });

            SelectRandomItemCommand = MiniCommand.Create(() =>
            {
                var random = new Random();

                using (Selection.BatchUpdate())
                {
                    Selection.Clear();
                    Selection.Select(random.Next(Items.Count - 1));
                }
            });

            RandomizeHeightsCommand = MiniCommand.Create(() =>
            {
                var random = new Random();

                foreach (var i in Items)
                {
                    i.Height = random.Next(240) + 10;
                }
            });
        }

        public ObservableCollection<Item> Items { get; }
        public SelectionModel<Item> Selection { get; }
        public IObservable<SelectionMode> SelectionMode => _selectionMode;

        public bool Multiple
        {
            get => _multiple;
            set => this.RaiseAndSetIfChanged(ref _multiple, value);
        }

        public bool Toggle
        {
            get => _toggle;
            set => this.RaiseAndSetIfChanged(ref _toggle, value);
        }

        public bool AlwaysSelected
        {
            get => _alwaysSelected;
            set => this.RaiseAndSetIfChanged(ref _alwaysSelected, value);
        }

        public bool AutoScrollToSelectedItem
        {
            get => _autoScrollToSelectedItem;
            set => this.RaiseAndSetIfChanged(ref _autoScrollToSelectedItem, value);
        }

        public ItemVirtualizationMode VirtualizationMode
        {
            get => _virtualizationMode;
            set => RaiseAndSetIfChanged(ref _virtualizationMode, value);
        }

        public ItemVirtualizationMode[] VirtualizationModes { get; } = new[]
        {
            ItemVirtualizationMode.Simple,
        };

        public MiniCommand AddItemCommand { get; }
        public MiniCommand RemoveItemCommand { get; }
        public MiniCommand SelectRandomItemCommand { get; }
        public MiniCommand RandomizeHeightsCommand { get; }

        private Item GenerateItem() => new($"Item {_counter++}");
        private IEnumerable<Item> GenerateItems(int count) => Enumerable.Range(0, count).Select(x => GenerateItem());

        public class Item : ViewModelBase
        {
            private double _height = double.NaN;
            public Item(string name) => Name = name;
            public string Name { get; }

            public double Height
            {
                get => _height;
                set => RaiseAndSetIfChanged(ref _height, value);
            }
        }
    }
}
