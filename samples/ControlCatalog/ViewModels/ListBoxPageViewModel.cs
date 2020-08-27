using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
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
            Selection = new SelectionModel<string>();
            Selection.Select(1);

            AddItemCommand = ReactiveCommand.Create(() => Items.Add(GenerateItem()));

            RemoveItemCommand = ReactiveCommand.Create(() =>
            {
                while (Selection.Count > 0)
                {
                    Items.Remove(Selection.SelectedItems.First());
                }
            });

            SelectRandomItemCommand = ReactiveCommand.Create(() =>
            {
                var random = new Random();

                using (Selection.BatchUpdate())
                {
                    Selection.Clear();
                    Selection.Select(random.Next(Items.Count - 1));
                }
            });
        }

        public ObservableCollection<string> Items { get; }

        public SelectionModel<string> Selection { get; }

        public ReactiveCommand<Unit, Unit> AddItemCommand { get; }

        public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }

        public ReactiveCommand<Unit, Unit> SelectRandomItemCommand { get; }

        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                Selection.Clear();
                this.RaiseAndSetIfChanged(ref _selectionMode, value);
            }
        }

        private string GenerateItem() => $"Item {_counter++.ToString()}";
    }
}
