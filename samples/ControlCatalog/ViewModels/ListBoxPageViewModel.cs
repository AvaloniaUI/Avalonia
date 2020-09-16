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
        private bool _multiple;
        private bool _toggle;
        private bool _alwaysSelected;
        private bool _autoScrollToSelectedItem = true;
        private int _counter;
        private ObservableAsPropertyHelper<SelectionMode> _selectionMode;

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
                    (m ? SelectionMode.Multiple : 0) |
                    (t ? SelectionMode.Toggle : 0) |
                    (a ? SelectionMode.AlwaysSelected : 0))
                .ToProperty(this, x => x.SelectionMode);

            AddItemCommand = ReactiveCommand.Create(() => Items.Add(GenerateItem()));

            RemoveItemCommand = ReactiveCommand.Create(() =>
            {
                var items = Selection.SelectedItems.ToList();

                foreach (var item in items)
                {
                    Items.Remove(item);
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
        public SelectionMode SelectionMode => _selectionMode.Value;

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

        public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectRandomItemCommand { get; }

        private string GenerateItem() => $"Item {_counter++.ToString()}";
    }
}
