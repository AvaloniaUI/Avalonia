// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using ReactiveUI;

namespace VirtualizationTest.ViewModels
{
    internal class MainWindowViewModel : ReactiveObject
    {
        private int _itemCount = 200;

        public MainWindowViewModel()
        {
            this.WhenAnyValue(x => x.ItemCount).Subscribe(ResizeItems);
        }

        public int ItemCount
        {
            get { return _itemCount; }
            set { this.RaiseAndSetIfChanged(ref _itemCount, value); }
        }

        public IReactiveList<ItemViewModel> Items { get; private set; }

        private void ResizeItems(int count)
        {
            if (Items == null)
            {
                var items = Enumerable.Range(0, count).Select(x => new ItemViewModel(x));
                Items = new ReactiveList<ItemViewModel>(items);
            }
        }
    }
}
