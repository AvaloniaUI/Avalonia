using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Layout;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public sealed class FlexViewModel : ViewModelBase
    {
        private readonly ObservableCollection<FlexItemViewModel> _numbers;

        private FlexDirection _direction = FlexDirection.Row;
        private JustifyContent _justifyContent = JustifyContent.FlexStart;
        private AlignItems _alignItems = AlignItems.FlexStart;
        private AlignContent _alignContent = AlignContent.FlexStart;
        private FlexWrap _wrap = FlexWrap.Wrap;

        private int _columnSpacing = 64;
        private int _rowSpacing = 32;

        private int _currentNumber = 41;

        private FlexItemViewModel? _selectedItem;

        public FlexViewModel()
        {
            _numbers = new ObservableCollection<FlexItemViewModel>(Enumerable.Range(1, 3).Select(x => new FlexItemViewModel(x)));

            Numbers = new ReadOnlyObservableCollection<FlexItemViewModel>(_numbers);

            AddItemCommand = MiniCommand.Create(AddItem);
            RemoveItemCommand = MiniCommand.Create(RemoveItem);
        }

        public IEnumerable DirectionValues { get; } = Enum.GetValues(typeof(FlexDirection));

        public IEnumerable JustifyContentValues { get; } = Enum.GetValues(typeof(JustifyContent));

        public IEnumerable AlignItemsValues { get; } = Enum.GetValues(typeof(AlignItems));

        public IEnumerable AlignContentValues { get; } = Enum.GetValues(typeof(AlignContent));

        public IEnumerable WrapValues { get; } = Enum.GetValues(typeof(FlexWrap));

        public IEnumerable FlexBasisKindValues { get; } = Enum.GetValues(typeof(FlexBasisKind));

        public IEnumerable HorizontalAlignmentValues { get; } = Enum.GetValues(typeof(HorizontalAlignment));

        public IEnumerable VerticalAlignmentValues { get; } = Enum.GetValues(typeof(VerticalAlignment));

        public IEnumerable AlignSelfValues { get; } = Enum.GetValues(typeof(AlignItems)).Cast<AlignItems>().Prepend(FlexItemViewModel.AlignSelfAuto);
        
        public FlexDirection Direction
        {
            get => _direction;
            set => this.RaiseAndSetIfChanged(ref _direction, value);
        }

        public JustifyContent JustifyContent
        {
            get => _justifyContent;
            set => this.RaiseAndSetIfChanged(ref _justifyContent, value);
        }

        public AlignItems AlignItems
        {
            get => _alignItems;
            set => this.RaiseAndSetIfChanged(ref _alignItems, value);
        }

        public AlignContent AlignContent
        {
            get => _alignContent;
            set => this.RaiseAndSetIfChanged(ref _alignContent, value);
        }

        public FlexWrap Wrap
        {
            get => _wrap;
            set => this.RaiseAndSetIfChanged(ref _wrap, value);
        }

        public int ColumnSpacing
        {
            get => _columnSpacing;
            set => this.RaiseAndSetIfChanged(ref _columnSpacing, value);
        }

        public int RowSpacing
        {
            get => _rowSpacing;
            set => this.RaiseAndSetIfChanged(ref _rowSpacing, value);
        }

        public ReadOnlyObservableCollection<FlexItemViewModel> Numbers { get; }

        public FlexItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public ICommand AddItemCommand { get; }

        public ICommand RemoveItemCommand { get; }

        private void AddItem() => _numbers.Add(new FlexItemViewModel(_currentNumber++));

        private void RemoveItem()
        {
            if (SelectedItem is null)
            {
                return;
            }

            _numbers.Remove(SelectedItem);

            SelectedItem.IsSelected = false;
            SelectedItem = null;
        }
    }
}
