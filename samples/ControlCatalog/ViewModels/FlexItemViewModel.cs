using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public sealed class FlexItemViewModel : ViewModelBase
    {
        internal const AlignItems AlignSelfAuto = (AlignItems)(-1);

        private AlignItems _alignSelf;

        private bool _isSelected;
        private bool _isVisible = true;

        private AlignItems _alignSelfItem = AlignSelfAuto;
        private int _order;
        private double _shrink = 1.0;
        private double _grow;
        private double _basisValue = 100.0;
        private FlexBasisKind _basisKind;
        private HorizontalAlignment _horizontalAlignment;
        private VerticalAlignment _verticalAlignment; 

        public FlexItemViewModel(int value)
        {
            Value = value;

            _alignSelf = AlignSelfItem == AlignSelfAuto ? default(AlignItems) : AlignSelfItem;

            var color = Random.Shared.Next();

            Color = new SolidColorBrush((uint)color);
        }

        public int Value { get; }

        public Brush Color { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => this.RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public AlignItems AlignSelfItem
        {
            get => _alignSelfItem;
            set
            {
                this.RaiseAndSetIfChanged(ref _alignSelfItem, value);
                this.RaisePropertyChanged(nameof(AlignSelf));
            }
        }

        public AlignItems? AlignSelf => _alignSelf;

        public int Order
        {
            get => _order;
            set => this.RaiseAndSetIfChanged(ref _order, value);
        }

        public double Shrink
        {
            get => _shrink;
            set => this.RaiseAndSetIfChanged(ref _shrink, value);
        }

        public double Grow
        {
            get => _grow;
            set => this.RaiseAndSetIfChanged(ref _grow, value);
        }

        public double BasisValue
        {
            get => _basisValue;
            set
            {
                this.RaiseAndSetIfChanged(ref _basisValue, value);
                this.RaisePropertyChanged(nameof(Basis));
            }
        }

        public FlexBasisKind BasisKind
        {
            get => _basisKind;
            set
            {
                this.RaiseAndSetIfChanged(ref _basisKind, value);
                this.RaisePropertyChanged(nameof(Basis));
            }
        }

        public FlexBasis Basis => new(_basisValue, _basisKind);

        public HorizontalAlignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set => this.RaiseAndSetIfChanged(ref _horizontalAlignment, value);
        }

        public VerticalAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set => this.RaiseAndSetIfChanged(ref _verticalAlignment, value);
        }
    }
}
