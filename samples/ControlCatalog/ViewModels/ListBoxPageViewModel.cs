using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using MiniMvvm;

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

        public ListBoxPageViewModel()
        {
            Items = new ObservableCollection<string>(Enumerable.Range(1, 10000).Select(i => GenerateItem()));
            
            Selection = new SelectionModel<string>();
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
        }

        public ObservableCollection<string> Items { get; }
        public SelectionModel<string> Selection { get; }
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

        public MiniCommand AddItemCommand { get; }
        public MiniCommand RemoveItemCommand { get; }
        public MiniCommand SelectRandomItemCommand { get; }

        private string GenerateItem() => $"Item {_counter++.ToString()}";
    }
}
