using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class ListBoxPageViewModel : ReactiveObject
    {
        private int _counter;
        private SelectionMode _selectionMode;

        public ListBoxPageViewModel()
        {
            Items = new ObservableCollection<string>(Enumerable.Range(1, 10000).Select(i => GenerateItem()));
            SelectedItems = new ObservableCollection<string>();
            SelectedItems.Add(Items[1]);

            AddItemCommand = ReactiveCommand.Create(() => Items.Add(GenerateItem()));

            RemoveItemCommand = ReactiveCommand.Create(() =>
            {
                while (SelectedItems.Count > 0)
                {
                    Items.Remove(SelectedItems.First());
                }
            });

            SelectRandomItemCommand = ReactiveCommand.Create(() =>
            {
                var random = new Random();

                SelectedItems.Clear();
                SelectedItems.Add(Items[random.Next(Items.Count - 1)]);
            });
        }

        public ObservableCollection<string> Items { get; }

        public ObservableCollection<string> SelectedItems { get; }

        public ReactiveCommand<Unit, Unit> AddItemCommand { get; }

        public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }

        public ReactiveCommand<Unit, Unit> SelectRandomItemCommand { get; }

        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectionMode, value);
            }
        }

        private string GenerateItem() => $"Item {_counter++.ToString()}";
    }
}
