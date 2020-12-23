using System;
using MiniMvvm;

namespace VirtualizationDemo.ViewModels
{
    internal class ItemViewModel : ViewModelBase
    {
        private string _prefix;
        private int _index;
        private double _height = double.NaN;

        public ItemViewModel(int index, string prefix = "Item")
        {
            _prefix = prefix;
            _index = index;
        }

        public string Header => $"{_prefix} {_index}";

        public double Height
        {
            get => _height;
            set => this.RaiseAndSetIfChanged(ref _height, value);
        }
    }
}
