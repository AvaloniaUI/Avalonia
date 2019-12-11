using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class ListBoxPage : UserControl
    {
        public ListBoxPage()
        {
            InitializeComponent();
            DataContext = new PageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class PageViewModel : ReactiveObject
        {
            private int _counter;
            private SelectionMode _selectionMode;

            public PageViewModel()
            {
                Items = new ObservableCollection<string>(Enumerable.Range(1, 10000).Select(i => GenerateItem()));
                SelectedItems = new ObservableCollection<string>();

                AddItemCommand = ReactiveCommand.Create(() => Items.Add(GenerateItem()));

                RemoveItemCommand = ReactiveCommand.Create(() =>
                {
                    while (SelectedItems.Count > 0)
                    {
                        Items.Remove(SelectedItems[0]);
                    }
                });

                SelectRandomItemCommand = ReactiveCommand.Create(() =>
                {
                    var random = new Random();

                    SelectedItem = Items[random.Next(Items.Count - 1)];
                });
            }

            public ObservableCollection<string> Items { get; }

            private string _selectedItem;

            public string SelectedItem
            {
                get { return _selectedItem; }
                set { this.RaiseAndSetIfChanged(ref _selectedItem, value); }
            }


            public ObservableCollection<string> SelectedItems { get; }

            public ReactiveCommand<Unit, Unit> AddItemCommand { get; }

            public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }

            public ReactiveCommand<Unit, Unit> SelectRandomItemCommand { get; }

            public SelectionMode SelectionMode
            {
                get => _selectionMode;
                set
                {
                    SelectedItems.Clear();
                    this.RaiseAndSetIfChanged(ref _selectionMode, value);
                }
            }

            private string GenerateItem() => $"Item {_counter++.ToString()}";
        }
    }
}
