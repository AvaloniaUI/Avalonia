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
            DataContext = new PageViewModel(this.Find<ListBox>("listBox"));
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class PageViewModel : ReactiveObject
        {
            private readonly ListBox _listBox;
            private int _counter;
            private SelectionMode _selectionMode;

            public PageViewModel(ListBox listBox)
            {
                _listBox = listBox;

                Items = new ObservableCollection<string>(Enumerable.Range(1, 10).Select(i => GenerateItem()));

                AddItemCommand = ReactiveCommand.Create(() => Items.Add(GenerateItem()));

                RemoveItemCommand = ReactiveCommand.Create(() =>
                {
                    foreach (string selectedItem in listBox.SelectedItems)
                    {
                        Items.Remove(selectedItem);
                    }
                });
            }

            public ObservableCollection<string> Items { get; }

            public ReactiveCommand<Unit, Unit> AddItemCommand { get; }

            public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }

            public SelectionMode SelectionMode
            {
                get => _selectionMode;
                set
                {
                    _listBox.SelectedItems.Clear();
                    this.RaiseAndSetIfChanged(ref _selectionMode, value);
                }
            }

            private string GenerateItem() => $"Item {_counter++}";
        }
    }
}
