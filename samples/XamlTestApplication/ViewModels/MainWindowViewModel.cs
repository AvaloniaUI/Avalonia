// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace XamlTestApplication.ViewModels
{
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            Items = new List<TestItem>();

            for (int i = 0; i < 10; ++i)
            {
                Items.Add(new TestItem($"Item {i}", $"Item {i} Value"));
            }
        }

        public List<TestItem> Items { get; }
    }
}
