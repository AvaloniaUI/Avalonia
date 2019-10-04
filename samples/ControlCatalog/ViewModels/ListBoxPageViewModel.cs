using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public enum LayoutType
    {
        StackVertical,
        StackHorizontal,
        UniformGridHorizontal,
        UniformGridVertical,
    }

    public class ListBoxPageViewModel :ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<AttachedLayout> _layout;
        private readonly ObservableAsPropertyHelper<ScrollBarVisibility> _horizontalScroll;
        private readonly ObservableAsPropertyHelper<ScrollBarVisibility> _verticalScroll;
        private int _counter;
        private LayoutType _layoutType;
        private SelectionMode _selectionMode;
        private string _selectedItem;

        public ListBoxPageViewModel()
        {
            Items = new ObservableCollection<string>(Enumerable.Range(1, 10000).Select(i => GenerateItem()));
            SelectedItems = new ObservableCollection<string>();

            _layout = this.WhenAnyValue(x => x.LayoutType)
                .Select(CreateLayout)
                .ToProperty(this, x => x.Layout);

            _horizontalScroll = this.WhenAnyValue(x => x.LayoutType)
                .Select(x => (((int)x) % 2) == 0 ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto)
                .ToProperty(this, x => x.HorizontalScroll);

            _verticalScroll = this.WhenAnyValue(x => x.LayoutType)
                .Select(x => (((int)x) % 2) != 0 ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto)
                .ToProperty(this, x => x.VerticalScroll);

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
        public AttachedLayout Layout => _layout.Value;
        public ScrollBarVisibility HorizontalScroll => _horizontalScroll.Value;
        public ScrollBarVisibility VerticalScroll => _verticalScroll.Value;
        public ObservableCollection<string> SelectedItems { get; }
        public ReactiveCommand<Unit, Unit> AddItemCommand { get; }
        public ReactiveCommand<Unit, Unit> RemoveItemCommand { get; }
        public ReactiveCommand<Unit, Unit> SelectRandomItemCommand { get; }

        public LayoutType LayoutType
        {
            get => _layoutType;
            set => this.RaiseAndSetIfChanged(ref _layoutType, value);
        }

        public string SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

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

        private static AttachedLayout CreateLayout(LayoutType type)
        {
            switch (type)
            {
                case LayoutType.StackVertical:
                    return new StackLayout { Orientation = Orientation.Vertical };
                case LayoutType.StackHorizontal:
                    return new StackLayout { Orientation = Orientation.Horizontal };
                case LayoutType.UniformGridVertical:
                    return new UniformGridLayout 
                    {
                        MinItemWidth = 70,
                        Orientation = Orientation.Vertical 
                    };
                case LayoutType.UniformGridHorizontal:
                    return new UniformGridLayout 
                    {
                        MinItemWidth = 70,
                        Orientation = Orientation.Horizontal 
                    };
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
