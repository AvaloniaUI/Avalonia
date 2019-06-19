// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using ReactiveUI;

namespace VirtualizationDemo.ViewModels
{
    internal class ItemViewModel : ReactiveObject
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
